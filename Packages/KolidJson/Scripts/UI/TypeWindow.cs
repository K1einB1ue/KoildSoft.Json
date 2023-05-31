using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KolidSoft.DesignPattern;
using KolidSoft.Json.Builder;
using KolidSoft.Json.Export;
using KolidSoft.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


namespace KolidSoft.Json.UI
{
    public class TypeWindow : EditorWindow
    {
        [MenuItem("KoildSoft/Json/TypeWindow")]
        public static void CreateWindow()
        {
            var wnd = GetWindow<TypeWindow>();
            wnd.titleContent = new GUIContent("KolidJson-TypeWindow");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(Config.ConfigPath.UIRooTPath, "TypeWindow.uxml"));
            visualTree.CloneTree(root);

            JsonType = root.Q<DropdownField>(nameof(JsonType));
            PrefabObject = root.Q<ObjectField>(nameof(PrefabObject));
            ImportPath = root.Q<Button>(nameof(ImportPath));
            ImportType = root.Q<Button>(nameof(ImportType));
            DeposeType = root.Q<Button>(nameof(DeposeType));
            DeposePath = root.Q<Button>(nameof(DeposePath));
            RemoveKey = root.Q<Button>(nameof(RemoveKey));
            TypeList = root.Q<ListView>(nameof(TypeList));
            PathList = root.Q<ListView>(nameof(PathList));
            DetailList = root.Q<ListView>(nameof(DetailList));
            PairedType = root.Q<Label>(nameof(PairedType));
            BindName = root.Q<TextField>(nameof(BindName));
            BindKey = root.Q<Button>(nameof(BindKey));
            
            Wrapper = new JsonTypeDropDownWrapper(JsonType);
            Wrapper.Update();
            
            PrefabObject.objectType = typeof(UnityEngine.Object);
            PrefabObject.allowSceneObjects = false;

            ImportPath.clicked += () =>
            {
                if (PrefabObject.value == null) return;
                _unbindPaths.Add(AssetDatabase.GetAssetPath(PrefabObject.value));
                JsonResources.Instance.UnBindPaths.Clear();
                JsonResources.Instance.UnBindPaths.AddRange(_unbindPaths);
                JsonResources.Save();
                PathList.Rebuild();
            };

            
            TypeList.makeItem = () =>
            {
                var label = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        marginLeft = 2,
                        paddingLeft = 3
                    }
                };
                return label;
            };
            
            if (_typesSet == null)
            {
                _typesSet = new();
                _typesBinding = new();
                _typesBinding.Clear();
                var dic = JsonResources.Instance.ResDictionary;
                foreach (var o in dic)
                {
                    _typesSet.Add(o.Key);
                    _typesBinding.Add(o.Key);
                }
                _unbindPaths.AddRange(JsonResources.Instance.UnBindPaths);
            }
            
            TypeList.bindItem = (ve, index) =>
            {
                // ReSharper disable once PossibleNullReferenceException
                (ve as Label).text = _typesBinding[index].FullName;
            };
            
            TypeList.itemsSource = _typesBinding;

            TypeList.onSelectionChange += objs =>
            {
                foreach (Type type in objs)
                {
                    PairedType.text = $"[{type.Name}]";
                    _detail.Clear();
                    foreach (var set in JsonResources.Instance.ResDictionary[type])
                    {
                        _detail.Add((set.Key, set.Value));
                    }
                    DetailList.Rebuild();
                    DetailList.selectedIndex = -1;
                }
            };

            ImportType.clicked += () =>
            {
                var type = Wrapper.Type;
                if (type == null) return;
                if (_typesSet.Contains(type)) return;
                _typesSet.Add(type);
                _typesBinding.Add(type);
                var dic = JsonResources.Instance.ResDictionary;
                if(dic.TryGetValue(type,out var key)) return;
                dic.Add(type, new Dictionary<string, string>());
                JsonResources.Save();
                TypeList.Rebuild();
            };
            
            DetailList.makeItem = () =>
            {
                var label = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        marginLeft = 2,
                        paddingLeft = 3
                    }
                };
                return label;
            };
            
            DetailList.bindItem = (ve, index) =>
            {
                // ReSharper disable once PossibleNullReferenceException
                (ve as Label).text = $"{_detail[index].key}:{_detail[index].value}";
            };
            
            DetailList.itemsSource = _detail;

            DetailList.onSelectionChange += objs => {};
            
            PathList.makeItem = () =>
            {
                var label = new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        marginLeft = 2,
                        paddingLeft = 3
                    }
                };
                return label;
            };
            
            PathList.bindItem = (ve, index) =>
            {
                // ReSharper disable once PossibleNullReferenceException
                (ve as Label).text = _unbindPaths[index];
            };
            
            PathList.itemsSource = _unbindPaths;

            PathList.onSelectionChange += objs => {};

            DeposeType.clicked += () =>
            {
                var type = SelectType;
                if (type == null) return;
                if (!_typesSet.Contains(type)) return;
                _typesSet.Remove(type);
                _typesBinding.Remove(type);
                JsonResources.Instance.ResDictionary.Remove(type);
                JsonResources.Save();
                TypeList.Rebuild();
                TypeList.selectedIndex = -1;
                PairedType.text = "[Missing]";
                _detail.Clear();
                DetailList.Rebuild();
            };

            DeposePath.clicked += () =>
            {
                var path = SelectPath;
                if (path == null) return;
                _unbindPaths.Remove(path);
                JsonResources.Instance.UnBindPaths.Clear();
                JsonResources.Instance.UnBindPaths.AddRange(_unbindPaths);
                JsonResources.Save();
                PathList.Rebuild();
                PathList.selectedIndex = -1;
            };

            BindKey.clicked += () =>
            {
                var key = BindName.value;
                if (key == "") return;
                var path = SelectPath;
                var type = SelectType;
                if (type == null || path == null) return;
                if (JsonResources.Instance.ResDictionary[type].ContainsKey(key)) return;
                _detail.Add((key, path));
                DetailList.Rebuild();
                _unbindPaths.Remove(path);
                JsonResources.Instance.UnBindPaths.Clear();
                JsonResources.Instance.UnBindPaths.AddRange(_unbindPaths);
                JsonResources.Instance.ResDictionary[type].Add(key, path);
                PathList.Rebuild();
                PathList.selectedIndex = -1;
                JsonResources.Save();
            };

            RemoveKey.clicked += () =>
            {
                var type = SelectType;
                if (type == null) return;
                var index = DetailList.selectedIndex;
                if (index == -1) return;
                var key =_detail[index].key;
                JsonResources.Instance.ResDictionary[type].Remove(key);
                JsonResources.Save();
                _detail.RemoveAt(index);
                DetailList.selectedIndex = -1;
                DetailList.Rebuild();
            };
        }

        private HashSet<Type> _typesSet = null;
        private List<Type> _typesBinding = null;
        private readonly List<string> _unbindPaths = new();
        private readonly List<(string key,string value)> _detail = new();

        public Type SelectType => TypeList.selectedIndex == -1 ? null : _typesBinding[TypeList.selectedIndex];
        public string SelectPath => PathList.selectedIndex == -1 ? null : JsonResources.Instance.UnBindPaths[PathList.selectedIndex];

        public void AddKeyPath(Type type, string key, string path)
        {
            var dic = JsonResources.Instance.ResDictionary;
            if (!dic.TryGetValue(type, out var keyPathDic)) return;
            keyPathDic.Add(key, path);
            JsonResources.Save();
            JsonResources.Reload();
        }
        
        
        

        public DropdownField JsonType;
        public JsonTypeDropDownWrapper Wrapper;
        public ListView TypeList;
        public ListView PathList;
        public ListView DetailList;
        public ObjectField PrefabObject;
        public Button ImportType;
        public Button ImportPath;
        public Button DeposeType;
        public Button DeposePath;
        public Button RemoveKey;
        public Label PairedType;

        public TextField BindName;
        public Button BindKey;
    }
}
