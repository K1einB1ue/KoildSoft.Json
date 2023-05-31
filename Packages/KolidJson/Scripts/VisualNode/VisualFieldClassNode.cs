using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualFieldClassNode : VisualNode
    {
        private readonly VisualElement _visualElement;
        private readonly FieldInfo _fieldInfo;
        private readonly List<VisualNode> _visualNodes = new();


        public VisualFieldClassNode(object bindTarget, FieldInfo fieldInfo, VisualInitConfig visualInitConfig) : base(
            bindTarget,
            visualInitConfig)
        {
            if (fieldInfo != null)
            {
                var foldout = new Foldout();
                _fieldInfo = fieldInfo;
                var additionName = visualInitConfig.fieldAdditionName.Invoke(_fieldInfo);
                if (additionName != null)
                {
                    foldout.text = $"{additionName}:{fieldInfo.Name}";
                }
                else
                {
                    foldout.text = fieldInfo.Name;
                }

                _visualElement = foldout;
            }
            else
            {
                _visualElement = new VisualElement();
            }

            //这样可以错开一个位置,让visualInstance里的代码简单点..
            if (_fieldInfo != null)
            {
                bindTarget = _fieldInfo.GetValue(bindTarget);
            }

            var rootType = bindTarget.GetType();
            var tempFields = rootType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fields = Array.FindAll(tempFields, item => visualInitConfig.fieldFilter(item));
            var tempProperties = rootType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var properties = Array.FindAll(tempProperties, item => visualInitConfig.propertyFilter(item));

            foreach (var field in fields)
            {
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
                else if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    Type elementType;
                    if (field.FieldType.IsArray)
                    {
                        elementType = field.FieldType.GetElementType();
                    }
                    else
                    {
                        elementType = field.FieldType.GetGenericArguments()[0];
                    }

                    if (elementType == null) throw new Exception("Missing elementType!");

                    _visualNodes.Add(new VisualListNode(bindTarget, field, elementType, visualInitConfig));

                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    _visualNodes.Add(new VisualFieldClassNode(bindTarget, field, visualInitConfig));
                }
                else
                {
                    _visualNodes.Add(new VisualFieldNode(bindTarget, field, visualInitConfig));
                }
            }

            foreach (var property in properties)
            {
                if ((property.PropertyType.IsClass || typeof(IList).IsAssignableFrom(property.PropertyType)) &&
                    property.PropertyType != typeof(string))
                {
                    if (property.PropertyType.IsDefined(typeof(CustomNodeAttribute), false))
                    {
                        if (!property.CanRead) throw new Exception();
                        var att = (CustomNodeAttribute) property.PropertyType.GetCustomAttribute(
                            typeof(CustomNodeAttribute), false);
                        var con = att.EditorType.GetConstructor(
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                            null, CallingConventions.HasThis, CustomNodeAttribute.ConstructorArgs, null);
                        if (con == null) throw new Exception("Don't change constructor!");
                        CustomNodeInfo customNodeInfo = new CustomNodeInfo()
                        {
                            isListNode = false,
                            propertyInfo = property
                        };
                        var node = (CustomNodeEditor) con.Invoke(new[] {bindTarget, customNodeInfo, visualInitConfig});
                        _visualNodes.Add(node);
                    }
                    else if (property.CanRead)
                    {
                        _visualNodes.Add(new VisualPropertyClassNode(bindTarget, property, visualInitConfig));
                    }
                }
                else
                {
                    _visualNodes.Add(new VisualPropertyNode(bindTarget, property, visualInitConfig));
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
                node.BuildVisualElement(visualBuildConfig);
                _visualElement.Add(node.VisualElement);
            }
        }
    }
}