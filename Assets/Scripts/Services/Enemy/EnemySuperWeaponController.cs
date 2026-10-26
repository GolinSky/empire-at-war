using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Utils;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Buys superweapon charges from spare money once the station level allows it, and fires a ready weapon
    /// after a difficulty-based delay at one of the most valuable visible player targets.
    /// </summary>
    public sealed class EnemySuperWeaponController : ITickable
    {
        private const float DECISION_INTERVAL = 5f;
        // Keep enough money aside that the superweapons never starve fleet production.
        private const float MONEY_RESERVE_MULTIPLIER = 2f;

        private readonly ISuperWeaponFireService _superWeaponFireService;
        private readonly IEntityLocator _entities;
        private readonly IVisionService _vision;
        private readonly IProductionAccess _productionAccess;
        private readonly IEnemyPurchaseProcessor _production;
        private readonly SuperWeaponData _data;
        private readonly IPlayerRelations _relations;

        private readonly SuperWeaponModel _model;
        private readonly PlayerSlot _owner;
        private readonly EnemyFactionModel _factionModel;
        private readonly EconomyModel _economyModel;
        private readonly Dictionary<SuperWeaponType, float> _chargeTimeLeft =
            new Dictionary<SuperWeaponType, float>();
        private readonly List<SuperWeaponType> _charging = new List<SuperWeaponType>();
        private readonly List<(IEntity Entity, Vector3 Position, float Score)> _targets =
            new List<(IEntity, Vector3, float)>();

        private float _decisionTimer;
        private bool _hasFireDelay;
        private float _fireDelayLeft;

        public EnemySuperWeaponController(ISuperWeaponFireService superWeaponFireService, IEntityLocator entities,
            IVisionService vision,
            IProductionAccess productionAccess, SuperWeaponModel model, EnemyFactionModel factionModel,
            EconomyModel economyModel,
            PlayerSlot owner, IEnemyPurchaseProcessor production, SuperWeaponData data,
            IPlayerRelations relations)
        {
            _owner = owner;
            _model = model;
            _factionModel = factionModel;
            _economyModel = economyModel;
            _superWeaponFireService = superWeaponFireService;
            _entities = entities;
            _vision = vision;
            _productionAccess = productionAccess;
            _production = production;
            _data = data;
            _relations = relations;
        }

        public void Tick()
        {
            AdvanceCharging(Time.deltaTime);
            _model.Tick(Time.deltaTime);
            if (_hasFireDelay) _fireDelayLeft -= Time.deltaTime;

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0f) return;
            _decisionTimer = DECISION_INTERVAL;

            foreach (KeyValuePair<SuperWeaponType, FactionData> option in _factionModel.SuperWeapons)
            {
                SuperWeaponState state = _model.GetState(option.Key);
                if (_model.CanFire(option.Key))
                {
                    // The shared cooldown after a firing blocks the remaining weapons of this decision.
                    TryFireAfterDelay(option.Key);
                }
                else if (state == SuperWeaponState.Unavailable && TryPurchase(option.Key, option.Value))
                {
                    // One purchase per decision so production gets the next look at the money.
                    return;
                }
            }
        }

        public float GetChargeTimeRemaining(SuperWeaponType type) => _chargeTimeLeft[type];

        public void RestoreCharging(SuperWeaponType type, float? timeLeft)
        {
            // Older saves kept the charging state but not its timer; restart the paid charge.
            _chargeTimeLeft[type] = timeLeft ?? _factionModel.SuperWeapons[type].BuildTime;
            _production.ReserveExternalPipeline(type.ToString());
        }

        public void AdvanceCharging(float deltaTime)
        {
            _charging.Clear();
            _charging.AddRange(_chargeTimeLeft.Keys);
            foreach (SuperWeaponType type in _charging)
            {
                float timeLeft = _chargeTimeLeft[type] - deltaTime;
                if (timeLeft > 0f)
                {
                    _chargeTimeLeft[type] = timeLeft;
                    continue;
                }

                _chargeTimeLeft.Remove(type);
                _production.ReleaseExternalPipeline(type.ToString());
                if (_productionAccess.CanProduce)
                {
                    _model.CompleteCharging(type);
                    continue;
                }

                // Same rule as player production: a charge that completes without a station is refunded.
                _model.CancelCharging(type);
                _economyModel.AddMoney(_factionModel.SuperWeapons[type].Price);
            }
        }

        private bool TryPurchase(SuperWeaponType type, FactionData data)
        {
            if (!_productionAccess.CanProduce ||
                !_production.CanQueue(type.ToString(), data.MaxCount) ||
                data.AvailableLevel > _factionModel.CurrentLevel ||
                _economyModel.Money < data.Price * MONEY_RESERVE_MULTIPLIER ||
                !_economyModel.TrySpend(data.Price))
                return false;

            _model.StartCharging(type);
            _chargeTimeLeft[type] = data.BuildTime;
            _production.ReserveExternalPipeline(type.ToString());
            Debug.Log($"[EnemyAI:SuperWeapon] Purchased {type}, Cost={data.Price}, Money={_economyModel.Money}");
            return true;
        }

        /// <summary>Waits like a player who has to notice the ready weapon and pick a target before firing.</summary>
        private void TryFireAfterDelay(SuperWeaponType type)
        {
            if (!_hasFireDelay)
            {
                EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(_owner.Difficulty);
                _fireDelayLeft = Random.Range(profile.SuperWeaponMinFireDelay, profile.SuperWeaponMaxFireDelay);
                _hasFireDelay = true;
                return;
            }

            if (_fireDelayLeft > 0f || !TryFire(type)) return;
            _hasFireDelay = false;
        }

        private bool TryFire(SuperWeaponType type)
        {
            IEntity target = SelectTarget(type);
            if (target == null) return false;

            _model.Consume(type);
            _superWeaponFireService.Fire(type, target);
            Debug.Log($"[EnemyAI:SuperWeapon] Fired {type} at {target.HealthModel.ShipClass} ({target.Id})");
            return true;
        }

        /// <summary>
        /// The ion cannon strips the strongest shields; the other weapons hit the toughest hull, and area weapons
        /// also count the visible targets inside their blast. The pick is random among the best few by difficulty.
        /// </summary>
        private IEntity SelectTarget(SuperWeaponType type)
        {
            _targets.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_superWeaponFireService.CanTarget(_owner.Id, entity)) continue;
                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (!_vision.IsVisible(_owner.Id, position)) continue;
                float score = type == SuperWeaponType.IonCannon
                    ? entity.HealthModel.Shields
                    : entity.HealthModel.Hull;
                _targets.Add((entity, position, score));
            }

            if (_targets.Count == 0) return null;
            AddAreaScores(_data.GetProfile(type));
            _targets.Sort((a, b) => b.Score.CompareTo(a.Score));
            int choices = Mathf.Min(EnemyAiDifficultyProfile.Get(_owner.Difficulty).SuperWeaponTargetChoices,
                _targets.Count);
            return _targets[Random.Range(0, choices)].Entity;
        }

        private void AddAreaScores(SuperWeaponProfile profile)
        {
            if (profile.AreaDamage <= 0f) return;
            for (int i = 0; i < _targets.Count; i++)
            {
                (IEntity entity, Vector3 position, float score) = _targets[i];
                foreach ((IEntity other, Vector3 otherPosition, _) in _targets)
                {
                    if (other == entity || !_relations.IsAllied(other.Owner, entity.Owner) ||
                        PlanarGeometry.Distance(position, otherPosition) > profile.AreaRadius) continue;
                    score += Mathf.Min(profile.AreaDamage, other.HealthModel.Hull);
                }

                _targets[i] = (entity, position, score);
            }
        }
    }
}
