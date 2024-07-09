using System.Collections.Generic;

namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 宏定义语句
        /// </summary>
        private static void compile__macro(BlockData data)
        {
            var line = data.curtLine;
            var reader = new Reader(line);
            List<KeyValuePair<string, string>> ls = null;
            if (reader.ReadIs("#==")) ls = data.macros_call;
            else if (reader.ReadIs("#<>")) ls = data.macros_note;
            else if (reader.ReadIs("#{}")) ls = data.macros_condition;
            else throw_error("[定义类型错误]");

            if (!reader.ReadTo("{", out var left)) throw_error("[定义对象格式错误]");
            if (!reader.ReadTo("}", out var right)) throw_error("[定义对象格式错误]");
            if (string.IsNullOrEmpty(left)) throw_error("[定义主体为空]");
            if (string.IsNullOrEmpty(right)) throw_error("[定义对象为空]");
            var pair = new KeyValuePair<string, string>(left, right);
            for (var i = 0; i < ls.Count; i++)
            {
                if (!ls[i].Key.Equals(pair.Key)) continue;
                ls.RemoveAt(i);
                break;
            }

            ls.Add(pair);
            ls.Sort((pair_a, pair_b) => -pair_a.Key.Length.CompareTo(pair_b.Key.Length));
        }
    }
}