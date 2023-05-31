using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KolidSoft.Utils
{
    public static partial class TypeUtils
    {
        public static List<Type> DerivedTypes(Type baseType,Assembly assembly)
        {
            var ret = new List<Type>();
            var types = assembly.GetTypes();
            foreach (var t in types)
            {
                if (t.IsSubclassOf(baseType))
                {
                    ret.Add(t);
                }   
            }
            return ret;
        }
    }
}