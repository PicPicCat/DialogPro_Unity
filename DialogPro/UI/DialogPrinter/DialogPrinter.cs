using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace DialogPro.UI
{
    /// <summary>
    /// 对话打印器支持 rich-text 可扩展功能
    /// </summary>
    public class DialogPrinter
    {
        private float timer;
        private int ptr;
        private PrintState state;
        private string richText_color;
                
        private Color baseColor;
        private float baseSize;
        private string textBuf;
        
        private readonly Queue<PrintElement> queue;
        private readonly StringBuilder str_builder;
        private readonly StringBuilder richText_builder;
        private readonly Dictionary<string, INoteFunction> noteFunctions;
        
        /// <summary>
        /// 对话打印器
        /// </summary>
        public DialogPrinter()
        {
            ptr = 0;
            timer = 0;
            queue = new Queue<PrintElement>();
            str_builder = new StringBuilder();
            richText_builder = new StringBuilder();
            noteFunctions = new Dictionary<string, INoteFunction>();
            richText_color = string.Empty;
            state = PrintState.None;
        }
        
        /// <summary>
        /// 设置打印数据
        /// </summary>
        public void SetPrintData(PrintData data)
        {
            Clear();
            foreach (var element in data.elements)
                queue.Enqueue(element);
            state = PrintState.Text;
        }
        
        /// <summary>
        /// 设置拓展函数
        /// </summary>
        public void SetFunction(INoteFunction function)
        {
            noteFunctions.Add(function.Name,function);
        }
        
        /// <summary>
        /// 清空打印内容
        /// </summary>
        public void Clear()
        {
            ptr = 0;
            timer = 0;
            queue.Clear();

            str_builder.Clear();
            richText_builder.Clear();
            richText_color = string.Empty;
            textBuf = string.Empty;
            state = PrintState.None;
        }


        /// <summary>
        /// 全部打印
        /// </summary>
        /// <param name="tmpText"></param>
        /// <param name="skipCall">是否跳过注解函数调用</param>
        public void Print(TMP_Text tmpText, bool skipCall = false)
        {
            if (state == PrintState.None)
            {
                tmpText.text = textBuf;
                return;
            }
            baseColor = tmpText.color;
            baseSize = tmpText.fontSize;
            print_curt_text(true, skipCall);
            tmpText.text = textBuf;
            timer = 0;
        }

        /// <summary>
        /// 按增量打印
        /// </summary>
        /// <param name="delta">字符数的增量</param>
        /// <param name="tmpText"></param>
        /// <param name="skipCall">是否跳过注解函数调用</param>
        /// <returns>是否打印结束</returns>
        public bool Print(float delta, TMP_Text tmpText, bool skipCall = false)
        {
            if (state == PrintState.None)
            {
                tmpText.text = textBuf;
                return false;
            }
            timer += delta;
            if (timer < 1)
            {
                tmpText.text = textBuf;
                return false;
            }
            baseColor = tmpText.color;
            baseSize = tmpText.fontSize;
            var end = state switch
            {
                PrintState.Text => print_curt_text(false, skipCall),
                PrintState.Notes => print_curt_notes(skipCall),
                _ => false
            };
            tmpText.text = textBuf;
            return end;
        }

        private bool print_curt_text(bool printAll, bool skipCall)
        {
            const int MaxNum = 10000000;
            var data = queue.Peek();
            var char_num = printAll ? MaxNum : (int)timer;
            while (char_num >= 1)
            {
                if (ptr + char_num < data.text.Length) //剩余长度足够
                {
                    var mStr = data.text.Substring(ptr, char_num);
                    str_builder.Append(mStr);
                    ptr += char_num;
                    char_num = 0;
                    timer = 0;
                }
                else //剩余长度不足
                {
                    var rl = data.text.Length - ptr;
                    if (rl > 0)
                    {
                        var mStr = data.text.Substring(ptr, rl);
                        str_builder.Append(mStr);
                        char_num -= rl; //减去剩余长度
                    }

                    ptr = 0;
                    var notes_cost = this.notes_cost(data);
                    timer = char_num - notes_cost;
                    char_num = printAll ? MaxNum : (int)timer;

                    if (char_num >= 0) //足够打印notes
                    {
                        queue.Dequeue();
                        notes_function(data,skipCall);
                        if (queue.Count == 0) //end
                        {
                            print_tmp();
                            state = PrintState.None;
                            return true;
                        }

                        data = queue.Peek();
                    }
                    else state = PrintState.Notes; //不足
                }
            }
            print_tmp();
            return false;
        }
        
        private bool print_curt_notes(bool skipCall)
        {
            var data = queue.Dequeue();
            notes_function(data,skipCall);
            if (queue.Count == 0)
            {
                print_tmp();
                state = PrintState.None;
                return true;
            }
            print_tmp();
            state = PrintState.Text;
            return false;
        }

        private void print_tmp()
        {
            var str = str_builder.ToString();
            if (richText_builder.Length != 0) str += richText_builder.ToString();
            textBuf = str;
        }

        protected enum PrintState
        {
            None,
            Text,
            Notes
        }

        private float notes_cost(PrintElement element)
        {
            var notes = element.notes;
            if (notes.dic.ContainsKey("stop") &&
                notes.dic.TryGetValue("time", out var stop_str) &&
                float.TryParse(stop_str, out var stop))
            {
                return stop;
            }

            if (notes.dic.ContainsKey("img")) return 1.0f;

            if (notes.dic.ContainsKey("call") &&
                notes.dic.TryGetValue("function", out var name))
            {
                if (noteFunctions.TryGetValue(name, out var function))
                    return function.Cost(this, element);
            }

            return 0;
        }

        /// <summary>
        /// 设置图片 img=imgName
        /// 设置字体大小和颜色 begin_rich_text, size=00.00%, color=#FFFFFF
        /// </summary>
        private void notes_function(PrintElement element, bool skipCall)
        {
            var notes = element.notes;
            if (notes.dic.TryGetValue("img", out var imgName))
            {
                str_builder.Append("<sprite name=");
                str_builder.Append("\"");
                str_builder.Append(imgName);
                str_builder.Append("\"");
                if (!notes.dic.TryGetValue("color", out var color_str))
                {
                    if (!string.IsNullOrEmpty(richText_color))
                    {
                        color_str = richText_color;
                        str_builder.Append("\"");
                    }
                    else color_str = "#" + ColorUtility.ToHtmlStringRGBA(baseColor);
                }

                str_builder.Append(" ");
                str_builder.Append("color=");
                str_builder.Append(color_str);
                str_builder.Append(">");
                return;
            }

            if (notes.dic.ContainsKey("begin_rich_text"))
            {
                begin_richText(element);
                return;
            }

            if (notes.dic.ContainsKey("end_rich_text"))
            {
                end_richText();
                return;
            }

            if (skipCall) return;
            if (notes.dic.TryGetValue("call", out var name))
            {
                if (noteFunctions.TryGetValue(name, out var function))
                    function.Handle(this, element);
            }
        }

        private void begin_richText(PrintElement element)
        {
            end_richText();
            var notes = element.notes;
            if (notes.dic.TryGetValue("size", out var size_str) &&
                float.TryParse(size_str, out var size))
            {
                size *= baseSize;
                size_str = size.ToString("0.00");
                str_builder.Append("<size=").Append(size_str).Append(">");
                richText_builder.Append("</size>");
            }

            if (notes.dic.TryGetValue("color", out var color_str))
            {
                richText_color = color_str;
                str_builder.Append("<color=").Append(color_str).Append(">");
                richText_builder.Append("</color>");
            }
        }

        private void end_richText()
        {
            if (richText_builder.Length == 0) return;
            str_builder.Append(richText_builder);
            richText_builder.Clear();
            richText_color = string.Empty;
        }
        
    }
}