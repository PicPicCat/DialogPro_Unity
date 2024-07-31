using System;
using System.Collections.Generic;

namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 包含语句
        /// </summary>
        private static void compile__include(BlockData data)
        {
            var line = data.curtLine;
            var sp = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length != 2) throw_error("[格式错误]");
            var fileName = sp[^1];
            if (!data.includes.TryGetValue(fileName, out var includeStr))throw_error("[未找到包含文件]");
            var include = new List<string>(includeStr.Split("\n"));
            var scriptData = new ScriptData(include);
            var includeBlock = new BlockData(scriptData);
            compile_block(includeBlock, 0, include.Count);
            
            if (includeBlock.lineCount >= 1)
            {
                data.results.Append(includeBlock.results);
                data.lineCount += includeBlock.lineCount;
            }
            data.macros_call.AddRange(includeBlock.macros_call);
            data.macros_note.AddRange(includeBlock.macros_note);
        }
    }
}