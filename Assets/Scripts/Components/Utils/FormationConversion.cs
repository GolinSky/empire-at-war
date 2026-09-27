using EmpireAtWar.Components.Movement.Formation;
using UnityEngine;

namespace EmpireAtWar.Utils
{
    public static class FormationConversion
    {
        public static FormationPoint ToPoint(Vector3 value) => new FormationPoint(value.x, value.z);
        public static Vector3 ToVector(FormationPoint value) => new Vector3(value.X, 0f, value.Z);
    }
}
