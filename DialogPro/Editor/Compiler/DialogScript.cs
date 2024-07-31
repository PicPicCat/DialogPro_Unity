using System;
using System.Collections.Generic;

namespace DialogPro
{
    public static class DialogScript
    {
        /// <summary>
        /// 编译 Dialog Script
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="includes">包含文件路径</param>
        /// <param name="target">输出输出字符串</param>
        /// <param name="info">输出信息</param>
        /// <returns>是否编译成功</returns>
        public static bool Compile(string source,
            IReadOnlyDictionary<string,string> includes,
            out string target, out string info)
        {
            try
            {
                target = Compiler.Compile(source, includes);
                info = string.Empty;
                return true;
            }
            catch (Exception e)
            {
                info = e.ToString();
                target = string.Empty;
                return false;
            }
        }
    }
    
    
    
}