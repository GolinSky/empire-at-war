using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    [LifecycleOrder(Initialize = -300, Dispose = 300)]
    public sealed class PlayerScopeBootstrap : IInitializable, IOrderedDisposable
    {
        private readonly SkirmishScope _owner;
        private readonly IPlayerRoster _roster;
        private readonly List<AssetLifetimeScope> _players = new List<AssetLifetimeScope>();

        public PlayerScopeBootstrap(SkirmishScope owner, IPlayerRoster roster)
        {
            _owner = owner;
            _roster = roster;
        }

        public void Initialize()
        {
            foreach (PlayerSlot slot in _roster.Players)
            {
                GameObject playerRoot = new GameObject($"Player{slot.Id.Index}");
                playerRoot.SetActive(false);
                playerRoot.transform.SetParent(_owner.transform, false);
                AssetLifetimeScope player;
                if (slot.IsAi)
                {
                    AiPlayerScope ai = playerRoot.AddComponent<AiPlayerScope>();
                    ai.ConfigurePlayer(slot);
                    player = ai;
                }
                else
                {
                    LocalPlayerScope local = playerRoot.AddComponent<LocalPlayerScope>();
                    local.ConfigurePlayer(slot);
                    player = local;
                }

                player.autoRun = false;
                player.parentReference.Object = _owner;
                _players.Add(player);
                player.Build();
                playerRoot.SetActive(true);
            }
        }

        public void Dispose()
        {
            for (int index = _players.Count - 1; index >= 0; index--)
            {
                AssetLifetimeScope player = _players[index];
                try
                {
                    player.DisposeCore();
                }
                finally
                {
                    if (Application.isPlaying) Object.Destroy(player.gameObject);
                    else Object.DestroyImmediate(player.gameObject);
                }
            }
            _players.Clear();
        }
    }
}
