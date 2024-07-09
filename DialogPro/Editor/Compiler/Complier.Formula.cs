using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DialogPro
{
    internal static partial class Compiler
    {
        /// <summary>
        /// 编译表达式
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>逆波兰表达式</returns>
        private static string compile_formula(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            //去除多余的空格
            var results = new List<string>();
            var rs = new Regex(@" {1,}", RegexOptions.IgnoreCase);
            var tmp = rs.Replace(source, " ").Replace(" ", "_");
            if (string.IsNullOrEmpty(tmp)) return string.Empty;

            formula_reorder(tmp, results);

            //检查
            var varCount = 0;
            foreach (var m in results)
            {
                var isKeyWord = Keyword.KeyWords.Any(keyWord => m.Equals(keyWord));
                if (isKeyWord && --varCount < 0) throw_error("[表达式格式错误]0");
                if (!isKeyWord)
                {
                    if (m.Contains("(")) throw_error("[表达式格式错误]1");
                    if (m.Contains(")")) throw_error("[表达式格式错误]2");
                    if (m.Contains("/")) throw_error("[表达式格式错误]3");
                    varCount++; //是变量名或字面量
                }
            }

            if (varCount != 1) throw_error("[表达式格式错误]5");

            var sBuilder = new StringBuilder();
            foreach (var m in results)
            {
                if (sBuilder.Length != 0) sBuilder.Append("$");
                sBuilder.Append(m);
            }

            return sBuilder.ToString();
        }

        /// <summary>
        /// 将表达式转化为后缀表达式
        /// </summary>
        private static void formula_reorder(string source, ICollection<string> results)
        {
            if (string.IsNullOrEmpty(source)) return;
            var reader = new Reader(source);
            // has bracket
            if (reader.ReadTo(LeftBracket, out var tmpLeft))
            {
                if (reader.ReadFormTo(LeftBracket, RightBracket, out var tmpRight))
                {
                    //bracket content:
                    formula_reorder(tmpRight, results);

                    //bracket content left:
                    formula_reorder(tmpLeft, results);

                    //bracket content right:
                    reader.ReadToEnd(out tmpRight);
                    formula_reorder(tmpRight, results);
                    return;
                }

                throw_error("[表达式括号缺失]");
            }

            // has key words
            foreach (var level in KeyWordLevels)
            {
                reader.Reset();
                if (reader.ReadToAny(level, out var kw, out tmpLeft))
                {
                    //key word right:
                    reader.ReadToEnd(out var tmpRight);
                    formula_reorder(tmpRight, results);

                    //key word left:
                    formula_reorder(tmpLeft, results);

                    //key word:
                    results.Add(kw.Replace("_", ""));
                    return;
                }
            }

            // no key words
            if (reader.ReadToEnd(out tmpLeft))
            {
                var value = tmpLeft.Replace("_", "");
                if (string.IsNullOrEmpty(value)) return;
                results.Add(value);
            }
        }
        
        private const string LeftBracket = "(_";
        private const string RightBracket = "_)";
        private const string Equal = "_==_";
        private const string NEql = "_!=_";
        private const string GrEql = "_>=_";
        private const string SrEql = "_<=_";
        private const string Gr = "_>_";
        private const string Sr = "_<_";
        private const string Add = "_+_";
        private const string Sub = "_-_";
        private const string Mul = "_*_";
        private const string Div = "_/_";
        private const string Or = "_||_";
        private const string And = "_&&_";
        private const string Set = "_=_";
        private const string AddSet = "_+=_";
        private const string SubSet = "_-=_";
        private const string MulSet = "_*=_";
        private const string DivSet = "_/=_";

        private static readonly string[][] KeyWordLevels =
        {
            new[] { Set, AddSet, SubSet, MulSet, DivSet, },
            new[] { Equal, NEql, GrEql, SrEql, Gr, Sr, },
            new[] { Or, And, }, new[] { Add, Sub, },
            new[] { Mul, Div, },
        };
    }
}