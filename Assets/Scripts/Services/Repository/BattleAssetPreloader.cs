using System;
using System.Threading;
using EmpireAtWar.Entities.Game;
using UnityEngine;

namespace EmpireAtWar.Repository
{
    public sealed class BattleAssetPreloader : IDisposable
    {
        private readonly IAssetHandleLoader _loader;
        private CachedAssetService _pendingAssets;
        private bool _isReady;

        public BattleAssetPreloader(IAssetHandleLoader loader)
        {
            _loader = loader;
        }

        public async Awaitable PrepareAsync(BattleDefinition battle, bool restoresSave, CancellationToken cancellationToken)
        {
            Dispose();
            CachedAssetService assets = new CachedAssetService(_loader);
            _pendingAssets = assets;
            try
            {
                await BattleAssetPreload.LoadAsync(assets, battle, restoresSave, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _isReady = true;
            }
            catch
            {
                if (ReferenceEquals(_pendingAssets, assets)) Dispose();
                throw;
            }
        }

        public CachedAssetService TakeAssets()
        {
            if (!_isReady)
                throw new InvalidOperationException("Battle assets must finish preloading before the battle scope builds.");
            CachedAssetService assets = _pendingAssets;
            _pendingAssets = null;
            _isReady = false;
            return assets;
        }

        public void Dispose()
        {
            _isReady = false;
            if (_pendingAssets != null) _pendingAssets.Dispose();
            _pendingAssets = null;
        }
    }
}
