using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace KolidSoft.Json.UI
{
    public interface IVisualListNode
    {
        int Index { get; set; }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class VisualAttribute : Attribute
    {
    }

    public abstract class VisualNode
    {
        private object _bindTarget;

        public object BindTarget
        {
            get => _bindTarget;
            set => _bindTarget = value;
        }

        public abstract VisualElement VisualElement { get; }

        protected VisualNode(object bindTarget, VisualInitConfig visualInitConfig)
        {
            _bindTarget = bindTarget;
        }

        public abstract void OnSave();

        public abstract void BuildVisualElement(VisualBuildConfig visualBuildConfig);

    }
}