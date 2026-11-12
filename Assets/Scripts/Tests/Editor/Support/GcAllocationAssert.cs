using System;
using NUnit.Framework;
using UnityEngine.Profiling;

namespace EmpireAtWar.Tests.Editor
{
    internal static class GcAllocationAssert
    {
        public static void DoesNotAllocate(Action action)
        {
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            action();
            int control = Count(recorder, () => GC.KeepAlive(new byte[1024]));
            Assert.That(control, Is.GreaterThan(0), "GC.Alloc recorder did not detect the allocating control.");
            int count = Count(recorder, action);
            Assert.That(count, Is.Zero, "Measured action allocated managed memory.");
        }

        private static int Count(Recorder recorder, Action action)
        {
            recorder.enabled = false;
            recorder.enabled = true;
            try { action(); }
            finally { recorder.enabled = false; }
            return recorder.sampleBlockCount;
        }
    }
}
