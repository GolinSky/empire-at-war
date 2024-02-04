using System;
using UnityEngine;

namespace EmpireAtWar.Models.Weapon
{
    [Serializable]
    public class ProjectileData
    {
        [field:SerializeField] public Color Color { get; private set; }
        [field:SerializeField] public Vector3 Size { get; private set; }
    }
}