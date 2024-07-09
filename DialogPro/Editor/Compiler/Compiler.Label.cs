using UnityEngine;

namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 标签语句
        /// </summary>
        private static void compile__label(BlockData data)
        {
            var line = data.curtLine;
            var reader = new Reader(line);
            reader.Read(1);
            if (!reader.ReadTo("}", out var condition))
                throw_error("[文段块缺少和`{`匹配的`}`]");
            foreach (var pair in data.macros_condition)
                condition = condition.Replace(pair.Key, pair.Value);

            if (reader.ReadTo("(", out _)) //选项分支
            {
                if (!reader.ReadTo(")", out var label))
                    throw_error("[文段块缺少和`(`匹配的`)`]");
                if (string.IsNullOrEmpty(label))
                    throw_error("[标签名称为空]");

                var formula = "1";
                if (!string.IsNullOrEmpty(condition))
                    formula = compile_formula(condition);

                data.results.Append(Keyword.O_OptionsHead);
                data.results.Append("$");
                data.results.Append(formula);
                data.results.Append("\n");
                data.lineCount++;
                data.requireBlock = true;

                compile_printData(data, label);
            }
            else //条件分支
            {
                if (string.IsNullOrEmpty(condition))
                    throw_error("[条件表达式为空]");
                var formula = compile_formula(condition);

                data.results.Append(Keyword.O_Condition);
                data.results.Append("$");
                data.results.Append(formula);
                data.results.Append("\n");
                data.lineCount++;
                data.requireBlock = true;
            }
        }
    }
}