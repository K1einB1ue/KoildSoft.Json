using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KolidSoft.DesignPattern;
using KolidSoft.Json.Builder;
using KolidSoft.Json.Export;
using KolidSoft.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;



namespace KolidSoft.Json.UI
{
    /*存储存档根目录-----------------------------------------------------------------------+
    |
    |                        +-scene0----+-Layout.save(关卡中物体的坐标)
    |                        |           +-a6451f89wa4.save
    |                        |           +-a561d1a66as.save
    |                        |           +-115a1yh16af.save
    |                        |
    |  Root----+saveFile0----+-scene1----+-Layout.save
    |          |             |           +-d5a1aw5ac3s.save
    |          |             |           +-.. ..
    |          +-.. ..       |
    |                        |           +-awd5ra1f1dd----+-awd5ra1f1dd.save
    |                        |           |                +-awd5ra851dd----+-.. ..
    |                        |           |
    |                        +-Container-+-wa9a56xcv1w----+.. .. (一些在容器内的物体)
    |                        |
    |                        +-Save.save(用于存储主体相关的信息)
    |                                                                                   
    +-------------------------------------------------------------------------------------*/

    //每个存档要存的东西
    public class SaveSave
    {
        //主体的场景和Uuid(然后定位到一个场景的物体)
        [Save(DefaultValue = "")] public string Scene;
        [Save(DefaultValue = "")] public string Uuid;
        
        private static SaveSave _instance = null;
        public static SaveSave Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var saveSavePath = Path.Combine(
                    PathSetting.Setting.RootPath, JsonObject.SaveFile, ObjectWindow.SaveSaveFullName
                );
                _instance = saveSavePath.BuildObject<SaveSave>();
                return _instance;
            }
        }

        public static void Save()
        {
            var saveSavePath = Path.Combine(
                PathSetting.Setting.RootPath,JsonObject.SaveFile,ObjectWindow.SaveSaveFullName
            );
            Instance.BuildJToken().Save(saveSavePath);
        }

        public static void Reload()
        {
            _instance = null;
        }
    }

    //每个存档下每个场景需要存的东西
    public class SceneSave
    {
        [Save] public List<LayoutInfo> LayoutInfo = new();
        
        private static SceneSave _instance = null;
        
        public static SceneSave Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var sceneSavePath = Path.Combine(
                    PathSetting.Setting.RootPath, JsonObject.SaveFile, SceneManager.GetActiveScene().name,
                    ObjectWindow.SceneSaveFullName
                );
                _instance = sceneSavePath.BuildObject<SceneSave>();
                return _instance;
            }
        }

        public static void Save()
        {
            var sceneSavePath = Path.Combine(
                PathSetting.Setting.RootPath, JsonObject.SaveFile, SceneManager.GetActiveScene().name,
                ObjectWindow.SceneSaveFullName
            );
            Instance.BuildJToken().Save(sceneSavePath);
        }

        public static void Reload()
        {
            _instance = null;
        }
    }

    public class LayoutInfo
    {
        [Save(DefaultValue = "")] public string Uuid;
        [Save(DefaultValue = 0f)] public float Px, Py, Pz;
        [Save(DefaultValue = 1f)] public float Qw;
        [Save(DefaultValue = 0f)] public float Qx, Qy, Qz;
        public Vector3 Position
        {
            get => new(Px, Py, Pz);
            set
            {
                Px = value.x;
                Py = value.y;
                Pz = value.z;
            }
        }

        public Quaternion Rotation
        {
            get => new(Qx, Qy, Qz, Qw);
            set
            {
                Qx = value.x;
                Qy = value.y;
                Qz = value.z;
                Qw = value.w;
            }
        }
    }
    
    public class ObjectWindow : EditorWindow
    {
        public const string JsonLayoutTag = "JsonLayout";
        public const string JsonEditingTag = "JsonEditing";
        public const string SaveSuffix = ".save";
        public const string SaveSaveFileName = "Save";
        public const string SaveSaveFullName = SaveSaveFileName + SaveSuffix;
        public const string SceneSaveFileName = "Layout";
        public const string SceneSaveFullName = SceneSaveFileName + SaveSuffix;
        public const string ContainerItemDirectoryName = "Container";

        [MenuItem("KoildSoft/Json/ObjectWindow")]
        public static void CreateWindow()
        {
            var wnd = GetWindow<ObjectWindow>();
            wnd.titleContent = new GUIContent("KolidJson-ObjectWindow");
        }

        [InitializeOnLoadMethod()]
        public static void InitTags()
        {
            if (!UnityEditorInternal.InternalEditorUtility.tags.Equals(JsonLayoutTag))
                UnityEditorInternal.InternalEditorUtility.AddTag(JsonLayoutTag);
            if (!UnityEditorInternal.InternalEditorUtility.tags.Equals(JsonEditingTag))
                UnityEditorInternal.InternalEditorUtility.AddTag(JsonEditingTag);
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type type = null;
            foreach (var assembly in assemblies)
            {
                type = assembly.GetType("UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode");
                if (type != null) break;
            }
            if (type == null) return;
            var field = type.GetField("s_SkipCompilePlayerScripts", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null) throw new Exception("This lib out of data");
            field.SetValue(null, true);
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(Config.ConfigPath.UIRooTPath, "ObjectWindow.uxml"));
            visualTree.CloneTree(root);

            
            SceneName = root.Q<Label>(nameof(SceneName));
            Addition = root.Q<ScrollView>(nameof(Addition));
            Edit = root.Q<Button>(nameof(Edit));
            Refresh = root.Q<Button>(nameof(Refresh));
            Switch = root.Q<Button>(nameof(Switch));
            CreateKObject = root.Q<Button>(nameof(CreateKObject));
            DeleteKObject = root.Q<Button>(nameof(DeleteKObject));
            BindSubject = root.Q<Button>(nameof(BindSubject));
            Subject = root.Q<Button>(nameof(Subject));
            ObjectType = root.Q<DropdownField>(nameof(ObjectType));
            SaveFile = root.Q<DropdownField>(nameof(SaveFile));
            ObjectsList = root.Q<ListView>(nameof(ObjectsList));
            _visualInstance = new VisualInstance(Addition);
            Wrapper = new JsonTypeDropDownWrapper(ObjectType);
            

            SceneName.text = SceneManager.GetActiveScene().name;
            SaveFileUpdate();
            Switching = true;
            
            Refresh.clicked += () =>
            {
                SceneName.text = SceneManager.GetActiveScene().name;
                SaveFileUpdate();
                if(!Switching) Switching = true;
            };
            
            Switch.clicked += () =>
            {
                Switching = !Switching;
            };
            
            Edit.clicked += () =>
            {
                IsEditingType = !IsEditingType;
            };
            
            CreateKObject.clicked += () =>
            {
                var layout = LayoutSpace;
                if (layout == null) return;
                var pos = Vector3.zero;
                var rot = Quaternion.identity;
                var activeGo = Selection.activeGameObject;
                if (activeGo)
                {
                    pos = activeGo.transform.position;
                    rot = activeGo.transform.rotation;
                }
                var proto = JsonObject.Create<JsonObject>();
                proto.Save();
                var go = new GameObject
                {
                    transform =
                    {
                        position = pos,
                        rotation = rot,
                        parent = LayoutSpace.transform,
                        tag = JsonEditingTag
                    },
                    name = $"{nameof(JsonObject)}({proto.uuid})"
                };
                var editing = go.AddComponent<JsonEditingTarget>();
                editing.uuid = proto.uuid;
                
                _editingTargets.Add(editing);
                ObjectsList.Rebuild();
                ObjectsList.selectedIndex = _editingTargets.Count - 1;
                Save();
            };

            DeleteKObject.clicked += () =>
            {
                var index = ObjectsList.selectedIndex;
                if (index == -1) return;
                var target = _editingTargets[index];
                if (target.uuid == SaveSave.Instance.Uuid)
                {
                    SaveSave.Instance.Uuid = "";
                    SaveSave.Instance.Scene = "";
                }
                JsonObject.Load(target.uuid).Delete();
                AssetDatabase.Refresh();
                DestroyImmediate(target.gameObject);
                _editingTargets.RemoveAt(index);
                ObjectsList.Rebuild();
                ObjectsList.selectedIndex = -1;
                _visualInstance.Clear();
                _selectTarget = null;
                ObjectType.index = -1;
                IsEditingType = false;
                Save();
            };

            BindSubject.clicked += () =>
            {
                var index = ObjectsList.selectedIndex;
                if (index == -1) return;
                SaveSave.Instance.Uuid = _editingTargets[index].uuid;
                SaveSave.Instance.Scene = SceneName.text;
                Save();
            };

            Subject.clicked += () =>
            {
                if (SaveSave.Instance.Uuid == "")
                {
                    Debug.LogWarning("没有绑定主体");
                    return;
                }

                if (SceneManager.GetActiveScene().name != SaveSave.Instance.Scene)
                {
                    Debug.Log($"主体在场景:{SaveSave.Instance.Scene}中");
                    return;
                }
                
                var index = _editingTargets.FindIndex(o => o.uuid == SaveSave.Instance.Uuid);
                Selection.activeObject = _editingTargets[index].gameObject;
                ObjectsList.selectedIndex = index;
            };
            

            _visualInstance.OnSave += obj =>
            {
                // ReSharper disable once PossibleNullReferenceException
                (obj as JsonObject).Save();
            };

            _visualInstance.OnPop += (_, obj) =>
            {
                //保存上个界面的对象
                _visualInstance.Save();
                //对新界面的对象插入静态数据,因为可能有一次对象保存了新的静态数据.
                var newType = obj.GetType();
                obj.InsertObject(JsonObject.StaticPath(newType));
                Wrapper.Update();
                Wrapper.Type = newType;
                _containerStack.Pop();
            };

            _visualInstance.OnPush += obj =>
            {
                Wrapper.Update();
                Wrapper.Type = obj.GetType();
                _containerStack.Push(obj as JsonObject);
            };

            _visualInstance.OnClear += () =>
            {
                _containerStack.Clear();
            };
            
            ObjectsList.makeItem = () =>
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
            
            ObjectsList.bindItem = (ve, index) =>
            {
                // ReSharper disable once PossibleNullReferenceException
                (ve as Label).text = _editingTargets[index].uuid;
            };
            
            ObjectsList.itemsSource = _editingTargets;

            ObjectsList.selectionChanged += objs =>
            {
                foreach (JsonEditingTarget obj in objs)
                {
                    var jo = JsonObject.Load(obj.uuid);
                    SelectTarget = jo;
                    Selection.activeGameObject = obj.gameObject;
                }
            };

            
        }

        private void SaveFileUpdate()
        {
            var setting = PathSetting.Setting;
            setting.ThrowException();
            var filePaths =
                PathUtils.GetFileDirs(setting.RootPath, @"([A-Z]|[a-z]|_|[0-9])+$", false, true, FileAttributes.Directory);
            var newChoices = new List<string>(filePaths);
            if (SaveFile.index >= newChoices.Count)
                SaveFile.index = newChoices.Count - 1;
            SaveFile.choices = newChoices;
        }

        //private SceneSave _sceneSave = new();
        //private SaveSave _saveSave = new();
        private readonly List<JsonEditingTarget> _editingTargets = new();
        


        

        private void Save()
        {
            if (SaveFile.index == -1) return;
            JsonObject.SaveFile = SaveFile.value;
            SaveSave.Save();
            
            
            SceneSave.Instance.LayoutInfo.Clear();
            foreach (var editingTarget in _editingTargets)
            {
                var info = new LayoutInfo
                {
                    Uuid = editingTarget.uuid
                };
                var trans = editingTarget.transform;
                info.Position = trans.position;
                info.Rotation = trans.rotation;
                SceneSave.Instance.LayoutInfo.Add(info);
            }
            SceneSave.Save();
            
        }

        

        

        private JsonLayoutSpace _layoutSpace = null;
        public JsonLayoutSpace LayoutSpace
        {
            get
            {
                if (SaveFile.index == -1) return null;
                if (_layoutSpace != null) return _layoutSpace;
                var saveFile = SaveFile.value;
                JsonObject.SaveFile = saveFile;
                var go = GameObject.FindWithTag(JsonLayoutTag);
                if(go) DestroyImmediate(go);
                
                go = new GameObject {
                    name = $"Layout({saveFile})",
                    tag = JsonLayoutTag
                };
                
                var layout = go.AddComponent<JsonLayoutSpace>();
                layout.saveFile = saveFile;
                _layoutSpace = layout;
                
                foreach (var layoutInfo in SceneSave.Instance.LayoutInfo)
                {
                    var type = JsonObject.LoadType(layoutInfo.Uuid);
                    var o = new GameObject
                    {
                        transform =
                        {
                            position = layoutInfo.Position,
                            rotation = layoutInfo.Rotation,
                            parent = LayoutSpace.transform,
                            tag = JsonEditingTag
                        },
                        name = $"{type.Name}({layoutInfo.Uuid})"
                    };
                    var editing = o.AddComponent<JsonEditingTarget>();
                    editing.uuid = layoutInfo.Uuid;
                    _editingTargets.Add(editing);
                }
                ObjectsList.Rebuild();
                return _layoutSpace;
            }
            set
            {
                if (value == null)
                {
                    _editingTargets.Clear();
                }
                _layoutSpace = value;
            }
        }

        
        public Label SceneName;
        public ScrollView Addition;
        public Button Edit;
        public Button Switch;
        public Button Refresh;
        public Button Subject;
        public Button CreateKObject;
        public Button DeleteKObject;
        public Button BindSubject;
        public DropdownField SaveFile;
        public DropdownField ObjectType;
        public JsonTypeDropDownWrapper Wrapper;
        public ListView ObjectsList;

        private bool _switching = false;

        private bool Switching
        {
            get => _switching;
            set
            {
                if (value)
                {
                    //一些启用之间的关系
                    SaveFile.SetEnabled(true);
                    CreateKObject.SetEnabled(false);
                    DeleteKObject.SetEnabled(false);
                    BindSubject.SetEnabled(false);
                    Switch.text = "Confirm";
                    //保存SaveSave和SceneSave
                    Save();
                    //清空Layout空间
                    if (LayoutSpace != null)
                        DestroyImmediate(LayoutSpace.gameObject);
                    LayoutSpace = null;
                    _editingTargets.Clear();
                    ObjectsList.Rebuild();
                    ObjectsList.selectedIndex = -1;
                    
                    SaveSave.Reload();
                    SceneSave.Reload();
                    //清除编辑页(同时内部会保存目前编辑的对象)
                    SelectTarget = null;
                    //关闭类编辑器
                    IsEditingType = false;
                    _switching = true;
                    
                }
                else
                {
                    var setting = PathSetting.Setting;
                    setting.ThrowException();
                    if (SaveFile.index == -1) return;
                    var path = Path.Combine(setting.RootPath, SaveFile.value);
                    if (!Directory.Exists(path)) throw new Exception($"未找到{path}存档文件夹.");
                    path = Path.Combine(path, SceneName.text);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    //一些启用之间的关系
                    SaveFile.SetEnabled(false);
                    CreateKObject.SetEnabled(true);
                    DeleteKObject.SetEnabled(true);
                    BindSubject.SetEnabled(true);
                    Switch.text = "Switch";
                    
                    if (SaveFile.index == -1) return;
                    JsonObject.SaveFile = SaveFile.value;
                    SaveSave.Reload();
                    SceneSave.Reload();

                    //创建LayoutSpace
                    var _ = LayoutSpace;
                    _switching = false;
                }
            }
        }

        private JsonObject _selectTarget;
        private VisualInstance _visualInstance;
        private readonly Stack<JsonObject> _containerStack = new();
        

        public JsonObject SelectTarget
        {
            get => _selectTarget;
            set
            {
                if (_selectTarget != null) _visualInstance.Save();
                if (value == null)
                {
                    _visualInstance.Clear();
                    ObjectType.SetEnabled(false);
                    Edit.SetEnabled(false);
                    Edit.text = "Edit";
                    ObjectType.index = -1;
                    _selectTarget = null;
                    return;
                }
                if (ReferenceEquals(_selectTarget, value)) return;
                Edit.SetEnabled(true);
                
                _visualInstance.Clear();
                _visualInstance.Push(value);
                _selectTarget = value;
            }
        }

        private bool _isEditingType;

        public bool IsEditingType
        {
            get => _isEditingType;
            set
            {
                if (value)
                {
                    _isEditingType = true;
                    ObjectType.SetEnabled(true);
                    Edit.text = "Confirm";
                    return;
                }
                if (ObjectType.index == -1) return;
                _isEditingType = false;
                ObjectType.SetEnabled(false);
                Edit.text = "Edit";

                if (_containerStack.Count <= 1)
                {
                    SelectTarget = SelectTarget.LoadAs(Wrapper.Type);
                    AssetDatabase.Refresh();
                }
                else
                {
                    var thisNode = _containerStack.Peek();
                    _containerStack.Pop();
                    var container = _containerStack.Peek() as JsonContainer;
                    _containerStack.Push(thisNode);
                    // ReSharper disable once PossibleNullReferenceException
                    var index = container.Contents.FindIndex(o => o.uuid == thisNode.uuid);
                    var o = container.AsType(index,Wrapper.Type);
                    AssetDatabase.Refresh();
                    _visualInstance.Pop();
                    _visualInstance.Push(o);
                }
            }
        }
    }
}
