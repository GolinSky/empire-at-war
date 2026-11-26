using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Runs the hit-and-run cycle of one task group: picks the strike target and the strike ships, turns the
    /// battle around them into numbers for <see cref="EnemyStrikeCycleModel"/> and keeps the safe point. Strike
    /// membership and the safe point are frozen from the pull-back until the next strike.
    /// </summary>
    public sealed class EnemyStrikeCycleAssessor
    {
        private readonly IEnemyStrikeSituation _situation;
        private readonly IFocusFireSlots _focusFire;
        private readonly EnemyHitAndRunTuning _tuning;
        private readonly EnemyStrikeCycleModel _model;
        private readonly EnemyStrikeGroupRules _rules = new EnemyStrikeGroupRules();
        private readonly List<IShipEntity> _strikers = new List<IShipEntity>();
        private readonly Dictionary<IShipEntity, GameEntity> _strikerEntities = new Dictionary<IShipEntity, GameEntity>();
        private readonly List<IShipEntity> _stateShips = new List<IShipEntity>();
        private readonly List<ForceComposition> _candidates = new List<ForceComposition>();
        private readonly List<UnitRole> _roles = new List<UnitRole>();
        private readonly List<int> _retained = new List<int>();
        private readonly List<int> _selected = new List<int>();
        private readonly ForceComposition _groupForce = new ForceComposition();
        private readonly ForceComposition _strikerForce = new ForceComposition();
        private readonly ForceComposition _restForce = new ForceComposition();
        private EnemyStrikeFallbackKind _safePointKind;
        private float _safePointDistance;
        private int _decisionsInPhase;
        private int _lostStrikers;
        private Vector3 _lastSafePoint;

        public EnemyStrikePhase Phase => _model.Phase;
        public bool IsActive => _model.IsActive;
        public string Reason => _model.Reason;
        /// <summary>The hostile big ship the cycle strikes; null while the cycle is idle.</summary>
        public GameEntity Target { get; private set; }
        public IReadOnlyList<IShipEntity> Strikers => _strikers;
        public Vector3? SafePoint { get; private set; }
        /// <summary>The last <see cref="Update"/> or <see cref="Stop"/> changed the phase.</summary>
        public bool PhaseChanged { get; private set; }

        public EnemyStrikeCycleAssessor(IEnemyStrikeSituation situation, IFocusFireSlots focusFire,
            EnemyHitAndRunTuning tuning)
        {
            _situation = situation;
            _focusFire = focusFire;
            _tuning = tuning;
            _model = new EnemyStrikeCycleModel(tuning);
        }

        /// <summary>Rates the cycle for this decision. <paramref name="context"/> still lists every group ship.</summary>
        /// <param name="isInFight">The group fights and takes no order that overrides the cycle.</param>
        public void Update(EnemyStrategicContext context, bool isInFight, float now)
        {
            EnemyStrikePhase before = _model.Phase;
            bool frozen = before is EnemyStrikePhase.PullBack or EnemyStrikePhase.Recover;
            PruneStrikers(context);
            GameEntity target = isInFight ? _situation.FindStrikeTarget(IsActive ? Target : null) : null;
            EnemyStrikeCycleInput input = default;
            input.Now = now;
            input.IsInFight = isInFight;
            input.HasTarget = target != null;
            Target = target;
            if (target != null)
            {
                ForceComposition defenders = context.TargetForces[target];
                _groupForce.Clear();
                foreach (IShipEntity ship in context.Ships) _groupForce.Add(context.ShipForces[ship]);
                input.GroupAdvantage = CombatMatchup.Advantage(_groupForce, defenders);
                if (!frozen) SelectStrikers(context, target, defenders);
                // Too few strike ships, or an idle cycle a straight fight rules out: nothing further to rate.
                bool fewStrikers = _strikers.Count < EnemyStrikeGroupRules.MINIMUM_STRIKERS;
                if (fewStrikers || !IsActive && input.GroupAdvantage >= _tuning.StraightFightAdvantage)
                {
                    Stop(fewStrikers ? "fewStrikers" : "groupWins", now);
                    return;
                }
                Rate(context, target, defenders, frozen, ref input);
            }
            _model.Evaluate(input);
            Finish(before);
        }

        /// <summary>Ends the cycle: the group left its fight or follows an order that overrides it.</summary>
        public void Stop(string reason, float now)
        {
            EnemyStrikePhase before = _model.Phase;
            _model.Stop(reason, now);
            Finish(before);
        }

        /// <summary>
        /// Splits the ships of <paramref name="context"/> for the executor while the cycle runs: the strike ships
        /// follow the cycle, the rest keep the state orders.
        /// </summary>
        public void Apply(EnemyStrategicContext context)
        {
            if (!IsActive) return;
            _stateShips.Clear();
            foreach (IShipEntity ship in context.Ships)
                if (!_strikers.Contains(ship)) _stateShips.Add(ship);
            context.SetStrikeCycle(Phase, Target, _strikers, SafePoint, _stateShips);
        }

        /// <summary>A strike ship was destroyed during the cycle: one reinforcement is asked for at the safe point.</summary>
        public bool TryTakeLostStriker(out Vector3 point)
        {
            point = _lastSafePoint;
            if (_lostStrikers == 0) return false;
            _lostStrikers--;
            return true;
        }

        private void Finish(EnemyStrikePhase before)
        {
            PhaseChanged = before != _model.Phase;
            _decisionsInPhase = PhaseChanged ? 1 : _decisionsInPhase + 1;
            if (IsActive) return;
            _strikers.Clear();
            _strikerEntities.Clear();
            _lostStrikers = 0;
            Target = null;
            SafePoint = null;
        }

        // Ships that left the group are no strikers any more; a destroyed one is replaced while the cycle runs.
        private void PruneStrikers(EnemyStrategicContext context)
        {
            for (int i = _strikers.Count - 1; i >= 0; i--)
            {
                IShipEntity ship = _strikers[i];
                if (context.Receivers.ContainsKey(ship)) continue;
                if (IsActive && _strikerEntities[ship].HealthModel.IsDestroyed) _lostStrikers++;
                _strikerEntities.Remove(ship);
                _strikers.RemoveAt(i);
            }
        }

        private void SelectStrikers(EnemyStrategicContext context, GameEntity target, ForceComposition defenders)
        {
            IReadOnlyList<IShipEntity> ships = context.Ships;
            _candidates.Clear();
            _roles.Clear();
            _retained.Clear();
            for (int i = 0; i < ships.Count; i++)
            {
                _candidates.Add(context.ShipForces[ships[i]]);
                _roles.Add(_situation.GetProfile(context.Receivers[ships[i]]).Roles);
                if (_strikers.Contains(ships[i])) _retained.Add(i);
            }
            IHealthModelObserver health = target.HealthModel;
            _rules.Select(_candidates, _roles, defenders, health.ShipClass, health.Hull, health.Shields,
                EnemyAiDifficultyProfile.Get(context.Snapshot.Difficulty).RequiredAttackRatio, _retained,
                EnemyTaskForceExecutor.GetStrikeLimit(_focusFire, context, target, ships, ships.Count), _selected);
            _strikers.Clear();
            _strikerEntities.Clear();
            foreach (int index in _selected)
            {
                _strikers.Add(ships[index]);
                _strikerEntities.Add(ships[index], context.Receivers[ships[index]]);
            }
        }

        private void Rate(EnemyStrategicContext context, GameEntity target, ForceComposition defenders, bool frozen,
            ref EnemyStrikeCycleInput input)
        {
            IHealthModelObserver targetHealth = target.HealthModel;
            UnitCombatProfile targetProfile = _situation.GetProfile(target);
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            _strikerForce.Clear();
            _restForce.Clear();
            Vector3 centre = Vector3.zero;
            float shipRadius = 0f;
            float slowest = float.PositiveInfinity;
            float hostileDps = 0f;
            float shieldFractions = 0f;
            float hullFractions = 0f;
            bool anyRetreating = false;
            foreach (IShipEntity ship in context.Ships)
            {
                if (!_strikers.Contains(ship))
                {
                    _restForce.Add(context.ShipForces[ship]);
                    continue;
                }
                _strikerForce.Add(context.ShipForces[ship]);
                GameEntity entity = context.Receivers[ship];
                IHealthModelObserver health = entity.HealthModel;
                UnitCombatProfile profile = _situation.GetProfile(entity);
                float regen = ForceCompositionBuilder.GetShieldRegen(profile, health);
                input.StrikerMaxShields += profile.Shields;
                input.StrikerShieldRegen += regen;
                if (regen > 0f && profile.Shields > 0f)
                {
                    input.RegeneratingStrikerCount++;
                    shieldFractions += health.ShieldPercentage;
                    hullFractions += health.HullPercentage;
                }
                hostileDps += defenders.ShieldDps(health.ShipClass);
                centre += ship.WorldPosition;
                shipRadius = Mathf.Max(shipRadius, ship.NavigationRadius);
                slowest = Mathf.Min(slowest, ship.NavigationSpeed);
                anyRetreating |= ship.CurrentOrder == UnitOrderType.Retreat;
            }
            int strikers = _strikers.Count;
            centre /= strikers;
            input.StrikerCount = strikers;
            input.NonStrikerCount = context.Ships.Count - strikers;
            // The hostiles focus one class at a time: their shield damage averaged over the strikers they shoot.
            input.HostileShieldDps = hostileDps / strikers;
            input.StrikerShieldDps = _strikerForce.ShieldDps(targetHealth.ShipClass);
            input.StrikerHullDps = _strikerForce.HullDps(targetHealth.ShipClass);
            input.StrikerPiercingDps = _strikerForce.PiercingDps(targetHealth.ShipClass);
            if (input.RegeneratingStrikerCount > 0)
            {
                input.AverageShieldFraction = shieldFractions / input.RegeneratingStrikerCount;
                input.AverageHullFraction = hullFractions / input.RegeneratingStrikerCount;
            }
            input.TargetShields = targetHealth.Shields;
            input.TargetMaxShields = targetProfile.Shields;
            input.TargetShieldRegen = ForceCompositionBuilder.GetShieldRegen(targetProfile, targetHealth);
            input.TargetMaxHull = targetProfile.Hull;
            input.TargetSpeed = targetProfile.Speed;
            input.TargetWeaponRange = target.GetFacade<IUnitCombatObserverFacade>().WeaponRange;
            input.HostileRepairPerSecond = _situation.GetHostileRepairPerSecond(target);
            input.TargetSelfRepairs = _situation.HasFieldRepairs(target.Owner);
            input.TargetUnderUnbeatableDefenses = _situation.IsUnderUnbeatableDefenses(target, _groupForce);
            input.LeftBehindSurvivalSeconds = CombatMatchup.TimeToDestroy(defenders, _restForce);
            if (!frozen)
            {
                bool found = _situation.TryFindStrikeFallbackPoint(centre, target, shipRadius, out Vector3 point,
                    out _safePointKind);
                SafePoint = found ? point : (Vector3?)null;
                if (found)
                {
                    _lastSafePoint = point;
                    _safePointDistance = PlanarDistance(centre, point);
                }
            }
            input.HasSafePoint = SafePoint.HasValue;
            if (SafePoint is Vector3 safePoint)
            {
                input.SafePointIsStationCover = _safePointKind == EnemyStrikeFallbackKind.Station;
                input.SafePointDistance = _safePointDistance;
                input.SlowestStrikerSpeed = slowest;
                input.TravelSeconds = slowest > 0f ? PlanarDistance(targetPosition, safePoint) / slowest : float.PositiveInfinity;
                input.IsHullRepairAvailable = _situation.HasFieldRepairs(_strikers[0].Owner) ||
                    _situation.IsRepairCovered(safePoint);
                input.IsChased = frozen && _situation.CanTargetReach(target, safePoint);
            }
            input.HasStationPressure = context.Snapshot.HasStationPressure;
            // The retreat orders go out with the decision that starts the pull-back: arrival counts from the next one.
            input.HaveStrikersArrived = _decisionsInPhase >= 1 && !anyRetreating;
        }

        private static float PlanarDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = from - to;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
