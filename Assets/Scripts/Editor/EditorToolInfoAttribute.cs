using System;

namespace EmpireAtWar.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class EditorToolInfoAttribute : Attribute
    {
        public string Description { get; }
        public Type WindowType { get; }

        public EditorToolInfoAttribute(string description, Type windowType = null)
        {
            Description = description;
            WindowType = windowType;
        }
    }
}
