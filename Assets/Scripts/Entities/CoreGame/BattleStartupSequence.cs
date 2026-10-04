using System.Threading;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Services.Player;
using UnityEngine;
using ViewComponents;

namespace EmpireAtWar.Controllers.Game
{
    /// <summary>The ordered battle startup steps; awaited by <see cref="SkirmishOrchestrator"/> while loading.</summary>
    public sealed class BattleStartupSequence : IBattleStartupSequence
    {
        private const float FADE_OUT_DURATION = 0.5f;

        private readonly IBattleMapLoader _mapLoader;
        private readonly IMapModelObserver _mapModel;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IPlayerRoster _playerRoster;
        private readonly ILocalPlayer _localPlayer;
        private readonly ICameraService _cameraService;
        private readonly IFogOfWarSystem _fogOfWarSystem;
        private readonly IFadeService _fadeService;

        public BattleStartupSequence(
            IBattleMapLoader mapLoader,
            IMapModelObserver mapModel,
            IPlayerRegistry playerRegistry,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            ICameraService cameraService,
            IFogOfWarSystem fogOfWarSystem,
            IFadeService fadeService)
        {
            _mapLoader = mapLoader;
            _mapModel = mapModel;
            _playerRegistry = playerRegistry;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _fogOfWarSystem = fogOfWarSystem;
            _fadeService = fadeService;
        }

        public async Awaitable RunAsync(CancellationToken cancellationToken)
        {
            // Covered before the first rendered frame, so map building and spawning stay hidden.
            _fadeService.Cover();
            // Player contexts register their station spawners in their own kernels' Start this frame.
            await Awaitable.NextFrameAsync(cancellationToken);
            await _mapLoader.LoadAsync(cancellationToken);
            foreach (PlayerSlot player in _playerRoster.Players)
            {
                _playerRegistry.GetStationSpawner(player.Id).Spawn();
            }

            _cameraService.MoveTo(_mapModel.GetStationPosition(_localPlayer.Id));
            // Spawned stations register their fog vision in their contexts' Start.
            await Awaitable.NextFrameAsync(cancellationToken);
            await Awaitable.NextFrameAsync(cancellationToken);
            _fogOfWarSystem.RevealImmediately();
            await _fadeService.FadeOutAsync(FADE_OUT_DURATION, cancellationToken);
        }
    }
}
