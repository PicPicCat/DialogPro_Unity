using System.Text;

namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 调用语句
        /// </summary>
        private static void compile__call(BlockData data)
        {
            var line = data.curtLine;
            var callBody = line.Substring(3, line.Length - 3);
            foreach (var pair in data.macros_call)
                callBody = callBody.Replace(pair.Key, pair.Value);

            var fvs = callBody.Split("|");

            var fun_name = fvs[0].Trim();
            if (string.IsNullOrEmpty(fun_name))
                throw_error("[函数名为空]");

            var callData = new StringBuilder();
            callData.Append(Keyword.O_Call);
            callData.Append("$");
            callData.Append(fun_name);

            for (var i = 1; i < fvs.Length; i++)
            {
                var sp = fvs[i];
                var param = sp.Trim();
                if (string.IsNullOrEmpty(param)) throw_error("[调用参数为空]");
                var kv = param.Split("=");
                if (kv.Length != 2) throw_error("[参数格式错误]");
                var param_name = kv[0].Trim();
                var param_value = kv[1].Trim();
                if (string.IsNullOrEmpty(param_name)) throw_error("[参数名称为空]");
                if (string.IsNullOrEmpty(param_value)) throw_error("[参数值为空]");
                callData.Append("$");
                callData.Append(param_name);
                callData.Append("$");
                callData.Append(param_value);
            }

            data.results.Append(callData);
            data.results.Append("\n");
            data.lineCount++;
        }
    }
}