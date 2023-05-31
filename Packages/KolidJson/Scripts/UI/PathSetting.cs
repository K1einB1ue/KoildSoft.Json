using System;
using System.IO;
using KolidSoft.Json.Builder;
using KolidSoft.Json.Export;
using KolidSoft.Utils;
using UnityEditor;
using UnityEngine;

namespace KolidSoft.Json.UI
{

    public class PathUnConfigException : Exception {}

    public class PathSetting
    {
        [Save(DefaultValue = "")] public string RootPath;
        [Save(DefaultValue = "")] public string ScenePath;
        
        private static string _settingPath;
        private static PathSetting _setting = null;
        
        public static PathSetting Setting
        {
            get 
            {
                if (_setting != null) return _setting;
                //递归寻找配置文件
                var kObjectCfgPaths = PathUtils.GetFileDirs(Application.dataPath, "KolidJson.setting", true);
                switch (kObjectCfgPaths.Length)
                {
                    //如果没有找到,则在根文件夹创建配置文件
                    case 0:
                        _settingPath = Path.Combine(Application.dataPath, "KolidJson.setting");
                        PathUtils.CreateFile(_settingPath);
#if UNITY_EDITOR
                        AssetDatabase.ImportAsset(PathUtils.GetAssetPath(_settingPath));
#endif
                        Debug.Log($"没有找到\"KolidJson.setting\",已生成至{_settingPath}");
                        break;
                    //如果找到了多个配置文件,则提示用户删除多余项
                    case > 1:
                        throw new Exception($"发现了多个\"KolidJson.setting\",请删除至只剩一个.");
                    //找到了准确的位置
                    default:
                        _settingPath = kObjectCfgPaths[0];
                        break;
                }

                _setting = _settingPath.BuildObject<PathSetting>();
                return _setting;
            }
        }

        public void ThrowException()
        {
            if (RootPath == "" || ScenePath == "") throw new PathUnConfigException();
        }

        public static void Save()
        {
            if (!File.Exists(_settingPath))
            {
                //递归寻找配置文件
                var kObjectCfgPaths = PathUtils.GetFileDirs(Application.dataPath, "KolidJson.setting", true, true);
                switch (kObjectCfgPaths.Length)
                {
                    //如果没有找到,则在根文件夹创建配置文件
                    case 0:
                        _settingPath = Path.Combine(Application.dataPath, "KolidJson.setting");
                        PathUtils.CreateFile(_settingPath);
#if UNITY_EDITOR
                        AssetDatabase.ImportAsset(PathUtils.GetAssetPath(_settingPath));
#endif
                        Debug.Log($"没有找到\"KolidJson.setting\",已生成至{_settingPath}");
                        break;
                    //如果找到了多个配置文件,则提示用户删除多余项
                    case > 1:
                        throw new Exception($"发现了多个\"KolidJson.setting\",请删除至只剩一个.");
                    //找到了准确的位置
                    default:
                        _settingPath = kObjectCfgPaths[0];
                        break;
                }
            }
            _setting.BuildJToken().Save(_settingPath);
        }

        public static void Reload()
        {
            _setting = null;
        }
        
    }
}