using System;

namespace DialogPro
{
    internal static partial class Compiler
    {
        public class ErrorException : Exception
        {
            public int Line { get; set; }
            public string Info { get; set; }

            public override string ToString()
            {
                return "[编译失败( at line: " + Line + " )] >> " + Info;
            }
        }
        
        private static void throw_error(string s)
        {
            throw new ErrorException { Info = s };
        }

        private static void throw_error(string s, int line)
        {
            throw new ErrorException { Info = s, Line = line };
        }

        private static void throw_error(ErrorException e, int line)
        {
            e.Line = line;
            throw e;
        }

        private static void throw_error(ErrorException e, string s)
        {
            e.Info = s + " >> " + e.Info;
            throw e;
        }
    }
}