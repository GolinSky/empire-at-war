using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>
    /// Automatic use (player auto-power and AI alike): the well is raised only where it can deny something — a hostile
    /// reinforcement anchor (hostile station or hostile-held relay) or a visible retreating hostile ship inside its
    /// range — and ended once neither is left, so the anchored caster takes movement orders again (W39).
    /// </summary>
    [Serializable]
    public sealed class GravityWellSettings : ShipAbilitySettings, IShipAbilityAutoUseRule, IShipAbilityAutoStopRule
    {
        [SerializeField] private GravityWellView viewPrefab;

        public GravityWellView ViewPrefab => viewPrefab;

        public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            HasInterdictionTarget(dependencies, caster);

        public bool ShouldAutoStop(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            !HasInterdictionTarget(dependencies, caster);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new GravityWellAbility(dependencies.BattleAreas, dependencies.GravityWells, this);

        private bool HasInterdictionTarget(ShipAbilityDependencies dependencies, IShipAbilityFacade caster)
        {
            float range = GetRange(caster);
            if (range <= 0f) return false;
            Vector3 origin = caster.WorldPosition;
            var owner = caster.Entity.Owner;
            if (dependencies.Zones != null)
                foreach (ReinforcementZonePresenter zone in dependencies.Zones.Zones)
                    if (!zone.Owner.IsNone && dependencies.Relations.IsHostile(owner, zone.Owner) &&
                        PlanarDistance(origin, zone.Center) <= range + zone.Radius)
                        return true;
            foreach (IEntity entity in dependencies.Entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !dependencies.Relations.IsHostile(owner, entity.Owner)) continue;
                bool station = entity.IsPlayerBase();
                bool retreating = !station && entity.TryGetFacade(out IUnitOrderObserverFacade orders) &&
                                  orders.CurrentOrder == UnitOrderType.Retreat;
                if (!station && !retreating) continue;
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (PlanarDistance(origin, position) > range) continue;
                if (retreating && dependencies.Vision != null && !dependencies.Vision.IsVisible(owner, position)) continue;
                return true;
            }
            return false;
        }

        private float GetRange(IShipAbilityFacade caster)
        {
            foreach (ShipAbilitySlot slot in caster.Slots)
                if (ReferenceEquals(slot.Definition.Settings, this)) return slot.Definition.Range;
            return 0f;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
