using UnityEngine;

namespace ScriptUtils.Math
{
    public static class PathCalculationUtils
    {
        public static Vector3[] GetWayPointsOfBezierPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int waypointCount = 20)
        {
            Vector3[] path = new Vector3[waypointCount];
            float divider = path.Length - 1;
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / divider;
                Vector3 point = CalculateCubicBezierPoint(p0, p1, p2, p3, t);
                path[i] = point;
            }
            return path;
        }
        
        public static Vector3 CalculateCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return 
                Mathf.Pow(1 - t, 3) * p0 + 3 *
                Mathf.Pow(1 - t, 2) * t * p1 + 3 * (1 - t) *
                Mathf.Pow(t, 2) * p2 + Mathf.Pow(t, 3) * p3;
        }
    }
}