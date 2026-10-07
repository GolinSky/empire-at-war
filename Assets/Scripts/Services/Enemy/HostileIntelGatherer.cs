using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using Zenject;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Reports every hostile ship and squadron the AI's team can see into the team's shared intel, and drops
    /// records that have faded out. Units outside vision keep their last report until it is forgotten.
    /// </summary>
    public sealed class HostileIntelGatherer : IInitializable, ITickable, ILateDisposable
    {
        private const float SCAN_INTERVAL = 1f;

        private readonly IEntityLocator _entityLocator;
        private readonly IVisionService _vision;
        private readonly IPlayerRoster _playerRoster;
        private readonly HostileIntelModel _intel;
        private readonly PlayerSlot _owner;

        private float _nextScanTime;

        public HostileIntelGatherer(
            IEntityLocator entityLocator,
            IVisionService vision,
            IPlayerRoster playerRoster,
            TeamIntelRegistry intelRegistry,
            PlayerSlot owner)
        {
            _entityLocator = entityLocator;
            _vision = vision;
            _playerRoster = playerRoster;
            _owner = owner;
            _intel = intelRegistry.Get(owner.Team);
        }

        public void Initialize()
        {
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
        }

        public void Tick()
        {
            float now = Time.time;
            if (now < _nextScanTime)
            {
                return;
            }

            _nextScanTime = now + SCAN_INTERVAL;
            _intel.ForgetExpired(now);
            foreach (IEntity entity in _entityLocator.Entities)
            {
                IHealthModelObserver health = entity.HealthModel;
                if (!_playerRoster.IsHostile(_owner.Id, entity.Owner) ||
                    health.IsDestroyed ||
                    !health.HasUnits ||
                    !entity.TryGetFacade(out IUnitTypeFacade unit))
                {
                    continue;
                }

                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (!_vision.IsVisible(_owner.Id, position))
                {
                    continue;
                }

                _intel.Report(new HostileSighting(entity.Id, unit.UnitTypeId, entity.Owner, ToPoint(position),
                    health.Hull, health.Shields, ForceCompositionBuilder.GetOffenseScale(health), now));
            }
        }

        /// <summary>A destroyed unit is struck from the record; its wreck is visible proof.</summary>
        private void HandleEntityRemoved(IEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _intel.Forget(entity.Id);
        }
    }
}
