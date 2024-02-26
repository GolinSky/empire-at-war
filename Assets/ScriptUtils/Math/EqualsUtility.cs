using UnityEngine;

namespace ScriptUtils.Math
{
    public static class EqualsUtility
    {
        private const float MinTolerance = 0.05f;
        
        public static bool IsEqual(this float a, float b, float minTolerance = MinTolerance)
        {
            return System.Math.Abs(a - b) < minTolerance;
        }


        public static bool IsEqual(this Vector3 a, Vector3 b, float minTolerance = MinTolerance)
        {
            return IsEqual(a.x, b.x, minTolerance) && IsEqual(a.y, b.y, minTolerance) &&
                   IsEqual(a.z, b.z, minTolerance);
        }
        
    }
}