using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogProEditor
{
    [CreateAssetMenu(fileName = "New Compiler",
        menuName = "DialogPro/Compiler")]
    public class CompilerAsset : ScriptableObject
    {
        public DefaultAsset sourceFolder;
        public DefaultAsset targetFolder;
        public List<DefaultAsset> includeFolders;
    }
}