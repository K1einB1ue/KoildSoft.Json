
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualPropertyNode : VisualNode
    {
        private readonly PropertyInfo _propertyInfo;
        private readonly TextField _textField = new();

        public VisualPropertyNode(object bindTarget, PropertyInfo propertyInfo, VisualInitConfig visualInitConfig) : base(bindTarget, visualInitConfig)
        {
            _propertyInfo = propertyInfo;
            var additionName = visualInitConfig.propertyAdditionName.Invoke(_propertyInfo);
            if (additionName != null)
            {
                _textField.label = $"{additionName}:{_propertyInfo.Name}:{_propertyInfo.PropertyType}";
            }
            else
            {
                _textField.label = $"{_propertyInfo.Name}:{_propertyInfo.PropertyType}";
            }

        }

        public override VisualElement VisualElement => _textField;

        public override void OnSave()
        {
            try
            {
                if (_propertyInfo.CanWrite)
                {
                    if (BindTarget != null)
                    {
                        var convert = Convert.ChangeType(_textField.value, _propertyInfo.PropertyType);
                        _propertyInfo.SetValue(BindTarget, convert);
                    }
                }
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning($"{_propertyInfo.Name}:{_textField.value} is not a {_propertyInfo.PropertyType}");
            }
            catch (FormatException)
            {
                Debug.LogWarning($"{_propertyInfo.Name}:{_textField.value} is not a {_propertyInfo.PropertyType}");
            }
        }



        public override void BuildVisualElement(VisualBuildConfig visualBuildConfig)
        {
            _textField.SetEnabled(_propertyInfo.CanWrite);
            if (_propertyInfo.CanRead)
            {
                if (BindTarget != null)
                {
                    _textField.value = _propertyInfo.GetValue(BindTarget).ToString();
                }
                else
                {
                    _textField.value = "NULL";
                }
            }
            else
            {
                _textField.value = "[Missing]";
            }
        }


    }

}