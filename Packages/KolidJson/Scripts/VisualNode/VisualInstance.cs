using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public class VisualInstance
    {
        private readonly VisualElement _container;
        private VisualElement _tools;
        private Button _back;
        private Button _reload;
        private Button _save;
        public event Action<object> OnSave;
        public event Action<object> OnPush;
        public event Action<object, object> OnPop;
        public event Action OnClear;

        private bool _needTools = true;

        public bool NeedTools
        {
            get => _needTools;
            set => _needTools = value;
        }

        private VisualFieldClassNode _root;
        private VisualInitConfig _visualInitConfig;
        private VisualBuildConfig _visualBuildConfig;

        //private object _bindTarget;
        private Stack<object> _bindTargets = new();

        public object Target
        {
            get
            {
                if (_bindTargets.Count == 0) return null;
                return _bindTargets.Peek();
            }
        }

        private object _rootTarget = null;
        public object RootTarget => _rootTarget;

        //private object CurrentTarget => _bindTargets.Count != 0 ? _bindTargets.Peek() : _bindTarget;

        public VisualInstance(VisualElement container)
        {
            _container = container;
            InitTools();
            _visualInitConfig = new(this);
            _visualBuildConfig = new(this);
        }

        private void InitTools()
        {
            _tools = new VisualElement {
                style = {flexDirection = FlexDirection.Row}
            };
            _back = new Button {
                text = "Back"
            };
            _back.clicked += Pop;

            _reload = new Button {
                text = "Reload"
            };
            _reload.clicked += Refresh;

            _save = new Button {
                text = "Save"
            };
            _save.clicked += Save;
        }

        private void ConfigTools()
        {
            _tools.Clear();
            if (_bindTargets.Count > 1) _tools.Add(_back);
            _tools.Add(_reload);
            _tools.Add(_save);
            if (_needTools) _container.Add(_tools);
        }

        public VisualElement Container => _container;

        public void Save()
        {
            _root?.OnSave();
            if (_bindTargets.Count != 0)
            {
                OnSave?.Invoke(_bindTargets.Peek());
            }
        }

        public void Refresh()
        {
            if (_root == null) throw new Exception("without root");
            _root.BuildVisualElement(_visualBuildConfig);
            _container.Clear();
            ConfigTools();
            _container.Add(_root.VisualElement);
        }

        public void Clear()
        {
            _bindTargets.Clear();
            _container.Clear();
            OnClear?.Invoke();
        }

        public void Push(object bindTarget)
        {
            OnPush?.Invoke(bindTarget);
            _bindTargets.Push(bindTarget);
            if (_bindTargets.Count == 1)
            {
                _rootTarget = bindTarget;
            }
            _root = new VisualFieldClassNode(_bindTargets.Peek(), null, _visualInitConfig);
            Refresh();
        }

        public void Pop()
        {
            var top = _bindTargets.Pop();
            if (_bindTargets.Count == 0)
            {
                _container.Clear();
                _rootTarget = null;
                return;
            }
            var newTop = _bindTargets.Peek();
            OnPop?.Invoke(top,newTop);
            _root = new VisualFieldClassNode(newTop, null, _visualInitConfig);
            Refresh();
        }

        public T Peek<T>() where T : class
        {
            return _bindTargets.Peek() as T;
        }

    }
}