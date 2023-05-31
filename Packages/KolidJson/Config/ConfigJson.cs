using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KolidSoft.Json.Config
{
    public static class ConfigJson
    {
        private static readonly Dictionary<Type, object> FallbackValue = new()
        {
            {typeof(string), ""}
        };

        public static object GetDefault(Type type)
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                if (FallbackValue.TryGetValue(type, out var value))
                    return value;
                throw new Exception($"[JsonBuilder] \"{type}\" without fallback value.");
            }
        }
    }
}
