using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using KolidSoft.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace KolidSoft.Json.UI
{

    /*存储存档根目录
    |
    |                         +-scene0
    |          +-kOriginal----+-scene1
    |          |              +-scene2
    |          |
    |          |              +-scene0
    |  Root----+-saveFile0----+-
    |          |              +-
    |          |
    |          |              +-scene1
    |          +-saveFile1----+-scene2
    |          |              +-
    |          |                                                          
    |          +-[typeof(T).FullName] (JsonSingleton<T>创建的)                                                                              
    |          |                                                        
    |          +-[typeof(T).FullName] (Static存储抽离开来的)                                                         
    |                                               
    |                                                                                   
    +------------------(1)-----------------*/
    
    /*存储场景根目录
    |
    |           +-scene0.unity
    |           |
    |  Scene----+-scene1.unity
    |           |
    |           +-scene2.unity
    |
    +------------------(2)-----------------*/


    public class ConfigWindow : EditorWindow
    {
        [MenuItem("KoildSoft/Json/ConfigWindow")]
        public static void CreateWindow()
        {
            var wnd = GetWindow<ConfigWindow>();
            wnd.titleContent = new GUIContent("KolidJson-ConfigWindow");
        }
        
        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(Config.ConfigPath.UIRooTPath, "ConfigWindow.uxml"));
            visualTree.CloneTree(root);
            
            Bind = root.Q<Button>(nameof(Bind));
            CreateSceneFile = root.Q<Button>(nameof(CreateSceneFile));
            Create = root.Q<Button>(nameof(Create));
            Delete = root.Q<Button>(nameof(Delete));
            SaveFile = root.Q<DropdownField>(nameof(SaveFile));
            Scene = root.Q<DropdownField>(nameof(Scene));
            SaveFileName = root.Q<TextField>(nameof(SaveFileName));
            RootPath = root.Q<TextField>(nameof(RootPath));
            ScenePath = root.Q<TextField>(nameof(ScenePath));
            
            Cancel = new Button
            {
                style = { 
                    backgroundColor = new StyleColor() {
                        value = new Color(0.588f, 0.576f, 0.0941f, 1f),
                    }
                },
                text = "Cancel",
            };
            
            RootPath.SetEnabled(false);
            ScenePath.SetEnabled(false);
            Bind.text = "Edit";
            
            PathSetting.Reload();
            var setting = PathSetting.Setting;
            RootPath.value = setting.RootPath;
            ScenePath.value = setting.ScenePath;
            SaveFileName.value = "";
            
            
            Bind.clicked += () =>
            {
                if (!Editing)
                {
                    Editing = true;
                }
                else
                {
                    var success = true;
                    var newSetting = PathSetting.Setting;
                    if (Directory.Exists(RootPath.value))
                        newSetting.RootPath = RootPath.value;
                    else
                    {
                        Debug.LogError("RootPath文件夹不存在.");
                        success = false;
                    }

                    if (Directory.Exists(ScenePath.value))
                        newSetting.ScenePath = ScenePath.value;
                    else
                    {
                        Debug.LogError("ScenePath文件夹不存在.");
                        success = false;
                    }

                    if (success)
                    {
                        PathSetting.Save();
                        Editing = false;
                        SceneUpdate();
                    }
                }
            };

            Cancel.clicked += () =>
            {
                Editing = false;
                var newSetting = PathSetting.Setting;
                RootPath.value = newSetting.RootPath;
                ScenePath.value = newSetting.ScenePath;
            };

            Delete.clicked += () =>
            {
                var path = PathSetting.Setting.RootPath;
                if (!Directory.Exists(path)) return;
                if (SaveFile.index == -1) return;
                var dirName = SaveFile.value;
                if (dirName == "") return;
                var dirPath = Path.Combine(path, dirName);
                if (!Directory.Exists(dirPath))
                {
                    Debug.LogWarning($"{dirPath}:存档不存在.");
                    return;
                }
                Directory.Delete(dirPath, true);
                Debug.Log($"删除了{dirPath}");
                SaveFileUpdate();
            };

            Create.clicked += () =>
            {
                var path = PathSetting.Setting.RootPath;
                if (!Directory.Exists(path)) return;
                var dirName = SaveFileName.value;
                if (dirName == "") return;
                var dirPath = Path.Combine(path, dirName);
                if (Directory.Exists(dirPath))
                {
                    Debug.LogWarning($"{dirPath}:存档已存在.");
                    return;
                }
                Directory.CreateDirectory(dirPath);
                Debug.Log($"创建了{dirPath}");
                SaveFileUpdate();
            };

            CreateSceneFile.clicked += () =>
            {
                if (Scene.index == -1) return;
                if (SaveFile.index == -1) return;
                var path = PathSetting.Setting.RootPath;
                if (!Directory.Exists(path)) return;
                var dirPath = Path.Combine(path, SaveFile.value, Scene.value);
                if (Directory.Exists(dirPath))
                {
                    Debug.LogWarning($"{dirPath}:存档已存在.");
                    return;
                }
                Directory.CreateDirectory(dirPath);
                Debug.Log($"创建了{dirPath}");
            };

            SceneUpdate();
            SaveFileUpdate();

            
        }
        
        private void SceneUpdate()
        {
            List<string> newChoices = null;
            var path = PathSetting.Setting.ScenePath;
            if (Directory.Exists(path))
            {
                var filePaths = PathUtils.GetFileDirs(path, @"([A-Z]|[a-z]|_|[0-9])+\.unity", true);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    filePaths[i]  = Regex.Match(filePaths[i], @"[^\\/]+(?=\.[^.]+$)").Value;
                }
                newChoices = new List<string>(filePaths);
            }
            newChoices ??= new List<string>();
            if (Scene.index >= newChoices.Count)
                Scene.index = newChoices.Count - 1;
            Scene.choices = newChoices;
        }

        private void SaveFileUpdate()
        {
            List<string> newChoices = null;
            var path = PathSetting.Setting.RootPath;
            if (Directory.Exists(path))
            {
                var filePaths = PathUtils.GetFileDirs(path, @"([A-Z]|[a-z]|_|[0-9])+$");
                for (int i = 0; i < filePaths.Length; i++)
                {
                    filePaths[i] = Regex.Match(filePaths[i], "([A-Z]|[a-z]|_|[0-9])+$").Value;
                    
                }
                newChoices = new List<string>(filePaths);
            }
            newChoices ??= new List<string>();
            if (SaveFile.index >= newChoices.Count)
                SaveFile.index = newChoices.Count - 1;
            SaveFile.choices = newChoices;
        }
        
        private void OnDestroy()
        {
            PathSetting.Save();
        }

        private void OnLostFocus()
        {
            PathSetting.Save();
        }
        
        
        
        
        
        
        
        
        
        
        public bool editing = false;
        public bool Editing
        {
            get => editing;
            set
            {
                if (value)
                {
                    Bind.parent.Add(Cancel);
                    Bind.text = "Bind";
                }
                else
                {
                    Bind.parent.Remove(Cancel);
                    Bind.text = "Edit";
                }
                RootPath.SetEnabled(value);
                ScenePath.SetEnabled(value);
                editing = value;
            }
            
        }
        public Button Bind;
        public Button Cancel;
        public Button CreateSceneFile;
        public Button Create;
        public Button Delete;
        public DropdownField SaveFile;
        public DropdownField Scene;
        public TextField SaveFileName;
        public TextField ScenePath;
        public TextField RootPath;
    }
}
