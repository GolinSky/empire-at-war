using System.Threading;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Player;
using UnityEngine;
using ViewComponents;

namespace EmpireAtWar.Controllers.Game
{
    /// <summary>The ordered battle startup steps; awaited by <see cref="SkirmishOrchestrator"/> while loading.</summary>
    public sealed class BattleStartupSequence : IBattleStartupSequence
    {
        private readonly IBattleMapLoader _mapLoader;
        private readonly IMapModelObserver _mapModel;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly IPlayerRoster _playerRoster;
        private readonly ILocalPlayer _localPlayer;
        private readonly ICameraService _cameraService;
        private readonly IFogOfWarSystem _fogOfWarSystem;

        public BattleStartupSequence(
            IBattleMapLoader mapLoader,
            IMapModelObserver mapModel,
            IPlayerRegistry playerRegistry,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            ICameraService cameraService,
            IFogOfWarSystem fogOfWarSystem)
        {
            _mapLoader = mapLoader;
            _mapModel = mapModel;
            _playerRegistry = playerRegistry;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _fogOfWarSystem = fogOfWarSystem;
        }

        public async Awaitable RunAsync(CancellationToken cancellationToken)
        {
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
        }
    }
}
