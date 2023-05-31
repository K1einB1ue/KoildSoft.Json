using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualListClassNode : VisualNode, IVisualListNode
    {
        private int _index;
        private Type _elementType;
        private VisualElement _visualElement = new();
        private List<VisualNode> _visualNodes = new();

        public int Index
        {
            get => _index;
            set => _index = value;
        }

        public object CurrentElementTarget
        {
            get
            {
                if (BindTarget == null) return null;
                return ((IList) BindTarget)[_index];
            }
        }

        public VisualListClassNode(object bindTarget, Type elementType, VisualInitConfig visualInitConfig) : base(
            bindTarget, visualInitConfig)
        {
            _elementType = elementType;
            var tempFields = _elementType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = Array.FindAll(tempFields, item => visualInitConfig.fieldFilter(item));
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                if (field.FieldType.IsDefined(typeof(CustomNodeAttribute), false))
                {
                    var att = (CustomNodeAttribute) field.FieldType.GetCustomAttribute(typeof(CustomNodeAttribute),
                        false);
                    var con = att.EditorType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                        null, CallingConventions.HasThis, CustomNodeAttribute.ConstructorArgs, null);
                    if (con == null) throw new Exception("Don't change constructor!");
                    CustomNodeInfo customNodeInfo = new CustomNodeInfo()
                    {
                        isListNode = false,
                        fieldInfo = field
                    };
                    var node = (CustomNodeEditor) con.Invoke(new[] {bindTarget, customNodeInfo, visualInitConfig});
                    _visualNodes.Add(node);
                }
                else if (typeof(IList).IsAssignableFrom(fieldType))
                {
                    Type newElementType;
                    if (field.FieldType.IsArray)
                    {
                        newElementType = field.FieldType.GetElementType();
                    }
                    else
                    {
                        newElementType = field.FieldType.GetGenericArguments()[0];
                    }

                    if (newElementType == null) throw new Exception("Missing elementType!");
                    _visualNodes.Add(new VisualListNode(CurrentElementTarget, field, newElementType, visualInitConfig));
                }
                else if (fieldType.IsClass && fieldType != typeof(string))
                {
                    _visualNodes.Add(new VisualFieldClassNode(field.GetValue(CurrentElementTarget), field,
                        visualInitConfig));
                }
                else
                {
                    _visualNodes.Add(new VisualFieldNode(CurrentElementTarget, field, visualInitConfig));
                }
            }
        }


        public override VisualElement VisualElement => _visualElement;

        public override void OnSave()
        {
            foreach (var node in _visualNodes)
            {
                node.OnSave();
            }
        }

        public override void BuildVisualElement(VisualBuildConfig visualBuildConfig)
        {
            _visualElement.Clear();
            foreach (var node in _visualNodes)
            {
                node.BindTarget = CurrentElementTarget;
                node.BuildVisualElement(visualBuildConfig);
                _visualElement.Add(node.VisualElement);
            }
        }
    }
}