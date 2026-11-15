using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    internal sealed class ArtilleryCapability : IArtilleryCapability
    {
        private readonly List<(WeaponHardPoint Mount, float Offset)> _launchers = new();
        private readonly Dictionary<ShipClass, float> _ranges = new();
        private float _visionMultiplier = 1f;

        public int ArtilleryRevision { get; private set; }

        public void RegisterMount(WeaponHardPoint mount, WeaponFamily family, Vector3 hullPosition)
        {
            if (mount.StrikecraftOnly ||
                family is not (WeaponFamily.Missile or WeaponFamily.Torpedo or WeaponFamily.IonTorpedo))
                return;

            // The full offset bounds the planar offset even when the hull banks.
            _launchers.Add((mount, Vector3.Distance(hullPosition, mount.Position)));
            mount.DestructionChanged += InvalidateRanges;
            InvalidateRanges();
        }

        public void UpdateVisionMultiplier(float visionMultiplier)
        {
            if (_visionMultiplier == visionMultiplier) return;
            _visionMultiplier = visionMultiplier;
            InvalidateRanges();
        }

        public void Clear()
        {
            foreach (var launcher in _launchers)
                launcher.Mount.DestructionChanged -= InvalidateRanges;
            _launchers.Clear();
            InvalidateRanges();
        }

        public float GetArtilleryRange(ShipClass targetClass)
        {
            if (_ranges.TryGetValue(targetClass, out float range)) return range;

            foreach (var (mount, offset) in _launchers)
            {
                if (!mount.IsDestroyed && mount.CanEngage(targetClass))
                    range = Mathf.Max(range, mount.MaxAttackDistance - offset);
            }

            _ranges.Add(targetClass, range);
            return range;
        }

        private void InvalidateRanges()
        {
            ArtilleryRevision++;
            _ranges.Clear();
        }
    }
}
