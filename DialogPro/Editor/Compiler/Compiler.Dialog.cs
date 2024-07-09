namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 对话语句
        /// </summary>
        private static void compile__dialog(BlockData data)
        {
            var line = data.curtLine;
            var reader = new Reader(line);
            if (!reader.ReadTo("#", out var speaker_content))
                throw_error("[对话者名称为空]");
            if (!reader.ReadToEnd(out var dialog_content))
                throw_error("[对话内容为空]");

            //ID；当前行号
            var dialogID = data.curtLineIndex;

            data.results.Append(Keyword.O_Speaker);
            data.results.Append("$");
            data.results.Append(dialogID);
            data.results.Append("\n");
            data.lineCount++;

            compile_printData(data, speaker_content);
            compile_printData(data, dialog_content);
        }
    }
}