using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor
{
    // Keep the original window's lifecycle and state while presenting its UI inside the hub.
    internal sealed class EditorToolWindowHost : IDisposable
    {
        private readonly Dictionary<Type, EditorWindow> _windows = new Dictionary<Type, EditorWindow>();
        private const BindingFlags UI_METHOD_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public bool HasUnsavedChanges => _windows.Values.Any(window => window.hasUnsavedChanges);
        public string SaveChangesMessage => string.Join("\n", _windows.Values
            .Where(window => window.hasUnsavedChanges).Select(window => window.saveChangesMessage));

        public VisualElement GetContent(Type type)
        {
            if (_windows.TryGetValue(type, out EditorWindow existing)) return existing.rootVisualElement;

            var window = (EditorWindow)ScriptableObject.CreateInstance(type);
            window.hideFlags = HideFlags.HideAndDontSave;
            _windows.Add(type, window);
            VisualElement root = window.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.minHeight = 0;
            MethodInfo createGui = type.GetMethod("CreateGUI", UI_METHOD_FLAGS);
            if (createGui != null)
                ((Action)createGui.CreateDelegate(typeof(Action), window))();
            else
            {
                MethodInfo onGui = type.GetMethod("OnGUI", UI_METHOD_FLAGS);
                var container = new IMGUIContainer((Action)onGui.CreateDelegate(typeof(Action), window));
                container.style.flexGrow = 1;
                root.Add(container);
            }
            return root;
        }

        public void SaveChanges()
        {
            foreach (EditorWindow window in _windows.Values.Where(window => window.hasUnsavedChanges))
                window.SaveChanges();
        }

        public void DiscardChanges()
        {
            foreach (EditorWindow window in _windows.Values.Where(window => window.hasUnsavedChanges))
                window.DiscardChanges();
        }

        public void Dispose()
        {
            foreach (EditorWindow window in _windows.Values)
            {
                window.rootVisualElement.RemoveFromHierarchy();
                UnityEngine.Object.DestroyImmediate(window);
            }
            _windows.Clear();
        }
    }
}
