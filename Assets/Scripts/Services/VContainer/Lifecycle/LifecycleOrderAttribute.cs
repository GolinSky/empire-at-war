using System;

namespace EmpireAtWar.Lifecycle
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class LifecycleOrderAttribute : Attribute
    {
        public int Initialize { get; set; }
        public int Tick { get; set; }
        public int FixedTick { get; set; }
        public int LateTick { get; set; }
        public int Dispose { get; set; }
        public int LateDispose { get; set; }
    }
}
