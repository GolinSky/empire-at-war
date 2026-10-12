using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;

namespace EmpireAtWar.Services.Cheats
{
    public sealed class CheatWorldService
    {
        private const int MAX_SPAWN_ATTEMPTS = 100;

        private readonly PlayerSlot _owner;
        private readonly IPlayerRelations _relations;
        private readonly IPlayerRegistry _players;
        private readonly PlayerFactionModel _faction;
        private readonly ReinforcementModel _reinforcement;
        private readonly DefendPlatformFactory _platformFactory;
        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly ReinforcementZoneData _zoneData;

        public CheatWorldService(
            PlayerSlot owner,
            IPlayerRelations relations,
            IPlayerRegistry players,
            PlayerFactionModel faction,
            ReinforcementModel reinforcement,
            DefendPlatformFactory platformFactory,
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            ReinforcementZoneData zoneData)
        {
            _owner = owner;
            _relations = relations;
            _players = players;
            _faction = faction;
            _reinforcement = reinforcement;
            _platformFactory = platformFactory;
            _zones = zones;
            _spawnRule = spawnRule;
            _zoneData = zoneData;
        }

        public void AddEnemyMoney(PlayerId enemy, float amount)
        {
            if (!_relations.IsHostile(_owner.Id, enemy))
                throw new InvalidOperationException("The selected player is not an enemy.");
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));

            _players.GetEconomy(enemy).AddMoney(amount);
        }

        public void AddPlatformReinforcement(DefendPlatformUnitRequest request)
        {
            _faction.ReserveStructure(request);
            _reinforcement.AddReinforcement(request);
        }

        public bool ForceSpawnPlatform(DefendPlatformUnitRequest request)
        {
            if (!_zones.TryGetDefaultZoneCenter(_owner.Id, out Vector3 home)) return false;

            for (int attempt = 0; attempt < MAX_SPAWN_ATTEMPTS; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * _zoneData.HomeAreaRadius;
                Vector3 position = new Vector3(home.x + offset.x, 0f, home.z + offset.y);
                if (!_spawnRule.CanSpawnStructure(_owner.Id, position)) continue;

                DefendPlatform platform = _platformFactory.Create(_owner.Id, request.Key, position);
                _faction.ReserveStructure(request);
                platform.OnRelease += () => _faction.ReleaseStructure(request);
                return true;
            }

            return false;
        }
    }
}
