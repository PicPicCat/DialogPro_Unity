using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DialogPro.UI
{
    public class DialogBox : MonoBehaviour
    {
        public TextMeshProUGUI text_content;
        public TextMeshProUGUI text_speaker;
        public Button button_next;
        public float printSpeed;
        public RectTransform optionsRoot;
        
        private Action<int> onSelect_callback;
        private Action onFinish_callback;
        private DialogPrinter printer;
        protected virtual DialogPrinter GetPrinter() => new();
        private void Start()
        {
            for (var i = 0; i < optionsRoot.childCount; i++)
            {
                var index = i;
                var child = optionsRoot.GetChild(index);
                child.gameObject.SetActive(false);
                var button = child.GetComponent<Button>();
                button.onClick.AddListener(() => select_finish(index));
            }
        }
        private void Update()
        {
            if(printer==null)return;
            var delta = printSpeed * Time.unscaledDeltaTime;
            text_content.text = printer.Print(delta, out var finish);
            if (finish) print_end();
        }
        
        /// <summary>
        /// 打印对话
        /// </summary>
        public void PrintDialog(PrintData speaker, PrintData content, Action callback)
        {
            onFinish_callback = callback;
            button_next.onClick.AddListener(go_next);

            button_next.interactable = false;
            text_content.enabled = true;
            enabled = true;

            printer = GetPrinter() ;
            printer.SetPrintData(speaker.elements);
            text_speaker.text = printer.Print();
            printer.SetPrintData(content.elements);
        }
        
        /// <summary>
        /// 设置选项
        /// </summary>
        public void SetOptions(IEnumerable<PrintData> options, Action<int> action)
        {
            var index = 0;
            var tmpPrinter = GetPrinter();
            onSelect_callback = action;
            foreach (var option in options)
            {
                var child = optionsRoot.GetChild(index);
                child.gameObject.SetActive(true);
                var label = child.GetChild(0).GetComponent<TextMeshProUGUI>();
                tmpPrinter.SetPrintData(option.elements);
                label.text = tmpPrinter.Print();
                index++;
            }
        }
        
        private void print_end()
        {
            printer = null;
            button_next.interactable = true;
        }
        private void go_next()
        {
            text_speaker.text = string.Empty;
            text_content.text = string.Empty;
            button_next.interactable = false;
            button_next.onClick.RemoveListener(go_next);
            var callback = onFinish_callback;
            onFinish_callback = null;
            callback?.Invoke();
        }
        private void select_finish(int index)
        {
            Debug.Log(index);
            for (var i = 0; i < optionsRoot.childCount; i++)
            {
                var child = optionsRoot.GetChild(i);
                child.gameObject.SetActive(false);
            }
            var action = onSelect_callback;
            onSelect_callback = null;
            action?.Invoke(index);
        }
    }
}