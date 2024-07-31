using System;
using System.Collections.Generic;
using System.Text;

namespace DialogPro
{
    internal static partial class Compiler
    {
        
        /// <summary>
        /// 编译打印信息
        /// </summary>
        private static void compile_printData(BlockData data, string source)
        {
            var reader = new Reader(source);
            var list = new List<PrintElement>();
            var values = new List<KeyValuePair<string, string>>();

            while (reader.ReadTo("<", out var content))
            {
                values.Clear();
                if (!reader.ReadTo(">", out var block_str)) //获取注解块
                    throw_error("[注解块缺少匹配的<>]");

                foreach (var pair in data.macros_note)
                    block_str = block_str.Replace(pair.Key, pair.Value);

                if (!string.IsNullOrEmpty(block_str)) //处理非空的注解块
                {
                    foreach (var split_values in block_str.Split("|"))
                    {
                        var pairs = split_values.Trim().Split("=");
                        var key = "";
                        var value = "0";
                        switch (pairs.Length)
                        {
                            case 1:
                                key = pairs[0].Trim();
                                break;
                            case 2:
                                key = pairs[0].Trim();
                                value = pairs[1].Trim();
                                break;
                            default:
                                throw_error("[注解格式错误]");
                                break;
                        }

                        values.Add(new KeyValuePair<string, string>(key, value));
                    }
                }

                list.Add(new PrintElement(content, new KeyValues(values)));
            }

            //处理最后一个文段块
            if (reader.ReadToEnd(out var m))
            {
                list.Add(new PrintElement(m.Trim(),
                    new KeyValues(Array.Empty<KeyValuePair<string, string>>())));
            }

            data.results.Append(Keyword.O_PrintData);
            data.results.Append("$");
            data.results.Append(printData_ToString(list[0]));
            data.results.Append("\n");
            data.lineCount++;

            for (var i = 1; i < list.Count; i++)
            {
                data.results.Append(Keyword.O_Array);
                data.results.Append("$");
                data.results.Append(printData_ToString(list[i]));
                data.results.Append("\n");
                data.lineCount++;
            }
        }
        
        /// <summary>
        /// 将打印信息转化为字符串
        /// </summary>
        private static string printData_ToString(PrintElement element)
        {
            var builder = new StringBuilder();
            builder.Append(element.text);
            for (var i = 0; i < element.note.keys.Length; i++)
            {
                builder.Append("$");
                builder.Append(element.note.keys[i]);
                builder.Append("$");
                builder.Append(element.note.values[i]);
            }
            return builder.ToString();
        }
    }
}