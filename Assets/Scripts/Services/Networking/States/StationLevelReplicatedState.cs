using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>
    /// A human player's station level, sent to everyone: each client shows every station at its level.
    /// On a client every remote slot (AI ones included) is a human scope, so this also receives AI levels
    /// sent by <see cref="AiStationLevelReplicatedState"/>.
    /// </summary>
    public sealed class StationLevelReplicatedState : ReplicatedState
    {
        private readonly PlayerSlot _owner;
        private readonly PlayerFactionModel _faction;
        private readonly SecondStationLevelModel _secondStation;

        public StationLevelReplicatedState(ReplicatedStateRegistry registry, IMatchSession session, PlayerSlot owner,
            PlayerFactionModel faction, SecondStationLevelModel secondStation) : base(registry, session)
        {
            _owner = owner;
            _faction = faction;
            _secondStation = secondStation;
        }

        public override string Key => KeyFor(_owner.Id);

        public static string KeyFor(PlayerId player) => $"player-{player.Index}-level";

        public override string Capture() => JsonUtility.ToJson(new StationLevelSnapshot
            { Level = _faction.CurrentLevel, SecondStationLevel = _secondStation.CurrentLevel });

        // Upgrades complete one level at a time, as on the host, so the station plays each upgrade.
        public override void Apply(string json)
        {
            StationLevelSnapshot snapshot = JsonUtility.FromJson<StationLevelSnapshot>(json);
            int level = snapshot.Level;
            if (level < _faction.CurrentLevel) _faction.CurrentLevel = level;
            while (_faction.CurrentLevel < level) _faction.CompleteLevelUpgrade();
            int secondLevel = snapshot.SecondStationLevel;
            if (secondLevel < _secondStation.CurrentLevel) _secondStation.CurrentLevel = secondLevel;
            while (_secondStation.CurrentLevel < secondLevel) _secondStation.CompleteLevelUpgrade();
        }

        // Level upgrades come from production, which FactionService stops on a client.
        protected override void FollowHost()
        {
        }
    }
}
