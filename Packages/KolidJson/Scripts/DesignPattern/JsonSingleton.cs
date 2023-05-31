using System.IO;
using KolidSoft.Json.Builder;
using KolidSoft.Json.Export;
using KolidSoft.Json.UI;

namespace KolidSoft.DesignPattern
{
    public class JsonSingleton<T> where T:class
    {
        private static T _instance = null;
        private static readonly object SyncObj = new();
        public static T Instance
        {
            get
            {
                lock (SyncObj)
                {
                    if (_instance != null) return _instance;
                    var setting = PathSetting.Setting;
                    setting.ThrowException();
                    _instance = Path.Combine(setting.RootPath, 
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(T).FullName).BuildObject<T>();
                    return _instance;
                }
            }
        }

        public static void Reload()
        {
            _instance = null;
        }

        public static void Save()
        {
            Instance.BuildJToken().Save(
                // ReSharper disable once AssignNullToNotNullAttribute
                Path.Combine(PathSetting.Setting.RootPath,typeof(T).FullName)
            );
        }
    }
}



