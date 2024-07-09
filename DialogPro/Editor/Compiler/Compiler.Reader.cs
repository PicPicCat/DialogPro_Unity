using System;

namespace DialogPro
{
    internal static partial class Compiler
    {
        private class Reader
        {
            public Reader(string source)
            {
                Source = source.Trim();
                curt_pos = 0;
            }

            /// <summary>
            /// 将内容指针置为0
            /// </summary>
            public void Reset()
            {
                curt_pos = 0;
            }

            /// <summary>
            /// 内容指针（当前内容的开始位置 初始值为0）
            /// </summary>
            private int curt_pos { get; set; }

            /// <summary>
            /// 源文段
            /// </summary>
            private string Source { get; }

            /// <summary>
            /// 是否为最后一行
            /// </summary>
            private bool IsEnd
            {
                get => curt_pos >= Source.Length;
            }


            /// <summary>
            /// 读取一段文本（不返回读取内容）
            /// </summary>
            /// <param name="len">文本长度</param>
            /// <returns>剩余长度是否足够</returns>
            public void Read(int len)
            {
                if (curt_pos + len >= Source.Length)
                {
                    curt_pos = Source.Length;
                    return;
                }

                curt_pos += len;
            }


            /// <summary>
            /// 读取一段文本（不移动移动内容指针）
            /// </summary>
            /// <param name="len">文本长度</param>
            /// <param name="s">读取内容（已trim）</param>
            /// <returns>剩余长度是否足够</returns>
            private bool TryRead(int len, out string s)
            {
                if (curt_pos + len > Source.Length) //剩余长度不够
                {
                    len = Source.Length - curt_pos;
                    //截取 ptr,length
                    s = Source.Substring(curt_pos, len).Trim();
                    return false;
                }

                //截取 ptr,ptr+len
                s = Source.Substring(curt_pos, len).Trim();
                return true;
            }

            /// <summary>
            /// 读取一个字段（移动内容指针）
            /// </summary>
            /// <param name="mark">要匹配的字段</param>
            /// <returns>是否匹配</returns>
            public bool ReadIs(string mark)
            {
                if (TryRead(mark.Length, out var s) && s.Equals(mark))
                {
                    Read(mark.Length);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 尝试读取直到匹配一个字段（不移动移动内容指针）
            /// </summary>
            /// <param name="mark">要匹配的字段</param>
            /// <param name="s">读取的内容</param>
            /// <param name="offset">指针应该移动的偏移量（如果匹配）</param>
            /// <returns>是否匹配（否则不读取任何内容）</returns>
            private bool TryReadTo(string mark, out string s, out int offset)
            {
                var mark_pos = Source.IndexOf(mark, curt_pos, StringComparison.Ordinal);
                if (mark_pos < 0)
                {
                    s = string.Empty;
                    offset = 0;
                    return false;
                }

                s = Source.Substring(curt_pos, mark_pos - curt_pos).Trim();
                offset = mark_pos - curt_pos + mark.Length;
                return true;
            }

            /// <summary>
            /// 读取直到匹配一个字段（移动移动内容指针）
            /// </summary>
            /// <param name="mark">要匹配的字段</param>
            /// <param name="s">读取的内容</param>
            /// <returns>是否匹配（否则不读取任何内容）</returns>
            public bool ReadTo(string mark, out string s)
            {
                if (!TryReadTo(mark, out s, out var offset)) return false;
                curt_pos += offset;
                return true;
            }

            /// <summary>
            /// 读取直到匹配一个字段（移动移动内容指针）
            /// </summary>
            /// <param name="mark">要匹配的字段</param>
            /// <returns>是否匹配（否则不读取任何内容）</returns>
            public bool ReadHas(string mark)
            {
                if (!TryReadTo(mark, out _, out var offset)) return false;
                curt_pos += offset;
                return true;
            }

            /// <summary>
            /// 读取直到匹配一个字段（移动内容指针）
            /// </summary>
            /// <param name="marks">要匹配的字段集</param>
            /// <param name="mark">匹配的字段</param>
            /// <param name="s">读取的内容</param>
            /// <returns>是否匹配（否则不读取任何内容）</returns>
            public bool ReadToAny(string[] marks, out string mark, out string s)
            {
                mark = string.Empty;
                s = string.Empty;
                if (IsEnd) return false;

                var max_mark_len = 0;
                var offset = 0;
                foreach (var m in marks)
                {
                    if (m.Length <= max_mark_len) continue;
                    if (!TryReadTo(m, out var tmp, out var tmpOffset)) continue;
                    mark = m;
                    offset = tmpOffset;
                    s = tmp;
                    max_mark_len = m.Length;
                }

                if (max_mark_len == 0)
                {
                    mark = string.Empty;
                    s = string.Empty;
                    return false;
                }

                curt_pos += offset;
                return true;
            }

            /// <summary>
            /// 读取嵌套的文段块（默认当前位置为第一个开始符之后）（移动内容指针）
            /// </summary>
            /// <param name="start">文段块开始符</param>
            /// <param name="end">文段块结束符</param>
            /// <param name="s">读取的内容</param>
            /// <returns>是否有匹配文段块</returns>
            public bool ReadFormTo(string start, string end, out string s)
            {
                s = string.Empty;
                if (start.Equals(end)) return false;
                var block_start_pos = curt_pos; //block的第一个字符
                var end_pos = curt_pos;

                var level = 1;
                while (level != 0)
                {
                    if (end_pos >= Source.Length) break;
                    var next_end_pos = Source.IndexOf(end, end_pos, StringComparison.Ordinal);
                    var next_start_pos = Source.IndexOf(start, end_pos, StringComparison.Ordinal);
                    if (next_end_pos < 0 && next_start_pos < 0) break;
                    if (next_end_pos < 0) next_end_pos = Source.Length;
                    if (next_start_pos < 0) next_start_pos = Source.Length;

                    if (next_end_pos < next_start_pos) //is end
                    {
                        end_pos = next_end_pos + end.Length;
                        level--; //减少一层
                    }
                    else if (next_start_pos < next_end_pos) //is start
                    {
                        end_pos = next_start_pos + start.Length;
                        level++; //增加一层
                    }
                }

                if (level != 0) return false;
                var block_end_pos = end_pos - end.Length; //end的第一个字符
                s = Source.Substring(block_start_pos, block_end_pos - block_start_pos).Trim();
                curt_pos = end_pos;
                return true;
            }

            /// <summary>
            /// 读取剩余文段
            /// </summary>
            /// <param name="s">读取的内容</param>
            /// <returns>是否有剩余文段</returns>
            public bool ReadToEnd(out string s)
            {
                s = string.Empty;
                if (IsEnd) return false;
                s = Source.Substring(curt_pos, Source.Length - curt_pos).Trim();
                curt_pos = Source.Length;
                return true;
            }
        }
    }
}