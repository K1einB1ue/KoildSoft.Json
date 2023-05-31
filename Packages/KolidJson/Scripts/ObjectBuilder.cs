using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KolidSoft.Json.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KolidSoft.Json.Builder
{
    public static class ObjectBuilder
    {
        
#region FindFirst

        public static T FindFirst<T>(this string jsonPath, string name) where T : class
        {
            using var file = File.OpenText(jsonPath);
            using var reader = new JsonTextReader(file);
            var rootToken = JToken.ReadFrom(reader);
            var token =Analyzer.JTokenAnalyzer.FindFirst(rootToken, name);
            if (token == null) return ConfigJson.GetDefault(typeof(T)) as T;
            return BuildObject(token,typeof(T)) as T;
        }

        public static object FindFirst(string jsonPath, string name, Type type)
        {
            using var file = File.OpenText(jsonPath);
            using var reader = new JsonTextReader(file);
            var rootToken = JToken.ReadFrom(reader);
            var token = Analyzer.JTokenAnalyzer.FindFirst(rootToken, name);
            if (token == null) throw new Analyzer.NotFoundException();
            return BuildObject(token, type);
        }
        
#endregion

#region BuildObject

        public static T BuildObject<T>(this string jsonPath) where T : class
        {
            try
            {
                JToken rootToken;
                using (var file = File.OpenText(jsonPath))
                using (var reader = new JsonTextReader(file))
                {
                    rootToken = JToken.ReadFrom(reader);
                }
                if (BuildObject(rootToken,typeof(T)) is not T tmp) throw new Exception();
                return tmp;
            }
            catch(Exception)
            {
                if (BuildObject(JTokenBuilder.BuildJToken(typeof(T)),typeof(T)) is not T tmp) throw new Exception();
                return tmp;
            }
        }
        
        public static object BuildObject(this string jsonPath, Type rootType)
        {
            
            try
            {
                JToken rootToken;
                using (var file = File.OpenText(jsonPath))
                using (var reader = new JsonTextReader(file))
                {
                    rootToken = JToken.ReadFrom(reader);
                }
                return BuildObject(rootToken,rootType);
            }
            catch(JsonReaderException)
            {
                return BuildObject(JTokenBuilder.BuildJToken(rootType), rootType);
            }
        }
        
        public static object BuildObject(JToken jToken, Type rootType, SaveAttribute attribute = null)
        {
            var tempFields = rootType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = Array.FindAll(tempFields,item => item.IsDefined(typeof(SaveAttribute), false));
            var tempMethods = rootType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var methods = Array.FindAll(tempMethods, item => item.IsDefined(typeof(OnLoadAttribute), false));
            var tempProperties = rootType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var properties = Array.FindAll(tempProperties, item => item.IsDefined(typeof(SaveAttribute), false));
            var listFlag = typeof(IList).IsAssignableFrom(rootType);
            var classFlag = fields.Length + methods.Length + properties.Length > 0;
            //如果是值类型，则target转换为JValueToken
            if (!listFlag && !classFlag)
            {
                try
                {
                    return jToken.ToObject(rootType) ?? ConfigJson.GetDefault(rootType);
                }
                catch
                {
                    return ConfigJson.GetDefault(rootType);
                }
            }

            //如果是列表类，则循环创建target下的item的ToJson实现
            if (listFlag)
            {
                object list;
                
                //如果是数组
                if (rootType.IsArray)
                {
                    var elementType = rootType.GetElementType();
                    if (elementType == null) throw new Exception("Missing elementType");
                    if (attribute == null) throw new Exception("Missing attribute");
                    if (attribute is not SaveArrayAttribute sa) throw new Exception("Error SaveAttribute!");
                    
                    var ptrToken = jToken.First;
                    if (ptrToken == null)
                    {
                        var arrayLen = sa.ArrayPayload;
                        if (arrayLen <= 0)
                        {
                            Debug.LogWarning("没有绑定Array的初始化大小,故程序以1的数组大小构建了对象.");
                            arrayLen = 1;
                        }

                        list = Array.CreateInstance(elementType, arrayLen);
                    }
                    else
                    {
                        IList temp = new List<object>();
                        while (ptrToken != null)
                        {
                            temp.Add(BuildObject(ptrToken, elementType));
                            ptrToken = ptrToken.Next;
                        }

                        list = Array.CreateInstance(elementType, temp.Count);
                        for (var i = 0; i < temp.Count; i++)
                        {
                            ((IList) list)[i] = temp[i];
                        }
                    }
                }
                else
                { 
                    list = Activator.CreateInstance(rootType);
                    var contentType = rootType.GetGenericArguments()[0];
                    var ptrToken = jToken.First;
                    while (ptrToken != null)
                    {
                        ((IList) list).Add(BuildObject(ptrToken, contentType));
                        ptrToken = ptrToken.Next;
                    }
                }
                return list;
            }

            //如果是类,则递归调用
            var obj = Activator.CreateInstance(rootType);
            if(obj == null) throw new Exception($"{rootType} without default constructor!");
            foreach (var field in fields)
            {
                var fieldToken = jToken.SelectToken(field.Name);
                var att = field.GetCustomAttribute(typeof(SaveAttribute)) as SaveAttribute;
                if (att == null) throw new Exception("");
                if (fieldToken != null)
                {
                    field.SetValue(obj, BuildObject(fieldToken, field.FieldType, att));
                }
                else
                {
                    if (att.DefaultValue != null)
                        field.SetValue(obj, att.DefaultValue);
                    else
                        field.SetValue(obj, ConfigJson.GetDefault(field.FieldType));
                }
            }
            
            foreach (var method in methods)
            {
                var loadAtt = method.GetCustomAttribute(typeof(OnLoadAttribute)) as OnLoadAttribute;
                if (loadAtt == null) throw new Exception();
                var saveMethod = rootType.GetMethod(loadAtt.FuncName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (saveMethod == null) throw new Exception();
                var saveAtt = saveMethod.GetCustomAttribute(typeof(OnSaveAttribute)) as OnSaveAttribute;
                if (method.GetParameters().Length != 1) throw new Exception();
                if (method.GetParameters()[0].ParameterType != saveMethod.ReturnType) throw new Exception();
                if (saveAtt == null) throw new Exception();
                var methodToken = jToken.SelectToken(saveMethod.Name);
                if (methodToken != null)
                {
                    method.Invoke(obj, new[] {BuildObject(methodToken, saveMethod.ReturnType)});
                }
            }
            
            
            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;
                var propertyToken = jToken.SelectToken(property.Name);
                if (propertyToken != null)
                {
                    property.SetValue(obj,BuildObject(propertyToken, property.PropertyType));
                }
                else
                {
                    var att = property.GetCustomAttribute(typeof(SaveAttribute)) as SaveAttribute;
                    if (att == null) throw new Exception();
                    property.SetValue(obj, att.DefaultValue ?? ConfigJson.GetDefault(property.PropertyType));
                }
            }

            return obj;
        }
        
#endregion
        
#region InsertObject

        public static void InsertObject(this object rootObject,string jsonPath)
        {
            try
            {
                JToken rootToken;
                using (var file = File.OpenText(jsonPath))
                using (var reader = new JsonTextReader(file))
                {
                    rootToken = JToken.ReadFrom(reader);
                }
                InsertObject(rootToken,rootObject);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void InsertObject(JToken jToken, object rootObject)
        {
            var rootType = rootObject.GetType();
            var tempFields = rootType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = Array.FindAll(tempFields, item => item.IsDefined(typeof(SaveAttribute), false));
            var tempMethods = rootType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var methods = Array.FindAll(tempMethods, item => item.IsDefined(typeof(OnLoadAttribute), false));
            var tempProperties = rootType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var properties = Array.FindAll(tempProperties, item => item.IsDefined(typeof(SaveAttribute), false));
            var listFlag = typeof(IList).IsAssignableFrom(rootType);
            var classFlag = fields.Length + methods.Length + properties.Length > 0;
            //如果是值类型，没救,没法插入要素
            if (!listFlag && !classFlag) return;

            //如果是列表类
            if (listFlag)
            {
                var list = (IList) rootObject;
                var ptrToken = jToken.First;
                foreach (var obj in list)
                {
                    if(ptrToken==null) break;
                    InsertObject(ptrToken,obj);
                    ptrToken = ptrToken.Next;
                }
                return;
            }

            //如果是类
            foreach (var field in fields)
            {
                var fieldToken = jToken.SelectToken(field.Name);
                if (fieldToken != null) field.SetValue(rootObject, BuildObject(fieldToken, field.FieldType));
            }
            
            foreach (var method in methods)
            {
                var loadAtt = method.GetCustomAttribute(typeof(OnLoadAttribute)) as OnLoadAttribute;
                if (loadAtt == null) throw new Exception();
                var saveMethod = rootType.GetMethod(loadAtt.FuncName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (saveMethod == null) throw new Exception();
                var saveAtt = saveMethod.GetCustomAttribute(typeof(OnSaveAttribute)) as OnSaveAttribute;
                if (method.GetParameters().Length != 1) throw new Exception();
                if (method.GetParameters()[0].ParameterType != saveMethod.ReturnType) throw new Exception();
                if (saveAtt == null) throw new Exception();
                var methodToken = jToken.SelectToken(saveMethod.Name);
                if (methodToken != null) method.Invoke(rootObject, new[] {BuildObject(methodToken, saveMethod.ReturnType)});
            }
            
            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;
                var propertyToken = jToken.SelectToken(property.Name);
                if (propertyToken != null) property.SetValue(rootObject,BuildObject(propertyToken, property.PropertyType));
            }
        }

#endregion
        
    }
}
