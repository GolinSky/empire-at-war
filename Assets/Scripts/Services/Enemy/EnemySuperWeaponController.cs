using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SuperWeapons;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Buys superweapon charges from spare money once the station level allows it,
    /// and fires each ready weapon at the most valuable player target.
    /// </summary>
    public sealed class EnemySuperWeaponController : ITickable
    {
        private const float DECISION_INTERVAL = 5f;
        // Keep enough money aside that the superweapons never starve fleet production.
        private const float MONEY_RESERVE_MULTIPLIER = 2f;

        private readonly SuperWeaponModel _model;
        private readonly EnemyFactionModel _factionModel;
        private readonly EconomyModel _economyModel;
        private readonly FactionsData _factionsData;
        private readonly ISuperWeaponFireService _fireService;
        private readonly IEntityLocator _entities;
        private readonly Dictionary<SuperWeaponType, float> _chargeTimeLeft =
            new Dictionary<SuperWeaponType, float>();
        private readonly List<SuperWeaponType> _charging = new List<SuperWeaponType>();
        private float _decisionTimer;

        public EnemySuperWeaponController(SuperWeaponModel model, EnemyFactionModel factionModel,
            EconomyModel economyModel, FactionsData factionsData, ISuperWeaponFireService fireService,
            IEntityLocator entities)
        {
            _model = model;
            _factionModel = factionModel;
            _economyModel = economyModel;
            _factionsData = factionsData;
            _fireService = fireService;
            _entities = entities;
        }

        public void Tick()
        {
            AdvanceCharging(Time.deltaTime);

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0f) return;
            _decisionTimer = DECISION_INTERVAL;

            foreach (KeyValuePair<SuperWeaponType, FactionData> option in _factionsData.SuperWeaponFactionData)
            {
                SuperWeaponState state = _model.GetState(option.Key);
                if (state == SuperWeaponState.Ready)
                {
                    TryFire(option.Key);
                }
                else if (state == SuperWeaponState.Unavailable && TryPurchase(option.Key, option.Value))
                {
                    // One purchase per decision so production gets the next look at the money.
                    return;
                }
            }
        }

        private void AdvanceCharging(float deltaTime)
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
                if (_entities.IsStationOperational(PlayerType.Opponent))
                {
                    _model.CompleteCharging(type);
                    continue;
                }

                // Same rule as player production: a charge that completes without a station is refunded.
                _model.CancelCharging(type);
                _economyModel.AddMoney(_factionsData.SuperWeaponFactionData[type].Price);
            }
        }

        private bool TryPurchase(SuperWeaponType type, FactionData data)
        {
            if (!_entities.IsStationOperational(PlayerType.Opponent) ||
                data.AvailableLevel > _factionModel.CurrentLevel ||
                _economyModel.Money < data.Price * MONEY_RESERVE_MULTIPLIER ||
                !_economyModel.TrySpend(data.Price))
                return false;

            _model.StartCharging(type);
            _chargeTimeLeft[type] = data.BuildTime;
            Debug.Log($"[EnemyAI:SuperWeapon] Purchased {type}, Cost={data.Price}, Money={_economyModel.Money}");
            return true;
        }

        private void TryFire(SuperWeaponType type)
        {
            IEntity target = SelectTarget(type);
            if (target == null) return;

            _model.Consume(type);
            _fireService.Fire(type, target);
            Debug.Log($"[EnemyAI:SuperWeapon] Fired {type} at {target.HealthModel.ShipClass} ({target.Id})");
        }

        /// <summary>The ion cannon strips the strongest shields; the other weapons hit the toughest hull.</summary>
        private IEntity SelectTarget(SuperWeaponType type)
        {
            IEntity best = null;
            float bestScore = 0f;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_fireService.CanTarget(PlayerType.Opponent, entity)) continue;
                float score = type == SuperWeaponType.IonCannon
                    ? entity.HealthModel.Shields
                    : entity.HealthModel.Hull;
                if (best != null && score <= bestScore) continue;
                best = entity;
                bestScore = score;
            }

            return best;
        }
    }
}
