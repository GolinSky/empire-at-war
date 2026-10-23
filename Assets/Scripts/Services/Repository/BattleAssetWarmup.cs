using System;
using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Repository
{
    // Loads unit views and wrecks into the battle cache once the battle runs, a few at a time, so the first
    // spawn or death of each type finds its prefab loaded. A spawn that comes first waits on that one handle.
    public sealed class BattleAssetWarmup : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private const int MAX_IN_FLIGHT = 4;

        private readonly CachedAssetService _assets;
        private readonly BattleDefinition _battle;
        private readonly INotifier<BattleState> _battleState;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private bool _isStarted;

        public BattleAssetWarmup(CachedAssetService assets, BattleDefinition battle, INotifier<BattleState> battleState)
        {
            _assets = assets;
            _battle = battle;
            _battleState = battleState;
        }

        public void Initialize() => _battleState.AddObserver(this);

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _cancellation.Cancel();
        }

        public void UpdateState(BattleState state)
        {
            if (state != BattleState.Running || _isStarted) return;
            _isStarted = true;
            WarmAsync(_cancellation.Token);
        }

        private async void WarmAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<string> keys = BattleAssetPreload.DeferredPrefabKeys(_assets, _battle);
                for (int start = 0; start < keys.Count; start += MAX_IN_FLIGHT)
                {
                    int end = Math.Min(start + MAX_IN_FLIGHT, keys.Count);
                    for (int index = start; index < end; index++) _assets.BeginPreload<GameObject>(keys[index]);
                    await _assets.CompletePreloadsAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
