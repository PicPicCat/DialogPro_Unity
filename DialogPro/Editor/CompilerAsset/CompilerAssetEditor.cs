using System;
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
        private CompilerAsset compilerAsset;

        private void OnEnable()
        {
            compilerAsset = target as CompilerAsset;
        }
        

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("清理文件", GUILayout.ExpandWidth(true)))
            {
                ClearFiles();
            }

            if (GUILayout.Button("编译文件", GUILayout.ExpandWidth(true)))
            {
                CompileFiles();
            }
        }

        private void ClearFiles()
        {
            if (compilerAsset.targetFolder == null)
            {
                Debug.LogWarning("缺少目标文件夹");
                return;
            }

            var targetPath = AssetDatabase.GetAssetPath(compilerAsset.targetFolder);
            ClearFolder(targetPath, false);
            AssetDatabase.Refresh();
        }

        private void CompileFiles()
        {
            if (compilerAsset.targetFolder == null)
            {
                Debug.LogWarning("缺少目标文件夹");
                return;
            }

            if (compilerAsset.targetFolder == null)
            {
                Debug.LogWarning("缺少源文件夹");
                return;
            }

            var targetPath = AssetDatabase.GetAssetPath(compilerAsset.targetFolder);
            var sourcePath = AssetDatabase.GetAssetPath(compilerAsset.sourceFolder);
            var includePaths = compilerAsset.includeFolders.Aggregate(string.Empty,
                (current, path) => current + root_path +
                                   AssetDatabase.GetAssetPath(path) + "%");

            ClearFolder(targetPath, false);
            CompileFolder(sourcePath, targetPath, includePaths);
            AssetDatabase.Refresh();
        }

        private static readonly string root_path =
            Application.dataPath[..Application.dataPath.LastIndexOf("/",
                StringComparison.Ordinal)] + "/";

        private static void ClearFolder(string folder, bool deleteEmpty = true)
        {
            var paths = new[] { folder };
            var files = AssetDatabase.FindAssets(".dd t:TextAsset", paths);
            foreach (var file in files)
            {
                var path = AssetDatabase.GUIDToAssetPath(file);
                AssetDatabase.DeleteAsset(path);
            }

            var dirs = AssetDatabase.GetSubFolders(folder);
            foreach (var dir in dirs) ClearFolder(dir);

            if (!deleteEmpty) return;
            files = AssetDatabase.FindAssets("", paths);
            if (files.Length == 0) AssetDatabase.DeleteAsset(folder);
        }

        private static void CreateFile(string text, string path)
        {
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

            File.WriteAllText(root_path + path, text);
            AssetDatabase.ImportAsset(path);
        }

        private static void CompileFolder(string folderPath,
            string targetPath, string includePaths)
        {
            var paths = new[] { folderPath };
            var files = AssetDatabase.FindAssets(".ds t:TextAsset", paths);
            bool save = true;
            foreach (var file in files)
            {
                var path = AssetDatabase.GUIDToAssetPath(file);
                var source = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var error = !DialogScript.Compile(source.text, includePaths,
                    out var target, out var info);

                var pathLabel = "[ " + path + " ]";
                if (error) Debug.LogError("COMPILE ERROR: " + pathLabel + "INFO: " + info);
                else Debug.Log("COMPILE SUCCESS: " + pathLabel);
                if (error) save = false;
                if (!save) continue;

                var target_file_path = path.Replace(folderPath, targetPath).Replace(".ds", ".dd");
                CreateFile(target, target_file_path);
            }
        }
    }
}