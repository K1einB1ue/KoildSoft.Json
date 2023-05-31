using System;
using System.Collections;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualListFieldNode : VisualNode, IVisualListNode
    {
        private int _index;
        private readonly Type _elementType;
        private readonly TextField _textField = new();

        public int Index
        {
            get => _index;
            set => _index = value;
        }

        public VisualListFieldNode(object bindTarget, Type elementType, VisualInitConfig visualInitConfig) : base(
            bindTarget, visualInitConfig)
        {
            _elementType = elementType;
        }

        public override VisualElement VisualElement => _textField;

        public override void OnSave()
        {
            ((IList) BindTarget)[_index] = Convert.ChangeType(_textField.value, _elementType);
        }

        public override void BuildVisualElement(VisualBuildConfig visualBuildConfig)
        {
            _textField.label = _elementType.ToString();
            if (BindTarget != null)
            {
                _textField.value = ((IList) BindTarget)[_index].ToString();
            }
            else
            {
                _textField.value = "NULL";
            }
        }
    }
}