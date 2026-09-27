using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Sends station-launched AI squadrons to defend threatened sites and capture the closest
    /// zone or site; squadrons hunt only when nothing is left to capture.
    /// </summary>
    public sealed class EnemySquadronCommander : IEnemySquadronCommander, ITickable, ILateDisposable
    {
        private const float DECISION_INTERVAL = 3f;

        private readonly ICaptureSitesSystem _captureSites;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly Dictionary<ISquadron, Action> _squadrons = new Dictionary<ISquadron, Action>();
        private readonly List<ISquadron> _orderBuffer = new List<ISquadron>();
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);

        public EnemySquadronCommander(
            ICaptureSitesSystem captureSites,
            IReinforcementZonesSystem reinforcementZonesSystem)
        {
            _captureSites = captureSites;
            _reinforcementZonesSystem = reinforcementZonesSystem;
        }

        public void Command(ISquadron squadron)
        {
            Action handler = () => Release(squadron);
            _squadrons.Add(squadron, handler);
            squadron.Released += handler;
            IssueOrder(squadron);
        }

        public void Tick()
        {
            if (!_decisionTimer.IsComplete)
            {
                return;
            }

            _decisionTimer.StartTimer();
            _orderBuffer.Clear();
            _orderBuffer.AddRange(_squadrons.Keys);
            foreach (ISquadron squadron in _orderBuffer)
            {
                IssueOrder(squadron);
            }
        }

        public void LateDispose()
        {
            foreach (KeyValuePair<ISquadron, Action> pair in _squadrons)
            {
                pair.Key.Released -= pair.Value;
            }

            _squadrons.Clear();
        }

        private void IssueOrder(ISquadron squadron)
        {
            // Attack-Move re-issued to the same point is ignored by the squadron, so repeats are cheap.
            if (_captureSites.TryGetThreatenedSite(PlayerType.Opponent, out Vector3 target) ||
                TryGetClosestCaptureTarget(squadron.WorldPosition, out target))
            {
                squadron.AttackMoveTo(target);
            }
            else
            {
                squadron.Hunt();
            }
        }

        private bool TryGetClosestCaptureTarget(Vector3 origin, out Vector3 captureTarget)
        {
            bool hasZone = _reinforcementZonesSystem.TryGetCaptureTarget(
                PlayerType.Opponent, origin, out Vector3 zoneTarget);
            bool hasSite = _captureSites.TryGetCaptureTarget(
                PlayerType.Opponent, origin, out Vector3 siteTarget);
            captureTarget = hasSite && (!hasZone ||
                (siteTarget - origin).sqrMagnitude < (zoneTarget - origin).sqrMagnitude)
                ? siteTarget
                : zoneTarget;
            return hasZone || hasSite;
        }

        private void Release(ISquadron squadron)
        {
            squadron.Released -= _squadrons[squadron];
            _squadrons.Remove(squadron);
        }
    }
}
