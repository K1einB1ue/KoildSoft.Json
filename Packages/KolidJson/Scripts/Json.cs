using System;


#nullable enable

namespace KolidSoft.Json
{
    [Flags]
    public enum SaveTargets
    {
        Editor  = 1<<0,
        Static  = 1<<1,
        Dynamic = 1<<2,
        
        Custom0 = 1<<10,
        Custom1 = 1<<11,
        Custom2 = 1<<12,
        Custom3 = 1<<13,
        Custom4 = 1<<14,
        Custom5 = 1<<15,
        Custom6 = 1<<16,
        Custom7 = 1<<17,
        Custom8 = 1<<18,
        Custom9 = 1<<19,

        Any = Editor|Static|Dynamic|Custom0|Custom1|Custom2|Custom3|Custom4|Custom5|Custom6|Custom7|Custom8|Custom9
    }

    public enum SaveAddition
    {
        NullToError,
        NullToDefault,
        NullToFallback,
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveAttribute : Attribute
    {
        //当没有在Json文件中找到对应的值时,使用该对象构造默认值.
        public object? DefaultValue;
        //用于将存储分为Static和Dynamic.
        public SaveTargets SaveTargets = SaveTargets.Any;
        
        
        
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveArrayAttribute : SaveAttribute
    {
        //用于设定数组的大小.
        public int ArrayPayload = -1;
        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class OnSaveAttribute : Attribute
    {
        public SaveTargets SaveTargets = SaveTargets.Any;
        public SaveAddition SaveAddition = SaveAddition.NullToError;
        public string? Fallback = null;
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class OnLoadAttribute : Attribute
    {
        public readonly string FuncName;
        
        public OnLoadAttribute(string funcName)
        {
            FuncName = funcName;
        }
    }
}
