using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using DialogPro;
using UnityEditor;

namespace DialogProEditor
{
    [CustomEditor(typeof(CompilerAsset))]
    public class CompilerAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("清理文件", GUILayout.ExpandWidth(true)))
            {
                var cAsset = target as CompilerAsset;
                if (!cAsset.targetFolder)
                {
                    Debug.LogWarning("缺少目标文件夹");
                    return;
                }

                var targetPath = AssetDatabase.GetAssetPath(cAsset.targetFolder);
                var files = AssetDatabase.FindAssets(".dd t:TextAsset", new[] { targetPath });
                foreach (var fileID in files)
                {
                    var filePath = AssetDatabase.GUIDToAssetPath(fileID);
                    AssetDatabase.DeleteAsset(filePath);
                }

                DeleteEmptyFolders(targetPath);
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("编译文件", GUILayout.ExpandWidth(true)))
            {
                var cAsset = target as CompilerAsset;
                if (!cAsset.targetFolder)
                {
                    Debug.LogWarning("缺少目标文件夹");
                    return;
                }

                if (!cAsset.targetFolder)
                {
                    Debug.LogWarning("缺少源文件夹");
                    return;
                }

                string[] files;
                var includes = new Dictionary<string, string>();
                var targetPath = AssetDatabase.GetAssetPath(cAsset.targetFolder);
                var sourcePath = AssetDatabase.GetAssetPath(cAsset.sourceFolder);
                foreach (var include in cAsset.includeFolders)
                {
                    var includeRoot = AssetDatabase.GetAssetPath(include);
                    var iRootLen = includeRoot.Length + 1;
                    files = AssetDatabase.FindAssets(".dh t:TextAsset", new[] { includeRoot });
                    foreach (var fileID in files)
                    {
                        var fileFullPath = AssetDatabase.GUIDToAssetPath(fileID);
                        var text = AssetDatabase.LoadAssetAtPath<TextAsset>(fileFullPath).text;
                        var key = fileFullPath.Substring(iRootLen, fileFullPath.Length - iRootLen);
                        key = key.Replace(".dh.txt", string.Empty);
                        if (!includes.TryAdd(key, text))
                        {
                            Debug.LogWarning($"包含文件中出现相同路径：{key}");
                            return;
                        }
                    }
                }

                files = AssetDatabase.FindAssets(".ds t:TextAsset", new[] { sourcePath });
                var sRootLen = sourcePath.Length;
                var targetPathSet = new HashSet<string>();
                foreach (var fileID in files)
                {
                    var sPath = AssetDatabase.GUIDToAssetPath(fileID);
                    var tPath = targetPath + sPath.Substring(sRootLen,
                        sPath.Length - sRootLen).Replace(".ds", ".dd");
                    var data = new CompileData
                    {
                        sourcePath = sPath,
                        targetPath = tPath,
                        includes = includes,
                    };
                    CompileFile(data);
                    targetPathSet.Add(tPath);
                }

                files = AssetDatabase.FindAssets(".dd t:TextAsset", new[] { targetPath });
                foreach (var fileID in files)
                {
                    var tPath = AssetDatabase.GUIDToAssetPath(fileID);
                    if (targetPathSet.Contains(tPath)) continue;
                    AssetDatabase.DeleteAsset(tPath);
                }

                DeleteEmptyFolders(targetPath);
                AssetDatabase.Refresh();
            }
        }

        private static void CompileFile(CompileData data)
        {
            var sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(data.sourcePath);
            var error = !DialogScript.Compile(sourceAsset.text, data.includes,
                out var targetText, out var info);
            if (error)
            {
                WriteFile(string.Empty, data.targetPath);
                Debug.LogError($"<color=#FFAAAA>COMPILE ERROR:[{data.sourcePath}]</color> INFO: {info}");
            }
            else
            {
                WriteFile(targetText, data.targetPath);
                Debug.Log($"<color=#AAFFAA>COMPILE SUCCESS:[{data.sourcePath}]</color>");
            }
        }

        private static void WriteFile(string text, string path)
        {
            //Create Folder
            var folders = path.Split("/");
            var parentFolder = folders[0];
            for (var i = 1; i < folders.Length - 1; i++)
            {
                var curtFolder = parentFolder + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(curtFolder))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);
                }

                parentFolder = curtFolder;
            }

            var assetRootPath = Application.dataPath;
            assetRootPath = assetRootPath[..assetRootPath.LastIndexOf("/")] + "/";
            var fullPath = assetRootPath + path;
            File.WriteAllText(fullPath, text);
            AssetDatabase.Refresh();
        }

        private static void DeleteEmptyFolders(string root, bool deleteSelf = false)
        {
            if (!AssetDatabase.IsValidFolder(root)) return;
            foreach (var subFolder in AssetDatabase.GetSubFolders(root))
            {
                DeleteEmptyFolders(subFolder, true);
            }

            if (!deleteSelf) return;
            if (AssetDatabase.FindAssets(string.Empty,
                    new[] { root }).Length > 0) return;
            AssetDatabase.DeleteAsset(root);
        }

        private class CompileData
        {
            public string sourcePath;
            public string targetPath;
            public IReadOnlyDictionary<string, string> includes;
        }
    }
}