using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace KolidSoft.Json.Export
{
    public static class JTokenExport
    {
        public static void Save(this JToken jToken, string path)
        {
            var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var sw = new StreamWriter(fs);
            var json = jToken.ToString();
            sw.WriteLine(json);
            fs.SetLength(json.Length);
            sw.Close();
            fs.Close();
#if UNITY_EDITOR
            AssetDatabase.ImportAsset(Regex.Match(path, "Assets.+").Value);
#endif
        }
    }
}
