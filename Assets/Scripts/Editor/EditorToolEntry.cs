using System;
using System.Reflection;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    public sealed class EditorToolEntry
    {
        private readonly Func<bool> _validate;

        public string MenuPath { get; }
        public string Category { get; }
        public string Group { get; }
        public string Title { get; }
        public string Description { get; }
        public Type WindowType { get; }
        public bool IsLegacy => Category == "Legacy";
        public bool IsEnabled => _validate == null || _validate();

        public EditorToolEntry(string menuPath, MethodInfo method, MethodInfo validator)
        {
            MenuPath = menuPath;
            string[] parts = menuPath.Substring(EditorToolCatalog.MENU_ROOT.Length).Split('/');
            Category = parts[0];
            Group = parts.Length > 2 ? string.Join(" / ", parts, 1, parts.Length - 2) : "Editors";
            Title = parts[parts.Length - 1];
            EditorToolInfoAttribute info = method.GetCustomAttribute<EditorToolInfoAttribute>()
                ?? method.DeclaringType.GetCustomAttribute<EditorToolInfoAttribute>();
            Description = info == null ? "Open the existing project editor." : info.Description;
            WindowType = typeof(EditorWindow).IsAssignableFrom(method.DeclaringType)
                ? method.DeclaringType : info == null ? null : info.WindowType;
            if (validator != null)
                _validate = (Func<bool>)validator.CreateDelegate(typeof(Func<bool>));
        }
    }
}
