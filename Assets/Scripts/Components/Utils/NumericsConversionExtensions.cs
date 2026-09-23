using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace EmpireAtWar.Utils
{
    public static class NumericsConversionExtensions
    {
        public static NumericsVector3 ToNumerics(this Vector3 value)
        {
            return new NumericsVector3(value.x, value.y, value.z);
        }

        public static NumericsQuaternion ToNumerics(this Quaternion value)
        {
            return new NumericsQuaternion(value.x, value.y, value.z, value.w);
        }

        public static Vector3 ToUnity(this NumericsVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static Quaternion ToUnity(this NumericsQuaternion value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W);
        }
    }
}
