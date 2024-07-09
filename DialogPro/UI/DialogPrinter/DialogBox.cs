using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DialogPro.UI
{
    public class DialogBox : MonoBehaviour
    {
        [Tooltip("对话内容文本")] public TextMeshProUGUI text_content;
        [Tooltip("讲述者名称文本")] public TextMeshProUGUI text_speaker;
        [Tooltip("继续推进的按钮")] public Button button_next;
        [Tooltip("打印速度 字符每秒")] public float printSpeed;
        [Tooltip("选项列表的根节点")] public RectTransform optionsRoot;
        
        private Action<int> onSelect_callback;
        private Action onFinish_callback;
        private DialogPrinter printer;
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
            if (printer.Print(delta, text_content)) print_end();
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

            printer = new DialogPrinter();
            printer.SetPrintData(speaker);
            printer.Print(text_speaker);
            printer.SetPrintData(content);
        }
        
        /// <summary>
        /// 设置选项
        /// </summary>
        public void SetOptions(IEnumerable<PrintData> options, Action<int> action)
        {
            var index = 0;
            var tmpPrinter = new DialogPrinter();
            onSelect_callback = action;
            foreach (var option in options)
            {
                var child = optionsRoot.GetChild(index);
                child.gameObject.SetActive(true);
                var label = child.GetChild(0).GetComponent<TextMeshProUGUI>();
                tmpPrinter.SetPrintData(option);
                tmpPrinter.Print(label);
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