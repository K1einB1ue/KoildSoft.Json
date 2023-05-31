using System;
using System.Collections;
using System.Reflection;
using KolidSoft.Json.Config;
using Newtonsoft.Json.Linq;

namespace KolidSoft.Json.Builder
{
    public static class JTokenBuilder
    {

        public static JToken BuildJToken(this object target, SaveTargets saveTargets = SaveTargets.Any)
        {
            var writer = new JTokenWriter();
            BuildJToken(target, ref writer, saveTargets);
            if (writer.Token == null) throw new Exception();
            return writer.Token;
        }

        public static JToken BuildJToken(Type targetType, SaveTargets saveTargets = SaveTargets.Any)
        {
            var writer = new JTokenWriter();
            BuildJToken(targetType, ref writer, null, saveTargets);
            if (writer.Token == null) throw new Exception();
            return writer.Token;
        }

        private static void BuildJToken(Type rootType, ref JTokenWriter writer, SaveAttribute externInfo = null, SaveTargets saveTargets = SaveTargets.Any)
        {
            var tempFields = rootType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = Array.FindAll(tempFields, item => item.IsDefined(typeof(SaveAttribute), false));
            var tempMethods = rootType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var methods = Array.FindAll(tempMethods, item => item.IsDefined(typeof(OnSaveAttribute), false));
            var tempProperties = rootType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var properties = Array.FindAll(tempProperties, item => item.IsDefined(typeof(SaveAttribute), false));
            var listFlag = typeof(IList).IsAssignableFrom(rootType);
            var classFlag = fields.Length + methods.Length + properties.Length > 0;
            //如果是值类型，则创建默认值JValueToken
            if (!listFlag && !classFlag)
            {
                if (externInfo != null)
                {
                    if (externInfo.DefaultValue != null)
                    {
                        writer.WriteValue(externInfo.DefaultValue);
                    }
                    else
                    {
                        writer.WriteValue(ConfigJson.GetDefault(rootType)); 
                    }
                }
                else
                {
                    writer.WriteValue(ConfigJson.GetDefault(rootType));
                }

                return;
            }

            //如果是列表类，则创建空JArrayToken
            if (listFlag)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            //如果是类,则递归调用
            if (classFlag)
            {
                writer.WriteStartObject();
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute(typeof(SaveAttribute), true) is not SaveAttribute att) throw new Exception();
                    if ((att.SaveTargets & saveTargets) == 0) continue;
                    writer.WritePropertyName(field.Name);
                    BuildJToken(field.FieldType, ref writer, att, att.SaveTargets & saveTargets);
                }
                /*
                for (int i = 0; i < methods.Count; i++)
                {
                    var att = methods[i].GetCustomAttribute(typeof(OnSaveAttribute)) as OnSaveAttribute;
                    if (att == null) throw new Exception();
                    if ((att.saveTargets & saveTargets) != 0)
                    {
                        if (methods[i].ReturnType != typeof(void))
                        {
                            writer.WritePropertyName(methods[i].Name);
                            ToJson(methods[i].ReturnType, ref writer, att.saveTargets & saveTargets);
                        }
                    }
                }

                for (int i = 0; i < properties.Count; i++)
                {
                    var att = properties[i].GetCustomAttribute(typeof(SaveAttribute), true) as SaveAttribute;
                    if (att == null) throw new Exception();
                    if (properties[i].CanRead)
                    {
                        if ((att.saveTargets & saveTargets) != 0)
                        {
                            writer.WritePropertyName(properties[i].Name);
                            ToJson(properties[i].PropertyType, ref writer, att, att.saveTargets & saveTargets);

                        }
                    }
                }
                */
                writer.WriteEndObject();
            }
        }


        private static void BuildJToken(object target, ref JTokenWriter writer, SaveTargets saveTargets = SaveTargets.Any)
        {
            var tempFields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = Array.FindAll(tempFields, item => item.IsDefined(typeof(SaveAttribute), false));
            var tempMethods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var methods = Array.FindAll(tempMethods, item => item.IsDefined(typeof(OnSaveAttribute), false));
            var tempProperties = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var properties = Array.FindAll(tempProperties, item => item.IsDefined(typeof(SaveAttribute), false));
            var listFlag = target is IList;
            var classFlag = fields.Length + methods.Length + properties.Length > 0;
            //如果是值类型，则target转换为JValueToken
            if (!listFlag && !classFlag)
            {
                writer.WriteValue(target);
                return;
            }

            //如果是列表类，则循环创建target下的item的ToJson实现
            if (listFlag)
            {
                writer.WriteStartArray();
                IList list = (IList) target;
                for (int i = 0; i < list.Count; i++)
                {
                    BuildJToken(list[i], ref writer);
                }

                writer.WriteEndArray();
                return;
            }

            //如果是类,则递归调用
            if (classFlag)
            {
                writer.WriteStartObject();
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute(typeof(SaveAttribute)) is not SaveAttribute att) throw new Exception();
                    if ((att.SaveTargets & saveTargets) == 0) continue;
                    writer.WritePropertyName(field.Name);
                    var value = field.GetValue(target);
                    if (value != null) BuildJToken(value, ref writer, att.SaveTargets & saveTargets);
                    else BuildJToken(field.FieldType, ref writer, att.SaveTargets & saveTargets);
                }

                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute(typeof(OnSaveAttribute)) is not OnSaveAttribute att) throw new Exception();
                    if ((att.SaveTargets & saveTargets) == 0) continue;
                    if (method.ReturnType != typeof(void))
                    {
                        var value = method.Invoke(target, null);
                        if (att.SaveAddition == SaveAddition.NullToError)
                        {
                            if (value == null) throw new Exception();
                            writer.WritePropertyName(method.Name);
                            BuildJToken(value, ref writer, att.SaveTargets & saveTargets);
                        }else if (att.SaveAddition == SaveAddition.NullToDefault)
                        {
                            writer.WritePropertyName(method.Name);
                            if (value != null) BuildJToken(value, ref writer, att.SaveTargets & saveTargets);
                            else BuildJToken(method.ReturnType, ref writer, att.SaveTargets & saveTargets);
                        }else if (att.SaveAddition == SaveAddition.NullToFallback)
                        {
                            if (att.Fallback == null) throw new Exception();
                            var fallback = target.GetType().GetMethod(att.Fallback, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (fallback == null) throw new Exception();
                            if (fallback.ReturnType != method.ReturnType) throw new Exception();
                            if (fallback.GetParameters().Length != 0) throw new Exception();
                            writer.WritePropertyName(method.Name);
                            value = fallback.Invoke(target, null);
                            if (value != null) BuildJToken(value, ref writer, att.SaveTargets & saveTargets);
                            else BuildJToken(method.ReturnType, ref writer, att.SaveTargets & saveTargets);
                        }
                    }
                    else
                    {
                        method.Invoke(target, null);
                    }
                }
                
                foreach (var property in properties)
                {
                    var att = property.GetCustomAttribute(typeof(SaveAttribute), true) as SaveAttribute;
                    if (att == null) throw new Exception();
                    if (!property.CanRead) continue;
                    if ((att.SaveTargets & saveTargets) == 0) continue;
                    writer.WritePropertyName(property.Name);
                    var value = property.GetMethod.Invoke(target, null);
                    if (value != null) BuildJToken(value, ref writer, att.SaveTargets & saveTargets);
                    else BuildJToken(property.PropertyType, ref writer, att.SaveTargets & saveTargets);
                }

                writer.WriteEndObject();
            }

        }
    }
}
