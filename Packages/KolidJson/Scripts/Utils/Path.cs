using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

namespace KolidSoft.Utils
{
    public static partial class PathUtils
    {

        public static string[] GetFileDirs(string path, [RegexPattern] string regexPattern, bool recurse = false,
            bool returnRegexValue = false, FileAttributes attributes =
                FileAttributes.Archive | FileAttributes.Compressed | FileAttributes.Device | FileAttributes.Directory |
                FileAttributes.Encrypted | FileAttributes.Hidden |
                FileAttributes.Normal | FileAttributes.Offline | FileAttributes.System | FileAttributes.Temporary |
                FileAttributes.IntegrityStream | FileAttributes.ReadOnly |
                FileAttributes.ReparsePoint | FileAttributes.SparseFile | FileAttributes.NoScrubData |
                FileAttributes.NotContentIndexed
        ) {
            var lst = new List<string>();
            foreach (var item in Directory.GetFileSystemEntries(path))
            {
                var match = Regex.Match(Path.GetFileName(item), regexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && (File.GetAttributes(item) & attributes) != 0)
                {
                    if (!Regex.IsMatch(item, ".meta$", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                    {
                        lst.Add(returnRegexValue ? match.Value : item);
                    }
                }
                if (recurse && (File.GetAttributes(item) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    lst.AddRange(GetFileDirs(item, regexPattern, true, returnRegexValue, attributes));
                }
            }
            return lst.ToArray();
        }
        
        public static string GetAssetPath(string dir)
        {
            return Regex.Match(dir, "Assets.+").Value;
        }

        public static string GetResourcePath(string dir)
        {
            return Regex.Match(dir, @"(?<=Resources[\\/]).+(?=\.prefab$)").Value;
        }

        /*
        public static string GetAbsolutePath(string assetPath)
        {
            var match = Regex.Match(assetPath, "(?<=Assets[\\/]).+");
            if (!match.Success) return null;
            return Path.Combine(Application.dataPath, match.Value);
        }
        */

        public static void CreateFile(string path)
        {
            Debug.Log($"CreateFile:{path}");
            File.Create(path).Close();
        }
    }
}