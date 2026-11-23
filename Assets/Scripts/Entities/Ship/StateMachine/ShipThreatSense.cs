using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.TeamContacts;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    /// <summary>
    /// Damage clock and attacker checks for one ship. No unit records who hit it, so an attacker is a hostile that
    /// tracks the ship (order target or weapon list) with the ship inside its weapon range, the same rule the AI
    /// uses for station attackers.
    /// </summary>
    public sealed class ShipThreatSense : IShipThreatSense
    {
        public const float RECENT_DAMAGE_SECONDS = 3f;

        private readonly IEntityLocator _entities;
        private readonly long _entityId;
        private readonly ITeamContacts _teamContacts;
        private readonly IPlayerRelations _relations;
        private readonly PlayerId _owner;
        private float _lastDamageTime = float.NegativeInfinity;

        public bool WasDamagedRecently => Time.time - _lastDamageTime <= RECENT_DAMAGE_SECONDS;

        public ShipThreatSense(IHealthDamageFacade damage, IEntityLocator entities, long entityId,
            ITeamContacts teamContacts, IPlayerRelations relations, PlayerId owner)
        {
            _entities = entities;
            _entityId = entityId;
            _teamContacts = teamContacts;
            _relations = relations;
            _owner = owner;
            // Same scope and lifetime as the health facade, so the handler never outlives it.
            damage.Damaged += RecordDamage;
        }

        public bool IsAttackingSelf(IEntity hostile)
        {
            if (!hostile.TryGetFacade(out IUnitCombatObserverFacade combat)) return false;
            IEntity self = _entities.GetEntity(_entityId);
            if (!combat.IsAttacking(self)) return false;
            Vector3 hostilePosition = hostile.GetFacade<IEntityTransformFacade>().Transform.position;
            Vector3 ownPosition = self.GetFacade<IEntityTransformFacade>().Transform.position;
            return PlanarGeometry.Distance(hostilePosition, ownPosition) <=
                   combat.WeaponRange + FootprintRadius(hostile) + FootprintRadius(self);
        }

        public bool IsAttackingTeamStation(IEntity hostile)
        {
            if (!hostile.TryGetFacade(out IUnitOrderObserverFacade orders)) return false;
            IEntity target = orders.AttackTarget;
            return target != null && !target.HealthModel.IsDestroyed && !_relations.IsHostile(_owner, target.Owner) &&
                   target.TryGetFacade(out IStructureKindFacade structure) &&
                   (structure.Kind == StructureKind.SpaceStation || structure.Kind == StructureKind.SecondStation);
        }

        public void FillTeamVisibleHostiles(Vector3 center, float radius, List<IEntity> results) =>
            _teamContacts.FillVisibleHostiles(_owner, center, radius, results);

        private void RecordDamage() => _lastDamageTime = Time.time;

        private static float FootprintRadius(IEntity entity) =>
            entity.TryGetFacade(out INavigationFootprintFacade footprint) ? footprint.NavigationRadius : 0f;
    }
}
