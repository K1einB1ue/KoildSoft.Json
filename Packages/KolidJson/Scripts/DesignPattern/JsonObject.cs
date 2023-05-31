using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KolidSoft.DesignPattern;
using KolidSoft.Json;
using KolidSoft.API;
using KolidSoft.Json.Builder;
using KolidSoft.Json.Config;
using KolidSoft.Json.Export;
using KolidSoft.Json.UI;
using KolidSoft.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

//#define USING_CLEAR_CONTAINER

namespace KolidSoft.API
{
    public partial class Item : JsonObject {}

    public partial class Container : JsonContainer {}
}

namespace KolidSoft.DesignPattern
{
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StaticSaveAttribute : SaveAttribute
    {
        public StaticSaveAttribute()
        {
            SaveTargets = SaveTargets.Static;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DynamicSaveAttribute : SaveAttribute
    {
        public DynamicSaveAttribute()
        {
            SaveTargets = SaveTargets.Dynamic;
        }
    }

    public class JsonTypeDropDownWrapper
    {
        private Type[] _types;
        private List<string> _enumDisplay;
        private readonly DropdownField _target;

        public JsonTypeDropDownWrapper(DropdownField dropdownField)
        {
            _target = dropdownField;
        }

        public void Update()
        {
            var types = typeof(JsonObject).Assembly.GetTypes();
            _types = Array.FindAll(types, type => typeof(JsonObject).IsAssignableFrom(type));
            _enumDisplay = _types.Select(type => type.FullName.Replace('.', '/')).ToList();
            _target.choices = _enumDisplay;
        }

        public Type Type
        {
            get => _target.index == -1 ? null : _types[_target.index];
            set => _target.index = Array.FindIndex(_types, type => type == value);
        }

        public void Clear()
        {
            _target.index = -1;
        }
    }

    
    

    [Serializable]
    public class JsonObject
    {
        [DynamicSave] public string uuid;
        [DynamicSave] public string dateInfo;
        [DynamicSave] public string typeInfo;

        public static T Create<T>() where T : JsonObject, new()
        {
            var ret = new T
            {
                uuid = Guid.NewGuid().ToString("N"),
                typeInfo = typeof(T).FullName,
                dateInfo = DateTime.Now.ToString("yyyy:MM:dd")
            };
            
            var staticPath = Path.Combine(
                PathSetting.Setting.RootPath,
                typeof(T).FullName + ObjectWindow.SaveSuffix
            );

            ret.InsertObject(staticPath);
            return ret;
        }
        
        public static JsonObject Create(Type type)
        {
            if (!typeof(JsonObject).IsAssignableFrom(type)) return null;
            var ret = Activator.CreateInstance(type) as JsonObject;
            // ReSharper disable once PossibleNullReferenceException
            ret.uuid = Guid.NewGuid().ToString("N");
            ret.typeInfo = type.FullName;
            ret.dateInfo = DateTime.Now.ToString("yyyy:MM:dd");
            
            var staticPath = Path.Combine(
                PathSetting.Setting.RootPath,
                type.FullName + ObjectWindow.SaveSuffix
            );

            ret.InsertObject(staticPath);
            return ret;
        }
        
        public static string StaticPath(Type type)
        {
            return Path.Combine(
                PathSetting.Setting.RootPath,
                type.FullName + ObjectWindow.SaveSuffix
            );
        }

        public static string StaticPath(string typeFullName)
        {
            return Path.Combine(
                PathSetting.Setting.RootPath,
                typeFullName + ObjectWindow.SaveSuffix
            );
        }

        public void Save()
        {
            string dynamicPath;
            if (OuterContainer == null)
            {
                dynamicPath = Path.Combine(
                    PathSetting.Setting.RootPath, SaveFile,
                    SceneManager.GetActiveScene().name, uuid + ObjectWindow.SaveSuffix
                );
            }
            else
            {
                var directory = Path.Combine(
                    PathSetting.Setting.RootPath, SaveFile,
                    ObjectWindow.ContainerItemDirectoryName, OuterContainer.uuid
                );
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                dynamicPath = Path.Combine(directory, uuid + ObjectWindow.SaveSuffix);
            }

            this.BuildJToken(SaveTargets.Dynamic).Save(dynamicPath);
            
#if UNITY_EDITOR
            this.BuildJToken(SaveTargets.Static).Save(StaticPath(GetType()));
#endif

            if (this is not JsonContainer container) return;
            
#if USING_CLEAR_CONTAINER
            ClearContainer(uuid);
#endif
            
            foreach (var obj in container.Contents)
            {
                obj.Save();
            }
        }

        public static Type LoadType(string uuid)
        {
            var dynamicPath = Path.Combine(
                PathSetting.Setting.RootPath, SaveFile,
                SceneManager.GetActiveScene().name, uuid + ObjectWindow.SaveSuffix
            );
            if (!File.Exists(dynamicPath))
            {
                Debug.LogError($"没有找到对应物体的存储:Uuid={uuid}");
                return null;
            }
            var typeFullName = dynamicPath.FindFirst<string>(nameof(typeInfo));
            return Type.GetType(typeFullName);
        }

        public static T Load<T>(string uuid) where T : JsonObject
        {
            return Load(uuid) as T;
        }
        
        public JsonObject LoadAs(Type type)
        {
            Save();
            string dynamicPath;
            if (OuterContainer == null)
            {
                dynamicPath = Path.Combine(
                    PathSetting.Setting.RootPath, SaveFile,
                    SceneManager.GetActiveScene().name, uuid + ObjectWindow.SaveSuffix
                );
                if (!File.Exists(dynamicPath))
                {
                    Debug.LogError($"没有找到对应物体的存储:Uuid={uuid}");
                    return null;
                }
            }
            else
            {
                dynamicPath = Path.Combine(
                    PathSetting.Setting.RootPath, SaveFile,
                    ObjectWindow.ContainerItemDirectoryName, OuterContainer.uuid, uuid + ObjectWindow.SaveSuffix
                );
                if (!File.Exists(dynamicPath))
                {
                    Debug.LogError($"没有找到对应物体的存储:Uuid={uuid},Container={OuterContainer.uuid}");
                    return null;
                }
            }

            var newTypeFullName = type.FullName;

            var ret = dynamicPath.BuildObject(type);
            ret.InsertObject(StaticPath(newTypeFullName));

            var jo = ret as JsonObject;
            // ReSharper disable once PossibleNullReferenceException
            jo.typeInfo = newTypeFullName;

            //如果同时是容器,它会在BuildObject内部完成对子对象的获取
            if (jo is JsonContainer) return jo;
            
            if (this is not JsonContainer container) return jo;
            //如果原来的类型是JsonContainer,而新的不是,则递归删除底下的JsonObjects
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < container.Contents.Count; i++)
                container.Contents[i].Delete();
            var directoryPath = Path.Combine(PathSetting.Setting.RootPath, SaveFile,
                ObjectWindow.ContainerItemDirectoryName, uuid);
            if (!Directory.Exists(directoryPath)) return jo;
            var files = PathUtils.GetFileDirs(directoryPath, @".+\.save$");
            if(files.Length!=0) Debug.LogWarning($"找到了{files.Length}个不关联的存档文件夹");
            Directory.Delete(directoryPath, true);
            var metaPath = directoryPath + ".meta";
            if (File.Exists(metaPath)) File.Delete(metaPath);
            return jo;
        }

        public void Delete()
        {
            string dynamicPath;
            if (OuterContainer == null)
            {
                dynamicPath = Path.Combine(
                    PathSetting.Setting.RootPath, SaveFile,
                    SceneManager.GetActiveScene().name, uuid + ObjectWindow.SaveSuffix
                );
            }
            else
            {
                dynamicPath = Path.Combine(
                    PathSetting.Setting.RootPath, SaveFile,
                    ObjectWindow.ContainerItemDirectoryName, OuterContainer.uuid, uuid + ObjectWindow.SaveSuffix
                );
                OuterContainer.Remove(this);
            }
            
            if (File.Exists(dynamicPath)) File.Delete(dynamicPath);
            var metaPath = dynamicPath + ".meta";
            if (File.Exists(metaPath)) File.Delete(metaPath);

            if (this is not JsonContainer container) return;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < container.Contents.Count; i++)
                container.Contents[i].Delete();
            var directoryPath = Path.Combine(PathSetting.Setting.RootPath, SaveFile,
                ObjectWindow.ContainerItemDirectoryName, uuid);
            if (!Directory.Exists(directoryPath)) return;
            var files = PathUtils.GetFileDirs(directoryPath, @".+\.save$");
            if(files.Length!=0) Debug.LogWarning($"找到了{files.Length}个不关联的存档文件夹");
            Directory.Delete(directoryPath, true);
            metaPath = directoryPath + ".meta";
            if (File.Exists(metaPath)) File.Delete(metaPath);
            
        }
        
        public static JsonObject Load(string uuid, string containerUuid = null)
        {
            var dynamicPath = containerUuid == null
            ? Path.Combine(
                PathSetting.Setting.RootPath, SaveFile,
                SceneManager.GetActiveScene().name, uuid + ObjectWindow.SaveSuffix
            )
            : Path.Combine(
                PathSetting.Setting.RootPath, SaveFile,
                ObjectWindow.ContainerItemDirectoryName, containerUuid, uuid + ObjectWindow.SaveSuffix
            );

            if (!File.Exists(dynamicPath))
            {
                Debug.LogError($"没有找到对应物体的存储:Uuid={uuid}");
                return null;
            }
            var typeFullName = dynamicPath.FindFirst<string>(nameof(typeInfo));
            var type = Type.GetType(typeFullName);
            var ret = dynamicPath.BuildObject(type);
            ret.InsertObject(StaticPath(typeFullName));
            
            var jo = ret as JsonObject;
            if (jo is not JsonContainer container) return jo;
            var uuids = dynamicPath.FindFirst<List<string>>(nameof(JsonContainer.__UUIDS__));
            foreach (var sonUuid in uuids)
                container.Add(Load(sonUuid, uuid));
            return jo;
        }

        
        
        
        public static string SaveFile;
        
        private JsonContainer _outerContainer = null;
        public JsonContainer OuterContainer
        {
            get => _outerContainer;
            internal set => _outerContainer = value;
        }

        public JsonObject RootObject
        {
            get
            {
                var container = OuterContainer;
                if (container == null) return this;
                while (container.OuterContainer!=null)
                    container = container.OuterContainer;
                return container;
            }
        }

        public bool IsRoot => _outerContainer == null;
    }
    
    public class JsonContainer : Item
    {
        private readonly JsonObjectList _contents = new();
        
        
        [Visual] public JsonObjectList Contents => _contents;

        public void Add(JsonObject jo)
        {
            //防止了一个物体同时出现在两个容器中.
            if (jo.OuterContainer == this) return;
            if (jo.OuterContainer != null) {
                if (jo.OuterContainer.Remove(jo)) _contents.Add(jo);
            } else _contents.Add(jo);
            jo.OuterContainer = this;
        }

        public bool Remove(JsonObject jo)
        {
            if (ReferenceEquals(jo.OuterContainer, this))
            {
                _contents.Remove(jo);
                jo.OuterContainer = null;
                return true;
            }
            return false;
        }

        public JsonObject AsType(int index,Type type)
        {
            if (_contents[index].GetType() == type) return null;
            if (!typeof(JsonObject).IsAssignableFrom(type)) return null;
            _contents[index] = _contents[index].LoadAs(type);
            _contents[index].OuterContainer = this;
            return _contents[index];
        }

        /// <summary>
        ///   <para>不要调用!这是Json存储时调用的函数!</para>
        /// </summary>
        [OnSave(SaveTargets = SaveTargets.Dynamic)] public List<string> __UUIDS__()
        {
            var save = new List<string>();
            foreach (var obj in _contents)
            {
                save.Add(obj.uuid);
            }
            return save;
        }
        
    }
    
    [CustomNode(typeof(JsonObjectListEditor))]
    public class JsonObjectList : List<JsonObject>{}

    
    public class JsonObjectListEditor : CustomNodeEditor
    {
        public readonly VisualElement AddBar = new()
        {
            style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)}
        };

        public readonly VisualElement Addition = new();

        public readonly DropdownField ObjectTypes = new()
        {
            style = {flexGrow = 1}
        };

        public JsonTypeDropDownWrapper Wrapper;
        
        public readonly Button AddButton = new()
        {
            text = "Add", 
            style = {
                flexGrow = 0,
                backgroundColor = new StyleColor(ConfigUi.Green)
            },
        };
        
        //public Type[] Types;
        //public List<string> names;


        public JsonObjectListEditor(object bindTarget, CustomNodeInfo customNodeInfo, VisualInitConfig visualInitConfig) : base(bindTarget, customNodeInfo, visualInitConfig)
        {
            Wrapper = new JsonTypeDropDownWrapper(ObjectTypes);
            NormalFramework(visualInitConfig);
            AddBar.Add(ObjectTypes);
            AddBar.Add(AddButton);
            Wrapper.Update();
            
            AddButton.clicked += () =>
            {
                if (ObjectTypes.index == -1) return;
                var container = visualInitConfig.VisualInstance.Peek<JsonContainer>();
                var newJo = JsonObject.Create(Wrapper.Type);
                container.Add(newJo);
                RefreshAddition(visualInitConfig.VisualInstance);
                (visualInitConfig.VisualInstance.RootTarget as JsonObject).Save();
            };
        }

        public override void OnSave()
        {

        }

        public override void BuildVisualElement(VisualBuildConfig visualBuildConfig)
        {
            RefreshAddition(visualBuildConfig.VisualInstance);
            VisualElement.Add(AddBar);
            VisualElement.Add(Addition);
        }

        private void RefreshAddition(VisualInstance visualInstance)
        {
            var container = visualInstance.Peek<JsonContainer>();
            Addition.Clear();
            foreach (var o in container.Contents)
            {
                var ve = new VisualElement()
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                    }
                };
                var deleteButton = new Button
                {
                    text = "Delete",
                    style =
                    {
                        flexGrow = 0,
                        backgroundColor = new StyleColor(ConfigUi.Red)
                    }
                };
                deleteButton.clicked += () =>
                {
                    container.Remove(o);
                    RefreshAddition(visualInstance);
                    (visualInstance.RootTarget as JsonObject).Save();
                };
                var enterButton = new Button {text = $"{o.GetType().Name}({o.uuid})", style = {flexGrow = 1}};
                enterButton.clicked += () =>
                {
                    foreach (var content in container.Contents)
                        content.Save();
                    visualInstance.Push(o);
                };
                ve.Add(enterButton);
                ve.Add(deleteButton);
                Addition.Add(ve);
            }
        }
    }
    
}