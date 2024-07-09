using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DialogPro
{
    /// <summary>
    /// 函数调用数据
    /// </summary>
    public readonly struct CallData
    {
        /// <summary>
        /// 函数名称
        /// </summary>
        public readonly string function;

        /// <summary>
        /// 参数键值对
        /// </summary>
        public readonly KeyValues pairs;

        public CallData(string function, KeyValues pairs)
        {
            this.function = function;
            this.pairs = pairs;
        }
    }

    /// <summary>
    /// 打印文本单元
    /// </summary>
    public readonly struct PrintElement
    {
        /// <summary>
        /// 打印文本
        /// </summary>
        public readonly string text;

        /// <summary>
        /// 注解键值对
        /// </summary>
        public readonly KeyValues notes;

        public PrintElement(string text,
            KeyValues notes)
        {
            this.text = text;
            this.notes = notes;
        }
    }

    /// <summary>
    /// 打印文本数据
    /// </summary>
    public readonly struct PrintData 
    {
        public readonly IReadOnlyList<PrintElement> elements;

        public PrintData(IReadOnlyList<PrintElement> elements)
        {
            this.elements = elements;
        }
        
        public override string ToString()
        {
            return elements.Aggregate("", (current, element) => current + element.text);
        }
    }

    /// <summary>
    /// 键值对
    /// </summary>
    public readonly struct KeyValues
    {
        public readonly IReadOnlyDictionary<string, string> dic;

        public KeyValues(IEnumerable<KeyValuePair<string, string>> data)
        {
            dic = new Dictionary<string, string>(data);
        }
    }
    
    
    public static class Keyword
    {
        public const string O_Call = "cal__";
        public const string O_Set = "set__";
        public const string O_PrintData = "ptd__";
        public const string O_Array = "ary__";
        public const string O_Speaker = "spk__";
        public const string O_BlockHead = "boh__";
        public const string O_OptionsHead = "oph__";
        public const string O_Condition = "con__";

        public const string Or = "||";
        public const string And = "&&";
        public const string Equal = "==";
        public const string NEql = "!=";
        public const string GrEql = ">=";
        public const string SrEql = "<=";
        public const string Gr = ">";
        public const string Sr = "<";

        public const string Set = "=";
        public const string AddSet = "+=";
        public const string SubSet = "-=";
        public const string MulSet = "*=";
        public const string DivSet = "/=";

        public const string Add = "+";
        public const string Sub = "-";
        public const string Mul = "*";
        public const string Div = "/";

        public static readonly string[] KeyWords =
        {
            Or, And, Equal, NEql, GrEql, SrEql,
            Gr, Sr, Add, Sub, Mul, Div,
            Set, AddSet, SubSet, MulSet, DivSet
        };
    }
}