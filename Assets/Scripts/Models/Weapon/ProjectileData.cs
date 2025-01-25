using System;
using UnityEngine;

namespace EmpireAtWar.Models.Weapon
{
    [Serializable]
    public class ProjectileData
    {
        [field:SerializeField] public Color Color { get; private set; }
        [field:SerializeField] public Vector3 Size { get; private set; }
        [field:SerializeField] public TurretType TurretType { get; private set; }
        [field:SerializeField] public float Delay { get; private set; }
    }

    public enum TurretType
    {
        Single = 0,
        Dual = 1,
        Laser = 2,
        Torpedo = 3,
        Rocket = 4,
    }
}