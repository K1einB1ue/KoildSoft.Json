using System;
using System.Collections.Generic;
using KolidSoft.Json;
using UnityEngine;

namespace KolidSoft.DesignPattern
{
    public class KeyPath
    {
        [Save] public string Key;
        [Save] public string Path;
    }
    public class TypeAndPaths
    {
        [Save] public string TypeFullName;
        [Save] public List<KeyPath> KeyPaths = new();
    }
    public class JsonResources : JsonSingleton<JsonResources>
    {

        [OnSave] public List<TypeAndPaths> ResourceList()
        {
            var save = new List<TypeAndPaths>();
            foreach (var type in ResDictionary.Keys)
            {
                var typeAndPaths = new TypeAndPaths
                {
                    TypeFullName = type.FullName,
                    KeyPaths = new List<KeyPath>()
                };
                foreach (var keyPath in ResDictionary[type])
                {
                    typeAndPaths.KeyPaths.Add(new KeyPath
                    {
                        Key = keyPath.Key,
                        Path = keyPath.Value
                    });
                }

                save.Add(typeAndPaths);
            }
            return save;
        }

        [OnLoad(nameof(ResourceList))]
        public void LoadResourceList(List<TypeAndPaths> list)
        {
            var resDic = new Dictionary<Type, Dictionary<string, string>>();
            foreach (var typeAndPaths in list)
            {
                var type = Type.GetType(typeAndPaths.TypeFullName);
                if (type == null) continue;
                var dic = new Dictionary<string, string>();
                resDic.Add(type, dic);
                foreach (var keyPath in typeAndPaths.KeyPaths)
                {
                    dic.Add(keyPath.Key, keyPath.Path);
                }
            }
            _resDic = resDic;
        }

        [Save] public List<string> UnBindPaths = new();

        private Dictionary<Type, Dictionary<string, string>> _resDic = new();
        public Dictionary<Type, Dictionary<string, string>> ResDictionary => _resDic;
        
    }

    public static class JsonResourcesExtern
    {
        public static string JsonQuery(this object obj, string key)
        {
            return JsonResources.Instance.ResDictionary[obj.GetType()][key];
        }
        public static Func<string,string> JsonQueryCache(this object obj)
        {
            return JsonResources.Instance.ResDictionary.TryGetValue(obj.GetType(), out var keyPathDic)
                ? str => keyPathDic[str]
                : null;
        }
    }
    
}