using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualFieldNode : VisualNode
    {
        private readonly FieldInfo _fieldInfo;
        private readonly TextField _textField = new();

        public VisualFieldNode(object bindTarget, FieldInfo fieldInfo, VisualInitConfig visualInitConfig) : base(
            bindTarget,
            visualInitConfig)
        {
            _fieldInfo = fieldInfo;
            var additionName = visualInitConfig.fieldAdditionName.Invoke(_fieldInfo);
            if (additionName != null)
            {
                _textField.label = $"{additionName}:{_fieldInfo.Name}";
            }
            else
            {
                _textField.label = _fieldInfo.Name;
            }

        }



        public override VisualElement VisualElement => _textField;

        public override void OnSave()
        {
            try
            {
                _fieldInfo.SetValue(BindTarget, Convert.ChangeType(_textField.value, _fieldInfo.FieldType));
            }
            catch
            {
                Debug.LogError($"{_fieldInfo.Name} {_textField.value}");
            }
        }



        public override void BuildVisualElement(VisualBuildConfig visualBuildConfig)
        {
            _textField.SetEnabled(visualBuildConfig.enableFilter.Invoke(_fieldInfo));
            if (BindTarget != null)
            {
                _textField.value = _fieldInfo.GetValue(BindTarget).ToString();
            }
            else
            {
                _textField.value = "NULL";
            }

        }


    }
}