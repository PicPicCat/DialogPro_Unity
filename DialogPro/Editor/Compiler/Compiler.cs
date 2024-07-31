using System;
using System.Text;
using System.Collections.Generic;

namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 编译DialogScript
        /// </summary>]
        public static string Compile(string source, IReadOnlyDictionary<string, string> includes)
        {
            try //编译整个文档
            {
                if (string.IsNullOrEmpty(source)) return string.Empty;
                var lines = new List<string>(source.Split(
                    new[] { Environment.NewLine }, StringSplitOptions.None)); //获取每一行
                var scriptData = new ScriptData(lines, includes);
                var blockData = new BlockData(scriptData);

                compile_block(blockData, 0, lines.Count);
                return blockData.results.ToString();
            }
            catch (ErrorException e)
            {
                throw new Exception(e.ToString());
            }
        }

        /// <summary>
        /// 获取文段块范围
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        private static int block_range(IReadOnlyList<string> lines, int start)
        {
            var level = 0;
            for (var index = start; index < lines.Count; index++)
            {
                var line = lines[index].Trim();
                if (line.Equals("{"))
                {
                    level++;
                    continue;
                }

                if (line.Equals("}"))
                {
                    level--;
                    if (level == 0) return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 编译块
        /// </summary>
        private static void compile_block(BlockData data, int start, int end)
        {
            for (var curt_pos = start; curt_pos < end; curt_pos++)
            {
                var line = data.lines[curt_pos].Trim();
                var lineIndex =curt_pos+ 1;
                if (string.IsNullOrEmpty(line)) continue; //剔除空行
                
                if (line.Equals("{")) //{ }文段块
                {
                    if (!data.requireBlock) throw_error("[文段块缺少与之匹配的标签]", lineIndex);
                    data.requireBlock = false;
                    var blockEnd = block_range(data.lines, curt_pos); //获取文段块的范围
                    if (blockEnd < 0) throw_error("[文段块缺少和`{`匹配的`}`]", lineIndex);

                    var block_start_index = data.lineCount; //记录当前行号
                    var child = new BlockData(data); //构造子块
                    compile_block(child, curt_pos + 1, blockEnd); //编译文段块
                    data.lineCount = child.lineCount + 1; //增加一行作为文段头
                    var block_line_count = data.lineCount - block_start_index; //子块的的行数
                    data.results.Append(Keyword.O_BlockHead + "$" + block_line_count + "\n"); //插入文段头 
                    data.results.Append(child.results); //插入子块文段头
                    curt_pos = blockEnd; //ptr置于子块之后
                    continue;
                }

                if (data.requireBlock)
                    throw_error("[标签缺少与之匹配的文段块]", lineIndex);

                var line_builder = new StringBuilder();
                line_builder.Append(line);
                while (curt_pos + 1 < data.lines.Count)
                {
                    var next_line = data.lines[curt_pos + 1].Trim();
                    if (string.IsNullOrEmpty(next_line)) curt_pos++; //跳过空行
                    else if (next_line[0].Equals('+')) //处理连接符
                    {
                        next_line = next_line.Substring(1, next_line.Length - 1).Trim();
                        line_builder.Append(next_line);
                        curt_pos++;
                    }
                    else break;
                }

                line = line_builder.ToString(); //获取行内容
                compile_line(data, line, lineIndex); //编译行内容
            }
        }

        /// <summary>
        /// 编译行
        /// </summary>
        private static void compile_line(BlockData data, string line, int lineIndex)
        {
            try
            {
                data.curtLineIndex = lineIndex;
                data.curtLine = line;
                var reader = new Reader(line);
                if (reader.ReadIs("---")) return;
                if (reader.ReadIs("#++")) compile(data, compile__include, "[包含语句错误]"); //宏定义语句
                else if (reader.ReadIs("#==")) compile(data, compile__macro, "[宏定义语句错误]"); //宏定义语句
                else if (reader.ReadIs("#<>")) compile(data, compile__macro, "[宏定义语句错误]"); //宏定义语句
                else if (reader.ReadIs("#{}")) compile(data, compile__macro, "[宏定义语句错误]"); //宏定义语句
                else if (reader.ReadIs("===")) compile(data, compile__call, "[调用语句错误]"); //调用语句
                else if (reader.ReadIs("*")) compile(data, compile__set, "[赋值语句错误]"); //赋值语句
                else if (reader.ReadIs("@")) compile(data, compile__set, "[赋值语句错误]"); //赋值语句
                else if (reader.ReadIs("{")) compile(data, compile__label, "[标签语句错误]"); //标签语句
                else if (reader.ReadHas("#")) compile(data, compile__dialog, "[对话语句错误]"); //对话语句
                else throw_error("[未定义的格式]");
            }
            catch (ErrorException e)
            {
                throw_error(e, lineIndex);
            }
        }

        /// <summary>
        /// 调用编译函数
        /// </summary>
        private static void compile(BlockData data, 
            CompileFunction function, string error_info)
        {
            try
            {
                function?.Invoke(data);
            }
            catch (ErrorException e)
            {
                throw_error(e, error_info);
            }
        }

        private delegate void CompileFunction(BlockData data);
    }
}