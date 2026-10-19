using System;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Health as fractions of the maximum: research and fleet command rescale maxima after a load,
    /// so absolute values would be scaled twice. Ships fill the hardpoint list; squadrons the member lists.
    /// </summary>
    [Serializable]
    public sealed class UnitHealthSave
    {
        [SerializeField] private float hull;
        [SerializeField] private float shields;
        [SerializeField] private float[] hardPoints = Array.Empty<float>();
        [SerializeField] private float[] memberHull = Array.Empty<float>();
        [SerializeField] private float[] memberShields = Array.Empty<float>();

        public float Hull => hull;
        public float Shields => shields;
        public float[] HardPoints => hardPoints;
        public float[] MemberHull => memberHull;
        public float[] MemberShields => memberShields;

        public static UnitHealthSave ForShip(float hull, float shields, float[] hardPoints) =>
            new UnitHealthSave { hull = hull, shields = shields, hardPoints = hardPoints };

        public static UnitHealthSave ForSquadron(float[] memberHull, float[] memberShields) =>
            new UnitHealthSave { memberHull = memberHull, memberShields = memberShields };
    }
}
