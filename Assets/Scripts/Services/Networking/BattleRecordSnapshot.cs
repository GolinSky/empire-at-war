using System;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// The end-of-battle record a client cannot rebuild from replicated units alone: a client that rejoins
    /// mid-battle missed the time and the deaths before it loaded.
    /// </summary>
    [Serializable]
    public sealed class BattleRecordSnapshot
    {
        public float Elapsed;
        public OwnerLoss[] Losses = Array.Empty<OwnerLoss>();

        public static string Capture(BattleClock clock, BattleLossTracker losses) => JsonUtility.ToJson(
            new BattleRecordSnapshot
            {
                Elapsed = clock.Elapsed,
                Losses = losses.GetLossesByOwner()
                    .Select(entry => new OwnerLoss { Owner = entry.Owner.Index, Loss = entry.Loss }).ToArray()
            });

        public static void Apply(string json, BattleClock clock, BattleLossTracker losses)
        {
            BattleRecordSnapshot snapshot = JsonUtility.FromJson<BattleRecordSnapshot>(json);
            clock.Restore(snapshot.Elapsed);
            losses.MergeHostLosses(snapshot.Losses.Select(entry => (new PlayerId(entry.Owner), entry.Loss)));
        }

        [Serializable]
        public sealed class OwnerLoss
        {
            public int Owner;
            public BattleUnitLoss Loss;
        }
    }
}
