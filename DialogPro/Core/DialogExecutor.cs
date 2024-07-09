using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DialogPro
{
    /// <summary>
    /// Dialog执行器
    /// </summary>
    public class DialogExecutor
    {
        private readonly int start_pos;
        private readonly int end_pos;
        private readonly IReadOnlyList<string> exe_lines;
        private readonly Provider provider;
        private readonly List<DialogExecutor> executorList;
        private Action on_end_callback;
        private int curt_pos;

        public DialogExecutor(string dialogStr, IDialogHandler handler)
        {
            provider = new Provider(handler);
            executorList = new List<DialogExecutor>();
            exe_lines = dialogStr.Split("\n");
            start_pos = 0;
            end_pos = exe_lines.Count;
        }

        private DialogExecutor(DialogExecutor parent, int start, int end)
        {
            provider = parent.provider;
            executorList = new List<DialogExecutor>();
            exe_lines = parent.exe_lines;
            start_pos = start;
            end_pos = end;
        }

        public void Start(Action callBack)
        {
            curt_pos = start_pos;
            on_end_callback = callBack;
            execute_next();
        }

        public Dictionary<string, string> LocalVals
        {
            get => provider.local_values;
            set => provider.local_values = value;
        }

        private void execute_next()
        {
            Action action = null;
            while (action == null) action = execute_line();
            action.Invoke();
        }

        private Action execute_line()
        {
            if (check_end()) return () => on_end_callback?.Invoke();
            return read_head() switch
            {
                Keyword.O_Set => exe_set(),
                Keyword.O_Call => exe_call(),
                Keyword.O_Speaker => exe_speak(),
                Keyword.O_OptionsHead => exe_options(),
                Keyword.O_Condition => exe_condition(),
                _ => null
            };
        }
        
        
        private Action exe_set()
        {
            var values = exe_lines[curt_pos++].Split("$"); //读取并移动
            read_formula(values);
            return null;
        }

        private Action exe_call()
        {
            var values = exe_lines[curt_pos++].Split("$"); //读取并移动
            var callData = new CallData(values[1], GetKeyValues(2, values));
            return () => provider.CallFunction(callData, execute_next);
        }

        private Action exe_speak()
        {
            var id = exe_lines[curt_pos++].Split("$")[1];
            var index = int.Parse(id);
            var speaker = read_print_data();
            var content = read_print_data();
            return () => provider.PrintDialog(index, speaker, content, execute_next);
        }

        private Action exe_condition()
        {
            var values = exe_lines[curt_pos++].Split("$");
            if (!ToBool(read_formula(values)))
            {
                pass_block();
                return null;
            }

            var next_exe = read_block();
            return () => next_exe.Start(execute_next);
        }

        private Action exe_options()
        {
            executorList.Clear();
            var option_list = new List<PrintData>();
            do
            {
                var values = exe_lines[curt_pos++].Split("$");
                var pd = read_print_data();
                var exe = read_block();
                if (values.Length <= 2 ||
                    ToBool(read_formula(values)))
                {
                    option_list.Add(pd);
                    executorList.Add(exe);
                }
            } while (check_head(Keyword.O_OptionsHead));

            if (option_list.Count == 0) return null;
            return () => provider.SetOptions(option_list, index =>
            {
                var executor = executorList[index];
                executorList.Clear();
                executor.Start(execute_next);
            });
        }
        
        
        private bool check_end()
        {
            return curt_pos >= end_pos || exe_lines[curt_pos].Length < 5;
        }

        private bool check_head(string s)
        {
            if (check_end()) return false;
            return exe_lines[curt_pos][..5].Equals(s);
        }

        private string read_head()
        {
            if (check_end()) return string.Empty;
            return exe_lines[curt_pos][..5];
        }
        
        

        /// <summary>
        /// 读取printData，调用该方法后指针位于print data的最后行+1
        /// </summary>
        private PrintData read_print_data()
        {
            var data_list = new List<PrintElement>();
            do
            {
                var values = exe_lines[curt_pos++].Split("$");
                var data = new PrintElement(values[1], GetKeyValues(2, values));
                data_list.Add(data);
            } while (check_head(Keyword.O_Array));

            return new PrintData(data_list);
        }


        /// <summary>
        /// 跳过段，调用该方法后指针位于段的最后行+1
        /// </summary>
        /// <returns></returns>
        private void pass_block()
        {
            var values = exe_lines[curt_pos].Split("$");
            curt_pos += int.Parse(values[1]);
        }


        /// <summary>
        /// 读取段，生成新的执行器，调用该方法后指针位于段的最后行+1
        /// </summary>
        /// <returns></returns>
        private DialogExecutor read_block()
        {
            var values = exe_lines[curt_pos].Split("$");
            var start = curt_pos + 1;
            var end = curt_pos + int.Parse(values[1]);
            var executor = new DialogExecutor(this, start, end);
            curt_pos = end;
            return executor;
        }


        /// <summary>
        /// 根据表达式计算出值
        /// </summary>
        private string read_formula(IReadOnlyList<string> line)
        {
            var stack = new Stack<string>();
            for (var i = 1; i < line.Count; i++)
            {
                var m = line[i];
                if (Keyword.KeyWords.Contains(m))
                {
                    var strLeft = stack.Pop();
                    var strRight = stack.Pop();
                    var valueRight = get_val(strRight);
                    var valueLeft = get_val(strLeft);
                    var value = Calc(valueLeft, valueRight, m);
                    strLeft = IsSetOpt(m) ? set_val(strLeft, value) : value;
                    stack.Push(strLeft);
                    continue;
                }

                stack.Push(m);
            }

            return get_val(stack.Pop());
        }
        
        

        /// <summary>
        /// 获取表达式中变量的值 第一次获取本地变量为0
        /// </summary>
        private string get_val(string valName)
        {
            return valName[0] switch
            {
                '*' => provider.GetLocalVal(valName.Substring(1, valName.Length - 1)),
                '@' => provider.GetGlobalVal(valName.Substring(1, valName.Length - 1)),
                _ => valName
            };
        }

        /// <summary>
        /// 对表达式中变量进行值
        /// </summary>
        private string set_val(string valName, string value)
        {
            switch (valName[0])
            {
                case '*':
                    provider.SetLocalVal(valName.Substring(1, valName.Length - 1), value);
                    return valName;
                case '@':
                    provider.SetGlobalVal(valName.Substring(1, valName.Length - 1), value);
                    return valName;
                default:
                    return value;
            }
        }

        /// <summary>
        /// 二元运算
        /// </summary>
        private static string Calc(string left, string right, string type)
        {
            return type switch
            {
                Keyword.Gr => ToValue(GreaterOpt(left, right)),
                Keyword.SrEql => ToValue(!GreaterOpt(left, right)),
                Keyword.Sr => ToValue(SmallerOpt(left, right)),
                Keyword.GrEql => ToValue(!SmallerOpt(left, right)),
                Keyword.Equal => ToValue(EqualOpt(left, right)),
                Keyword.NEql => ToValue(!EqualOpt(left, right)),
                Keyword.Add => ToValue(ToFloat(left) + ToFloat(right)),
                Keyword.Sub => ToValue(ToFloat(left) - ToFloat(right)),
                Keyword.Mul => ToValue(ToFloat(left) * ToFloat(right)),
                Keyword.Div => ToValue(ToFloat(left) / ToFloat(right)),
                Keyword.And => ToValue(ToBool(left) && ToBool(right)),
                Keyword.Or => ToValue(ToBool(left) || ToBool(right)),
                Keyword.Set => right,
                Keyword.AddSet => ToValue(ToFloat(left) + ToFloat(right)),
                Keyword.SubSet => ToValue(ToFloat(left) - ToFloat(right)),
                Keyword.MulSet => ToValue(ToFloat(left) * ToFloat(right)),
                Keyword.DivSet => ToValue(ToFloat(left) / ToFloat(right)),
                _ => "0"
            };
        }
        
        

        /// <summary>
        /// 读取列表中的键值对
        /// </summary>
        private static KeyValues GetKeyValues(int begin, IReadOnlyList<string> ls)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            for (var i = begin; i + 1 < ls.Count; i += 2)
                pairs.Add(new KeyValuePair<string, string>(ls[i], ls[i + 1])); //获取参数列表
            return new KeyValues(pairs);
        }


        internal static bool IsSetOpt(string m)
        {
            return m.Equals(Keyword.Set) || m.Equals(Keyword.AddSet) ||
                   m.Equals(Keyword.SubSet) || m.Equals(Keyword.MulSet) ||
                   m.Equals(Keyword.DivSet);
        }

        internal static bool EqualOpt(string a, string b)
        {
            if (a.Equals(b)) return true;
            var fa = ToFloat(a);
            var fb = ToFloat(b);
            return Math.Abs(fa - fb) < 0.0001f;
        }

        internal static bool GreaterOpt(string a, string b)
        {
            var fa = ToFloat(a);
            var fb = ToFloat(b);
            return fa > fb + 0.0001f;
        }

        internal static bool SmallerOpt(string a, string b)
        {
            var fa = ToFloat(a);
            var fb = ToFloat(b);
            return fa < fb - 0.0001f;
        }

        
        public static string ToValue(bool m)
        {
            return m ? "1" : "0";
        }

        public static string ToValue(float m)
        {
            var n = Math.Round(m);
            return Math.Abs(m - n) < 0.0001f
                ? n.ToString(CultureInfo.InvariantCulture)
                : $"{m:F2}";
        }

        public static bool ToBool(string m)
        {
            if (string.IsNullOrEmpty(m)) return false;
            if (!float.TryParse(m, out var value)) return false;
            return Math.Abs(value) > 0.0001f;
        }

        public static float ToFloat(string m)
        {
            if (!float.TryParse(m, out var value)) return 0;
            return value;
        }
    }

    /// <summary>
    /// 对话系统接口
    /// </summary>
    public interface IDialogHandler
    {
        /// <summary>
        /// 获取全局变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <returns>值</returns>
        string GetGlobalVar(string name);

        /// <summary>
        /// 设置全局变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="value">值</param>
        void SetGlobalVar(string name, string value);

        /// <summary>
        /// 调用函数
        /// </summary>
        /// <param name="callData">调用数据</param>
        /// <param name="callback">回调</param>
        void CallFunction(CallData callData, Action callback);

        /// <summary>
        /// 打印对话
        /// </summary>
        /// <param name="dialogID">对话ID（在Script中的行号）</param>
        /// <param name="speaker">讲话名称文本的打印数据</param>
        /// <param name="content">对话内容的文本打印数据</param>
        /// <param name="callback">回调</param>
        void PrintDialog(int dialogID, PrintData speaker, PrintData content, Action callback);

        /// <summary>
        /// 设置选项
        /// </summary>
        /// <param name="options">选项标签文本的打印数据</param>
        /// <param name="callback">回调, 参数：选择选项的序号</param>
        void SetOptions(IEnumerable<PrintData> options, Action<int> callback);
    }
    
    internal class Provider
    {
        public IDialogHandler handler;
        public Dictionary<string, string> local_values;

        public Provider(IDialogHandler handler)
        {
            this.handler = handler;
            local_values = new Dictionary<string, string>();
        }

        public void CallFunction(CallData callData, Action callback)
        {
            handler.CallFunction(callData, callback);
        }

        public void PrintDialog(int dialogID, PrintData speaker, PrintData content, Action callBack)
        {
            handler.PrintDialog(dialogID, speaker, content, callBack);
        }

        public void SetOptions(IEnumerable<PrintData> options, Action<int> callBack)
        {
            handler.SetOptions(options, callBack);
        }

        public string GetLocalVal(string name)
        {
            return !local_values.TryAdd(name, "0") ? local_values[name] : "0";
        }

        public void SetLocalVal(string name, string value)
        {
            if (local_values.TryAdd(name, value)) return;
            local_values[name] = value;
        }

        public string GetGlobalVal(string name)
        {
            return handler.GetGlobalVar(name);
        }

        public void SetGlobalVal(string name, string value)
        {
            handler.SetGlobalVar(name, value);
        }
    }
}
