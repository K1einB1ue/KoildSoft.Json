using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualListNode : VisualNode
    {
        //这是把列表填回去
        private readonly FieldInfo _fieldInfo;
        private Type _elementType;
        private List<object> _elementTarget = new();
        private List<VisualNode> _visualNodes = new();
        private Foldout _foldout = new();
        private ListView _listView = new();

        private IList BindList => (IList) _fieldInfo.GetValue(BindTarget);





        public VisualListNode(object bindTarget, FieldInfo fieldInfo, Type elementType,
            VisualInitConfig visualInitConfig) : base(
            bindTarget, visualInitConfig)
        {
            _fieldInfo = fieldInfo;
            var additionName = visualInitConfig.fieldAdditionName.Invoke(_fieldInfo);
            if (additionName != null)
            {
                _foldout.text = $"{additionName}:{fieldInfo.Name}";
            }
            else
            {
                _foldout.text = fieldInfo.Name;
            }

            _elementType = elementType;
            _listView.showBorder = true;


            var bindList = BindList;

            for (var index = 0; index < bindList.Count; index++)
            {
                var element = bindList[index];
                _elementTarget.Add(element);
                if (_elementType.IsDefined(typeof(CustomNodeAttribute), false))
                {
                    var att = (CustomNodeAttribute) _elementType.GetCustomAttribute(typeof(CustomNodeAttribute), false);
                    var con = att.EditorType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                        null, CallingConventions.HasThis, CustomNodeAttribute.ConstructorArgs, null);
                    if (con == null) throw new Exception("Don't change constructor!");
                    CustomNodeInfo customNodeInfo = new CustomNodeInfo()
                    {
                        isListNode = true,
                        index = index,
                        elementType = _elementType
                    };
                    var node = (CustomNodeEditor) con.Invoke(new[]
                        {(object) _elementTarget, customNodeInfo, visualInitConfig});
                    _visualNodes.Add(node);
                }
                else if (typeof(IList).IsAssignableFrom(_elementType))
                {
                    Type newElementType;
                    if (_elementType.IsArray)
                    {
                        newElementType = _elementType.GetElementType();
                    }
                    else
                    {
                        newElementType = _elementType.GetGenericArguments()[0];
                    }

                    var vn = new VisualListListNode(_elementTarget, newElementType, visualInitConfig)
                    {
                        Index = index
                    };
                    _visualNodes.Add(vn);
                }
                else if (_elementType.IsClass && _elementType != typeof(string))
                {
                    var vn = new VisualListClassNode(_elementTarget, _elementType, visualInitConfig)
                    {
                        Index = index
                    };
                    _visualNodes.Add(vn);
                }
                else
                {
                    var vn = new VisualListFieldNode(_elementTarget, _elementType, visualInitConfig)
                    {
                        Index = index
                    };
                    _visualNodes.Add(vn);
                }
            }

            _listView.makeItem += () => new VisualElement();
            _listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _listView.bindItem += (ve, index) =>
            {
                ve.Clear();
                ve.Add(_visualNodes[index].VisualElement);
            };
            /*
            _listView.reorderable = true;
            _listView.reorderMode = ListViewReorderMode.Animated;
            */
            _listView.selectionType = SelectionType.None;
            _listView.itemsSource = _visualNodes;
            _foldout.Add(_listView);
        }


        public override VisualElement VisualElement => _foldout;

        public override void OnSave()
        {
            foreach (var node in _visualNodes)
            {
                node.OnSave();
            }

            var index = 0;
            var bindList = BindList;
            var bindTargetLen = bindList.Count;
            for (; index < _elementTarget.Count; index++)
            {
                var mapping = ((IVisualListNode) _visualNodes[index]).Index;
                if (index < bindTargetLen)
                {
                    bindList[index] = _elementTarget[mapping];
                }
                else
                {
                    bindList.Add(_elementTarget[mapping]);
                }
            }


            for (; index < bindTargetLen; bindTargetLen--)
            {
                bindList.RemoveAt(bindTargetLen - 1);
            }


        }

        public override void BuildVisualElement(VisualBuildConfig visualBuildConfig)
        {
            //_foldout.Clear();
            var bindList = BindList;
            for (var index = 0; index < bindList.Count; index++)
            {
                var node = _visualNodes[index];
                ((IVisualListNode) node).Index = index;
                node.BuildVisualElement(visualBuildConfig);
                //_foldout.Add(node.VisualElement);
            }

        }
    }
}