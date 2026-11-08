using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>
    /// One human's built units waiting to be placed, population and superweapon states, sent only to that player.
    /// Deploying already goes to the host as a command; the reserve shrinks here when the host's does.
    /// </summary>
    public sealed class PlayerReserveReplicatedState : ReplicatedState
    {
        private readonly PlayerSlot _owner;
        private readonly ReinforcementModel _reinforcements;
        private readonly ReinforcementService _reinforcementService;
        private readonly SuperWeaponModel _superWeapons;
        private readonly IUnitRequestFactory _requests;
        private readonly FactionRoster _roster;
        private readonly StationLevelData _levels;

        public PlayerReserveReplicatedState(ReplicatedStateRegistry registry, IMatchSession session, PlayerSlot owner,
            ReinforcementModel reinforcements, ReinforcementService reinforcementService,
            SuperWeaponModel superWeapons, IUnitRequestFactory requests, FactionRoster roster,
            StationLevelData levels) : base(registry, session)
        {
            _owner = owner;
            _reinforcements = reinforcements;
            _reinforcementService = reinforcementService;
            _superWeapons = superWeapons;
            _requests = requests;
            _roster = roster;
            _levels = levels;
        }

        public override string Key => $"player-{_owner.Id.Index}-reserve";

        public override PlayerId Audience => _owner.Id;

        public override string Capture() => JsonUtility.ToJson(new Snapshot
        {
            Reserve = _reinforcements.Reserve
                .SelectMany(entry => Enumerable.Repeat(UnitRequestSave.From(entry.Key), entry.Value)).ToArray(),
            UnitCapacity = _reinforcements.CurrentUnitCapacity,
            SuperWeapons = Enum.GetValues(typeof(SuperWeaponType)).Cast<SuperWeaponType>()
                .Select(type => SuperWeaponSave.Create(type, _superWeapons.GetState(type),
                    _superWeapons.GetCooldownRemaining(type))).ToArray()
        });

        public override void Apply(string json)
        {
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(json);
            ApplyReserve(snapshot.Reserve.Select(item => item.ToRequest(_requests, _roster, _levels)).ToList());
            _reinforcements.RestoreUnitCapacity(snapshot.UnitCapacity);
            foreach (SuperWeaponSave weapon in snapshot.SuperWeapons)
                _superWeapons.Restore(weapon.Type, weapon.State, weapon.Cooldown);
        }

        // Nothing to stop: the reserve fills only from production, which runs on the host.
        protected override void FollowHost()
        {
        }

        // Adds go through the service so the reinforcement UI announces them; removals follow the host's deploys.
        private void ApplyReserve(List<UnitRequest> hostReserve)
        {
            Dictionary<UnitLimitKey, int> hostCounts = hostReserve.GroupBy(UnitLimitKey.From)
                .ToDictionary(kind => kind.Key, kind => kind.Count());
            foreach (UnitRequest request in hostReserve.GroupBy(UnitLimitKey.From).Select(kind => kind.First()))
            {
                for (int i = _reinforcements.GetReserveCount(request); i < hostCounts[UnitLimitKey.From(request)]; i++)
                    _reinforcementService.Add(request);
            }

            foreach (KeyValuePair<UnitRequest, int> entry in _reinforcements.Reserve.ToList())
            {
                hostCounts.TryGetValue(UnitLimitKey.From(entry.Key), out int hostCount);
                for (int i = hostCount; i < entry.Value; i++) _reinforcements.ConsumeReinforcement(entry.Key);
            }
        }

        [Serializable]
        private sealed class Snapshot
        {
            public UnitRequestSave[] Reserve;
            public int UnitCapacity;
            public SuperWeaponSave[] SuperWeapons;
        }
    }
}
