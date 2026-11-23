using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>How a flying bolt's particles look: size per axis and faction color.</summary>
    internal struct BoltLook
    {
        public float3 Size;
        public Color32 Color;
    }
}
