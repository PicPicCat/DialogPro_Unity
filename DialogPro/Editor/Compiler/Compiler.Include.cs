using System;
using System.Collections.Generic;
using System.IO;

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
            var text = string.Empty;
            var exists = false;
            foreach (var path in data.include_paths)
            {
                var p = path + "/" + fileName + ".dh.txt";
                if (!File.Exists(p)) continue;
                text = File.ReadAllText(p);
                exists = true;
                break;
            }

            if (!exists) throw_error("[未找到包含文件]");

            var ls = new List<string>(text.Split("\n"));
            var scriptData = new ScriptData(ls, Array.Empty<string>());
            var child = new BlockData(scriptData);
            compile_block(child, 0, ls.Count);
            
            if (child.lineCount >= 1)
            {
                data.results.Append(child.results);
                data.lineCount += child.lineCount;
            }
            data.macros_call.AddRange(child.macros_call);
            data.macros_note.AddRange(child.macros_note);
        }
    }
}