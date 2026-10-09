using Unity.Collections;

namespace EmpireAtWar.Utils
{
    internal static class NativeArrayBuffer
    {
        public static void EnsureCapacity<T>(ref NativeArray<T> buffer, int requiredCapacity) where T : struct
        {
            if (buffer.IsCreated && buffer.Length >= requiredCapacity) return;
            if (buffer.IsCreated) buffer.Dispose();
            int capacity = 8;
            while (capacity < requiredCapacity) capacity *= 2;
            buffer = new NativeArray<T>(capacity, Allocator.Persistent);
        }
    }
}
