using System;
using System.Text;
using UnityEngine;

namespace DialogPro.UI
{
    /// <summary>
    /// 对话打印器
    /// </summary>
    [Serializable]
    public class DialogPrinter
    {
        [SerializeField] private int ptr;
        [SerializeField] private float timer;
        [SerializeField] private string textBuf = string.Empty;
        [SerializeField] private PrintElement[] elements = Array.Empty<PrintElement>();
        
        /// <summary>
        /// 设置打印数据
        /// </summary>
        public void SetPrintData(PrintElement[] printElements)
        {
            ptr = 0;
            timer = 0;
            textBuf = string.Empty;
            elements = printElements;
        }
        
        /// <summary>
        /// 获取打印数据
        /// </summary>
        public PrintElement[] GetPrintData() => elements;
        
        /// <summary>
        /// 清空打印内容
        /// </summary>
        public void ClearPrintData()
        {
            ptr = 0;
            timer = 0;
            textBuf = string.Empty;
            elements = Array.Empty<PrintElement>();
        }
        /// <summary>
        /// 重置打印器到初始位置
        /// </summary>
        public void ReSet()
        {
            ptr = 0;
            timer = 0;
            textBuf = string.Empty;
        }

        /// <summary>
        /// 全部打印
        /// </summary>
        /// <returns>打印结果</returns>
        public string Print()
        {
            PrintCurtText(true);
            timer = 0;
            return textBuf;
        }

        /// <summary>
        /// 按增量打印
        /// </summary>
        /// <param name="delta">字符数的增量</param>
        /// <param name="finish">是否完成打印</param>
        /// <returns>打印结果</returns>
        public string Print(float delta, out bool finish)
        {
            timer += delta;
            finish = PrintCurtText(false);
            return textBuf;
        }

        private bool PrintCurtText(bool printAll)
        {
            var finish = true;
            var bufStr = new StringBuilder();
            while (ptr >> 1 < elements.Length)
            {
                if (printAll) timer = float.MaxValue;
                var printType = (ptr & 1) == 0 ? PrintType.TextBlock : PrintType.NoteBlock;
                var goNext = Print(printType, bufStr);
                if (goNext)
                {
                    ptr++;
                    continue;
                }

                finish = false;
                break;
            }
            textBuf = bufStr.ToString();
            return finish;
        }

        private enum PrintType
        {
            TextBlock,
            NoteBlock,
        };

        private bool Print(PrintType type ,StringBuilder bufStr)
        {
            return type switch
            {
                PrintType.TextBlock => PrintTextBlock(elements[ptr >> 1], ref timer, bufStr),
                PrintType.NoteBlock => PrintNoteBlock(elements[ptr >> 1], ref timer, bufStr),
                _ => true,
            };
        }

        protected virtual bool PrintTextBlock(PrintElement element, ref float charNum,StringBuilder bufStr)
        {
            if (charNum > element.text.Length)
            {
                bufStr.Append(element.text);
                charNum -= element.text.Length;
                return true;
            }
            var tmpLen = (int)charNum;
            if (tmpLen == 0) return false;
            bufStr.Append(element.text[..tmpLen]);
            return false;
        }

        protected virtual bool PrintNoteBlock(PrintElement element, ref float charNum, StringBuilder bufStr)
        {
            return true;
        }
    }
}