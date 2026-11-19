using System;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>
    /// Stations the host's victory rule has seen alive. A client that rejoins after a station fell never sees it,
    /// so without this that player would never count as defeated and the battle would not end for the client.
    /// </summary>
    public sealed class BattleVictoryReplicatedState : ReplicatedState
    {
        private readonly BattleVictoryModel _victory;

        public BattleVictoryReplicatedState(ReplicatedStateRegistry registry, IMatchSession session,
            BattleVictoryModel victory) : base(registry, session)
        {
            _victory = victory;
        }

        public override string Key => "victory";

        public override string Capture() => JsonUtility.ToJson(new Snapshot
        {
            ObservedBases = _victory.ObservedBases.Select(player => player.Index).OrderBy(index => index).ToArray()
        });

        public override void Apply(string json) =>
            _victory.RestoreObservedBases(JsonUtility.FromJson<Snapshot>(json).ObservedBases
                .Select(index => new PlayerId(index)));

        // Clients still decide the outcome from replicated units; they only add the host's observed stations.
        protected override void FollowHost()
        {
        }

        [Serializable]
        private sealed class Snapshot
        {
            public int[] ObservedBases;
        }
    }
}
