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
        private readonly List<(WeaponHardPoint Mount, float Offset)> _mainBatteries = new();
        private readonly Dictionary<ShipClass, float> _ranges = new();
        private float _visionMultiplier = 1f;

        public int ArtilleryRevision { get; private set; }

        public void RegisterMount(WeaponHardPoint mount, WeaponFamily family, Vector3 hullPosition)
        {
            if (mount.StrikecraftOnly) return;
            bool launcher = family is WeaponFamily.Missile or WeaponFamily.Torpedo or WeaponFamily.IonTorpedo;
            if (!launcher && !mount.IsMainBattery) return;

            // The full offset bounds the planar offset even when the hull banks.
            float offset = Vector3.Distance(hullPosition, mount.Position);
            if (launcher) _launchers.Add((mount, offset));
            if (mount.IsMainBattery) _mainBatteries.Add((mount, offset));
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
            foreach (var main in _mainBatteries)
                if (!_launchers.Exists(launcher => launcher.Mount == main.Mount))
                    main.Mount.DestructionChanged -= InvalidateRanges;
            _launchers.Clear();
            _mainBatteries.Clear();
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

            // Hold where surviving main batteries still reach; otherwise the facing solver falls back
            // to launchers and can swing the stern toward the target.
            float mainReach = 0f;
            foreach (var (mount, offset) in _mainBatteries)
            {
                if (!mount.IsDestroyed && mount.CanEngage(targetClass))
                    mainReach = Mathf.Max(mainReach, mount.MaxAttackDistance - offset);
            }
            if (mainReach > 0f) range = Mathf.Min(range, mainReach);

            _ranges.Add(targetClass, range);
            return range;
        }

        public bool HasMainBatteryCover(ShipClass targetClass)
        {
            foreach (var (mount, _) in _mainBatteries)
                if (!mount.IsDestroyed && mount.CanEngage(targetClass)) return true;
            return false;
        }

        private void InvalidateRanges()
        {
            ArtilleryRevision++;
            _ranges.Clear();
        }
    }
}
