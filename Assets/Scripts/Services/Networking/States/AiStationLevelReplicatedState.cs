using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>Host: an AI player's station level for clients (AI scopes exist only on the host).</summary>
    public sealed class AiStationLevelReplicatedState : ReplicatedState
    {
        private readonly PlayerSlot _owner;
        private readonly EnemyFactionModel _level;
        private readonly SecondStationLevelModel _secondStation;

        public AiStationLevelReplicatedState(ReplicatedStateRegistry registry, IMatchSession session, PlayerSlot owner,
            EnemyFactionModel level, SecondStationLevelModel secondStation) : base(registry, session)
        {
            _owner = owner;
            _level = level;
            _secondStation = secondStation;
        }

        public override string Key => StationLevelReplicatedState.KeyFor(_owner.Id);

        public override string Capture() => JsonUtility.ToJson(new StationLevelSnapshot
            { Level = _level.CurrentLevel, SecondStationLevel = _secondStation.CurrentLevel });

        public override void Apply(string json) =>
            throw new InvalidOperationException("AI players run only on the host; clients apply levels in human scopes.");

        protected override void FollowHost() =>
            throw new InvalidOperationException("AI players run only on the host.");
    }
}
