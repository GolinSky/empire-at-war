using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Sends station-launched AI squadrons to defend threatened sites and capture the closest
    /// zone or site; squadrons hunt only when nothing is left to capture. While hostile strikecraft
    /// are in play, squadrons that destroy them faster than they destroy hostile ships escort the
    /// own ships those strikecraft hurt the most.
    /// </summary>
    public sealed class EnemySquadronCommander : IEnemySquadronCommander, ITickable, ILateDisposable
    {
        private const float DECISION_INTERVAL = 3f;

        private readonly ICaptureSitesSystem _captureSites;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IEntityLocator _entityLocator;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly UnitCombatProfileCatalog _profileCatalog;
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);

        private readonly PlayerSlot _owner;
        private readonly Dictionary<ISquadron, CommandedSquadron> _squadrons =
            new Dictionary<ISquadron, CommandedSquadron>();
        private readonly List<ISquadron> _orderBuffer = new List<ISquadron>();
        private readonly ForceComposition _hostileStrikecraft = new ForceComposition();
        private readonly ForceComposition _hostileShips = new ForceComposition();
        private readonly ForceComposition _squadronForce = new ForceComposition();
        private readonly List<IEntity> _escortTargets = new List<IEntity>();

        private int _nextEscortTarget;

        public EnemySquadronCommander(
            ICaptureSitesSystem captureSites,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEntityLocator entityLocator,
            IPlayerRoster playerRoster,
            ForceCompositionBuilder forceBuilder,
            UnitCombatProfileCatalog profileCatalog,
            PlayerSlot owner)
        {
            _owner = owner;
            _captureSites = captureSites;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _entityLocator = entityLocator;
            _playerRoster = playerRoster;
            _forceBuilder = forceBuilder;
            _profileCatalog = profileCatalog;
        }

        public void LateDispose()
        {
            foreach (KeyValuePair<ISquadron, CommandedSquadron> pair in _squadrons)
            {
                pair.Key.Released -= pair.Value.ReleaseHandler;
            }

            _squadrons.Clear();
        }

        public void Command(ISquadron squadron, SquadronType squadronType)
        {
            Action handler = () => Release(squadron);
            _squadrons.Add(squadron, new CommandedSquadron(
                _profileCatalog.Get(UnitTypeId.Squadron(squadronType)), handler));
            squadron.Released += handler;
            AssessThreats();
            IssueOrder(squadron);
        }

        public void Tick()
        {
            if (!_decisionTimer.IsComplete)
            {
                return;
            }

            _decisionTimer.StartTimer();
            AssessThreats();
            _orderBuffer.Clear();
            _orderBuffer.AddRange(_squadrons.Keys);
            foreach (ISquadron squadron in _orderBuffer)
            {
                IssueOrder(squadron);
            }
        }

        private void IssueOrder(ISquadron squadron)
        {
            if (ShouldEscort(_squadrons[squadron].Profile))
            {
                // Guard re-issued to the same ship is ignored by the squadron; escorts spread across threatened ships.
                squadron.Guard(_escortTargets[_nextEscortTarget % _escortTargets.Count], Vector3.zero);
                _nextEscortTarget++;
                return;
            }

            // Attack-Move re-issued to the same point is ignored by the squadron, so repeats are cheap.
            if (_captureSites.TryGetThreatenedSite(_owner.Id, out Vector3 target) ||
                TryGetClosestCaptureTarget(squadron.WorldPosition, out target))
            {
                squadron.AttackMoveTo(target);
            }
            else
            {
                squadron.Hunt();
            }
        }

        /// <summary>
        /// Rebuilds the hostile strikecraft and ship forces and orders own ships by how much damage
        /// the hostile strikecraft deal to their class.
        /// </summary>
        private void AssessThreats()
        {
            PlayerId self = _owner.Id;
            _forceBuilder.Build(_hostileStrikecraft,
                entity => _playerRoster.IsHostile(self, entity.Owner) && entity.IsSquadron());
            _forceBuilder.Build(_hostileShips,
                entity => _playerRoster.IsHostile(self, entity.Owner) && entity.IsShip());
            _escortTargets.Clear();
            _nextEscortTarget = 0;
            if (_hostileStrikecraft.IsEmpty)
            {
                return;
            }

            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Owner == self && entity.IsShip() && !entity.HealthModel.IsDestroyed)
                {
                    _escortTargets.Add(entity);
                }
            }

            _escortTargets.Sort((first, second) =>
            {
                int byThreat = StrikecraftThreat(second).CompareTo(StrikecraftThreat(first));
                return byThreat != 0
                    ? byThreat
                    : (second.HealthModel.Hull + second.HealthModel.Shields)
                    .CompareTo(first.HealthModel.Hull + first.HealthModel.Shields);
            });
        }

        private float StrikecraftThreat(IEntity ship)
        {
            ShipClass shipClass = ship.HealthModel.ShipClass;
            return _hostileStrikecraft.HullDps(shipClass) + _hostileStrikecraft.PiercingDps(shipClass);
        }

        private bool ShouldEscort(UnitCombatProfile profile)
        {
            if (_hostileStrikecraft.IsEmpty || _escortTargets.Count == 0)
            {
                return false;
            }

            if (_hostileShips.IsEmpty)
            {
                return true;
            }

            _squadronForce.Clear();
            _squadronForce.AddNew(profile, false);
            return CombatMatchup.TimeToDestroy(_squadronForce, _hostileStrikecraft) <=
                   CombatMatchup.TimeToDestroy(_squadronForce, _hostileShips);
        }

        private bool TryGetClosestCaptureTarget(Vector3 origin, out Vector3 captureTarget)
        {
            bool hasZone = _reinforcementZonesSystem.TryGetCaptureTarget(
                _owner.Id, origin, out Vector3 zoneTarget);
            bool hasSite = _captureSites.TryGetCaptureTarget(
                _owner.Id, origin, out Vector3 siteTarget);
            captureTarget = hasSite && (!hasZone ||
                (siteTarget - origin).sqrMagnitude < (zoneTarget - origin).sqrMagnitude)
                ? siteTarget
                : zoneTarget;
            return hasZone || hasSite;
        }

        private void Release(ISquadron squadron)
        {
            squadron.Released -= _squadrons[squadron].ReleaseHandler;
            _squadrons.Remove(squadron);
        }
    }
}
