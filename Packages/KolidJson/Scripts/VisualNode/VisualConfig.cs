
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using KolidSoft.Json.Config;

namespace KolidSoft.Json.UI
{
    public class VisualInitConfig
    {
        public Func<FieldInfo, bool> fieldFilter;
        public Func<PropertyInfo, bool> propertyFilter;
        public Func<FieldInfo, string?> fieldAdditionName;
        public Func<PropertyInfo, string?> propertyAdditionName;

        private readonly VisualInstance _instance;
        public VisualInstance VisualInstance => _instance;
        public VisualInitConfig(VisualInstance instance)
        {
            _instance = instance;
            fieldFilter = ConfigUi.DefaultFieldFilter;
            propertyFilter = ConfigUi.DefaultPropertyFilter;
            fieldAdditionName = ConfigUi.DefaultFieldAdditionName;
            propertyAdditionName = ConfigUi.DefaultPropertyAdditionName;
        }
    }
    
    public class VisualBuildConfig
    {
        public Func<FieldInfo, bool> enableFilter;
        private readonly VisualInstance _instance;
        public VisualInstance VisualInstance => _instance;
        public VisualBuildConfig(VisualInstance instance)
        {
            _instance = instance;
            enableFilter = ConfigUi.DefaultEnableFilter;
        }

    }
    
}


