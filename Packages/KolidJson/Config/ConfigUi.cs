#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using KolidSoft.Json.Builder;
using KolidSoft.Json.Export;
using KolidSoft.Json.UI;
using UnityEditor;
using UnityEngine;

namespace KolidSoft.Json.Config
{
    public static class ConfigUi
    {

        public static Color Red = new Color(137 / 255f, 39 / 255f, 17 / 255f, 1f);
        public static Color Green = new Color(52 / 255f, 132 / 255f, 51 / 255f, 1f);
        public static Color Yellow = new Color(171 / 255f, 137 / 255f, 52 / 255f, 1f);

        public static string? DefaultFieldAdditionName(FieldInfo fieldInfo)
        {
            var att = (SaveAttribute) fieldInfo.GetCustomAttribute(typeof(SaveAttribute), false);
            return att.SaveTargets switch
            {
                SaveTargets.Dynamic => "[Dynamic]",
                SaveTargets.Static => "[Static]",
                _ => null
            };
        }
        
        public static string? DefaultPropertyAdditionName(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttribute(typeof(VisualAttribute), false) is VisualAttribute attVisual) return "[Visual]";
            var attSave = (SaveAttribute) propertyInfo.GetCustomAttribute(typeof(SaveAttribute), false);
            return attSave.SaveTargets switch
            {
                SaveTargets.Dynamic => "[Dynamic]",
                SaveTargets.Static => "[Static]",
                _ => null
            };
        }
        
        
        public static bool DefaultFieldFilter(FieldInfo fieldInfo)
        {
            return fieldInfo.IsDefined(typeof(SaveAttribute), false);
        }
        
        public static bool DefaultPropertyFilter(PropertyInfo propertyInfo)
        {
            return propertyInfo.IsDefined(typeof(VisualAttribute), false) ||
                   propertyInfo.IsDefined(typeof(SaveAttribute), false);
        }

        private static readonly HashSet<string> DisableSet = new()
        {
            "uuid",
            "dateInfo",
            "typeInfo"
        };
        
        public static bool DefaultEnableFilter(FieldInfo fieldInfo)
        {
            return !DisableSet.Contains(fieldInfo.Name);
        }

    }
}

