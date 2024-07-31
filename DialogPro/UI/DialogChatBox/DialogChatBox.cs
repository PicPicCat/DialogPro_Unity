using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScrollerPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogPro.UI
{
    public abstract class DialogChatBox : MonoBehaviour
    {
        [SerializeField] private ScrollerList scrollerList;
        [SerializeField] private float popUpSpeed;
        [SerializeField] private AnimationCurve popUpCurve;

        [SerializeField] private GameObject contentCellViewPrefab;
        [SerializeField] private int contentCellPoolSize;
        [SerializeField] private GameObject optionsCellViewPrefab;
        [SerializeField] private int optionsCCellPoolSize;

        private List<ScrollerNode> _nodeList = new();
        private Action _callBack_goNext;
        private Action<int> _callback_selectOption;
        private bool _lock_printFinish;
        private bool _lock_popUpFinish;
        private ScrollerNodePool<ContentViewNode, ContentCellView> _contentViewPool;
        private ScrollerNodePool<OptionsViewNode, OptionsCellView> _optionsViewPool;
        public virtual DialogPrinter GetPrinter() => new();

        private void Awake()
        {
            _contentViewPool = new ScrollerNodePool<ContentViewNode, ContentCellView>
            {
                prefab = contentCellViewPrefab,
                root = transform,
                poolSize = contentCellPoolSize
            };
            _optionsViewPool = new ScrollerNodePool<OptionsViewNode, OptionsCellView>
            {
                prefab = optionsCellViewPrefab,
                root = transform,
                poolSize = optionsCCellPoolSize
            };
        }

        public void ClearAll()
        {
            scrollerList.ClearList();
            _nodeList.Clear();
            _contentViewPool.Clear();
            _optionsViewPool.Clear();
        }

        public void PrintDialog(int dialogID, PrintData speaker, PrintData content, Action callback)
        {
            _callBack_goNext = callback;
            SetGoNextBtnInteractable(false);
            
            var speakerPrinter = GetPrinter();
            speakerPrinter.SetPrintData(speaker.elements);
            
            var contentPrinter = GetPrinter();
            contentPrinter.SetPrintData(content.elements);
            
            var cellData = new DialogContentCellData
            {
                first = true,
                speaker = speakerPrinter,
                content = contentPrinter,
            };
            
            cellData.cellLen = GetContentCellLen(cellData);
            var node = new ContentViewNode
            {
                pool = _contentViewPool,
                cellData = cellData,
            };
            if (_nodeList.Count > 0)
            {
                node.right = _nodeList[0];
                _nodeList[0].left = node;
            }

            _lock_printFinish = false;
            _lock_popUpFinish = false;
            _nodeList.Insert(0, node);
            scrollerList.SetHeadPos(node, 1);
            StopAllCoroutines();
            StartCoroutine(PopUp());
            return;

            IEnumerator PopUp()
            {
                yield return null;
                var len = node.cellData.cellLen;
                for (var s = 0f; s < len; s += Time.unscaledDeltaTime * popUpSpeed)
                {
                    var delta = s / len;
                    var v = popUpCurve.Evaluate(delta);
                    scrollerList.SetHeadPos(node, 1 - v);
                    yield return null;
                }

                scrollerList.SetHeadPos(node, 0);

                yield return null;
                _lock_popUpFinish = true;
                if (_lock_printFinish) SetGoNextBtnInteractable(true);
            }
        }

        public void SetOptions(IEnumerable<PrintData> options, Action<int> callback)
        {
            _callback_selectOption = callback;
            SetGoNextBtnInteractable(false);
            var labelPrinters = new List<DialogPrinter>();
            foreach (var option in options)
            {
                var printer = GetPrinter();
                printer.SetPrintData(option.elements);
                labelPrinters.Add(printer);
            }
            var cellData = new DialogOptionsCellData
            {
                first = true,
                options = labelPrinters.ToArray()
            };
            cellData.cellLen = GetOptionsCellLen(cellData);
            var node = new OptionsViewNode
            {
                pool = _optionsViewPool,
                cellData = cellData,
            };
            if (_nodeList.Count > 0)
            {
                node.right = _nodeList[0];
                _nodeList[0].left = node;
            }

            _lock_printFinish = false;
            _lock_popUpFinish = false;
            _nodeList.Insert(0, node);
            scrollerList.SetHeadPos(node, 1);
            StopAllCoroutines();
            StartCoroutine(PopUp());
            return;

            IEnumerator PopUp()
            {
                yield return null;
                var len = node.cellData.cellLen;
                for (var s = 0f; s < len; s += Time.unscaledDeltaTime * popUpSpeed)
                {
                    var delta = s / len;
                    var v = popUpCurve.Evaluate(delta);
                    scrollerList.SetHeadPos(node, 1 - v);
                    yield return null;
                }

                scrollerList.SetHeadPos(node, 0);
                yield return null;
                _lock_popUpFinish = true;

                if (_lock_printFinish)
                {
                    var index = node.cellData.selectedIndex;
                    var tmp = _callback_selectOption;
                    _callback_selectOption = null;
                    tmp?.Invoke(index);
                }
            }
        }

        public virtual float GetContentCellLen(DialogContentCellData cellData)
        {
            var view = contentCellViewPrefab.GetComponent<ContentCellView>();
            view.gameObject.SetActive(true);
            view.rectTransform.sizeDelta = scrollerList.ViewSize;

            var printer = GetPrinter();
            printer.SetPrintData(cellData.content.GetPrintData());
            view.text_content.text = printer.Print();
            
            var height_speaker = view.text_speaker.rectTransform.sizeDelta.y;
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.text_content.rectTransform);
            var height_content = view.text_content.preferredHeight;
            view.text_content.text = string.Empty;

            view.gameObject.SetActive(false);
            return height_speaker + height_content;
        }

        public virtual float GetOptionsCellLen(DialogOptionsCellData cellData)
        {
            var view = optionsCellViewPrefab.GetComponent<OptionsCellView>();
            view.gameObject.SetActive(true);
            view.rectTransform.sizeDelta = scrollerList.ViewSize;

            var layout = view.labelRoot.GetComponent<VerticalLayoutGroup>();
            var len = layout.spacing * (cellData.options.Length - 1)
                      + layout.padding.top + layout.padding.bottom;
            var label_height = view.labelRoot.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
            len += cellData.options.Length * label_height;
            view.gameObject.SetActive(false);
            return len;
        }

        public void OnContentPrintFinish()
        {
            _lock_printFinish = true;
            if (_lock_popUpFinish) SetGoNextBtnInteractable(true);
        }

        public void OnSelectOptionFinish(int index)
        {
            _lock_printFinish = true;
            if (_lock_popUpFinish)
            {
                var tmp = _callback_selectOption;
                _callback_selectOption = null;
                tmp?.Invoke(index);
            }
        }

        public void OnClickGoNext()
        {
            var tmp = _callBack_goNext;
            _callBack_goNext = null;
            tmp?.Invoke();
        }

        protected abstract void SetGoNextBtnInteractable(bool val);

        private class ContentViewNode : ScrollerNode<ContentCellView>
        {
            public DialogContentCellData cellData;

            protected override void BeforeNewCellUse(ContentCellView cellView)
            {
                cellView.SetData(cellData);
            }
        }

        private class OptionsViewNode : ScrollerNode<OptionsCellView>
        {
            public DialogOptionsCellData cellData;

            protected override void BeforeNewCellUse(OptionsCellView cellView)
            {
                cellView.SetData(cellData);
            }
        }
    }
}