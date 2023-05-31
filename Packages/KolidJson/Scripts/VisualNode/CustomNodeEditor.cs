using System;
using System.Collections;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomNodeAttribute : Attribute
    {
        public static readonly Type[] ConstructorArgs = {typeof(object), typeof(CustomNodeInfo), typeof(VisualInitConfig)};
        
        public readonly Type EditorType;
        public CustomNodeAttribute(Type editorType)
        {
            if (!typeof(CustomNodeEditor).IsAssignableFrom(editorType))
            {
                throw new Exception("This is not a CustomNodeEditor type!");
            }

            EditorType = editorType;
        }
    }

    public class CustomNodeInfo
    {
        public bool isListNode;
        public FieldInfo fieldInfo;
        public PropertyInfo propertyInfo;
        public int index;
        public Type elementType;
    }

    public abstract class CustomNodeEditor : VisualNode, IVisualListNode
    {
        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;
        private VisualElement _visualElement;

        int IVisualListNode.Index
        {
            get => _index;
            set => _index = value;
        }

        private readonly bool _isListNode = false;
        private int _index;
        private Type _elementType;

        public CustomNodeEditor(object bindTarget, CustomNodeInfo customNodeInfo, VisualInitConfig visualInitConfig) : base(bindTarget, visualInitConfig)
        {
            _isListNode = customNodeInfo.isListNode;
            if (_isListNode)
            {
                _index = customNodeInfo.index;
                _elementType = customNodeInfo.elementType;
            }
            else
            {
                _fieldInfo = customNodeInfo.fieldInfo;
                _propertyInfo = customNodeInfo.propertyInfo;
            }
        }

        protected void NormalFramework(VisualInitConfig visualInitConfig)
        {
            if (!_isListNode)
            {
                var foldout = new Foldout();
                _visualElement = new Foldout();
                if (_fieldInfo != null)
                {
                    var additionName = visualInitConfig.fieldAdditionName.Invoke(_fieldInfo);
                    if (additionName != null) foldout.text = $"{additionName}:{_fieldInfo.Name}";
                    else foldout.text = _fieldInfo.Name;
                }else if (_propertyInfo != null)
                {
                    var additionName = visualInitConfig.propertyAdditionName.Invoke(_propertyInfo);
                    if (additionName != null) foldout.text = $"{additionName}:{_propertyInfo.Name}";
                    else foldout.text = _propertyInfo.Name;
                }

                _visualElement = foldout;
            }
            else
            {
                _visualElement = new VisualElement();
            }
        }

        protected void NewFramework(VisualElement visualElement)
        {
            _visualElement = visualElement;
        }


        protected object Target
        {
            get
            {
                if (_isListNode) return ((IList) BindTarget)[_index];
                if (_fieldInfo != null) return _fieldInfo.GetValue(BindTarget);
                return _propertyInfo.GetValue(BindTarget);
            }
        } 

        public sealed override VisualElement VisualElement => _visualElement;
    }

}



