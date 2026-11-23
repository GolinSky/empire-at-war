using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Laser / turbolaser bolt prefab: authoring data only, never pooled or fired as an object.
    /// <see cref="BoltFlightBatch"/> flies every bolt and draws all bolts of one prefab through one shared copy of
    /// <see cref="Vfx"/>: its material, stretched-billboard renderer and barrel emission mesh stay as authored.
    /// </summary>
    public class BoltShot : ShotEffect
    {
        [SerializeField] private ParticleSystem vfx;

        [Tooltip("Multiplies the weapon profile size for the bolt particle only (not the muzzle flash).")]
        [SerializeField] private float sizeScale = 3f;

        internal ParticleSystem Vfx => vfx;
        internal float SizeScale => sizeScale;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile) =>
            throw new InvalidOperationException($"{name}: bolts fly through BoltFlightBatch, never as pooled shots.");

        protected override void ResetVisuals() { }
    }
}
