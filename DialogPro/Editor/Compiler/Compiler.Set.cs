namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 赋值语句
        /// </summary>
        private static void compile__set(BlockData data)
        {
            var line = data.curtLine;
            var formula = compile_formula(line);
            data.results.Append(Keyword.O_Set);
            data.results.Append("$");
            data.results.Append(formula);
            data.results.Append("\n");
            data.lineCount++;
        }
    }
}