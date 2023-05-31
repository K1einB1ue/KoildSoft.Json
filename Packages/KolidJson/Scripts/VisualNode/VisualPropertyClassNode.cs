using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualPropertyClassNode : VisualNode
    {
        private VisualElement _visualElement;
        private PropertyInfo _propertyInfo;
        private List<VisualNode> _visualNodes = new();

        public VisualPropertyClassNode(object bindTarget, PropertyInfo propertyInfo, VisualInitConfig visualInitConfig) : base(bindTarget, visualInitConfig)
        {
            if (propertyInfo != null)
            {
                var foldout = new Foldout();
                _propertyInfo = propertyInfo;
                var additionName = visualInitConfig.propertyAdditionName.Invoke(_propertyInfo);
                if (additionName != null)
                {
                    foldout.text = $"{additionName}:{_propertyInfo.Name}";
                }
                else
                {
                    foldout.text = _propertyInfo.Name;
                }

                _visualElement = foldout;
            }
            else
            {
                _visualElement = new VisualElement();
            }



            if (propertyInfo == null) throw new Exception();
            bindTarget = propertyInfo.GetValue(bindTarget);

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
                    _visualNodes.Add(new VisualFieldClassNode(field.GetValue(bindTarget), field, visualInitConfig));
                }
                else
                {
                    _visualNodes.Add(new VisualFieldNode(bindTarget, field, visualInitConfig));
                }
            }

            foreach (var property in properties)
            {
                if (property.PropertyType.IsClass || typeof(IList).IsAssignableFrom(property.PropertyType))
                {
                    if ((property.PropertyType.IsClass || typeof(IList).IsAssignableFrom(property.PropertyType)) &&
                        property.PropertyType != typeof(string))
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