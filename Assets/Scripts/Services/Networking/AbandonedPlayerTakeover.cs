using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: a player stayed disconnected past the grace period. Their units keep their last orders;
    /// handing the slot to an AI player scope at runtime is the remaining Phase 5 work.
    /// </summary>
    public sealed class AbandonedPlayerTakeover : IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;

        public AbandonedPlayerTakeover(OnlineMatchService online)
        {
            _online = online;
        }

        public void Initialize() => _online.PlayerAbandoned += TakeOver;

        public void LateDispose() => _online.PlayerAbandoned -= TakeOver;

        private static void TakeOver(PlayerId player) =>
            Debug.LogWarning($"[Online] Player {player} did not reconnect within {OnlineMatchService.RECONNECT_GRACE_SECONDS}s.");
    }
}
