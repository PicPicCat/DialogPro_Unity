using System.Collections.Generic;
using System.Text;

namespace DialogPro
{
    internal static partial class Compiler
    {
        private class ScriptData
        {
            public ScriptData(List<string> lines,
                IReadOnlyDictionary<string, string> includes = null)
            {
                this.lines = lines;
                this.includes = includes;
                macros_note = new List<KeyValuePair<string, string>>();
                macros_call = new List<KeyValuePair<string, string>>();
                macros_condition = new List<KeyValuePair<string, string>>();
            }

            public readonly List<string> lines;
            public readonly IReadOnlyDictionary<string,string> includes;
            public readonly List<KeyValuePair<string, string>> macros_call;
            public readonly List<KeyValuePair<string, string>> macros_note;
            public readonly List<KeyValuePair<string, string>> macros_condition;
        }

        private class BlockData
        {
            public BlockData(ScriptData data)
            {
                scriptData = data;
                results = new StringBuilder();
            }

            public BlockData(BlockData parent)
            {
                lineCount = parent.lineCount;
                scriptData = parent.scriptData;
                results = new StringBuilder();
            }

            public ScriptData scriptData { get; }
            
            public string curtLine { get; set; }
            public int curtLineIndex { get; set; }

            public int lineCount { get; set; }
            public bool requireBlock { get; set; }
            public StringBuilder results { get; }
            public List<string> lines => scriptData.lines;
            public IReadOnlyDictionary<string, string> includes => scriptData.includes;
            public List<KeyValuePair<string, string>> macros_call => scriptData.macros_call;
            public List<KeyValuePair<string, string>> macros_note => scriptData.macros_note;
            public List<KeyValuePair<string, string>> macros_condition => scriptData.macros_condition;
        }
    }
}