using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Player;
using UnityEngine;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Weapon;

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
        private readonly IReadOnlyList<IBattleRevealStep> _revealSteps;
        private readonly IFadeService _fadeService;
        private readonly BattleDefinition _battle;
        private readonly BattleRestoreStep _restore;
        private readonly ShotPoolPrewarmer _shots;
        private readonly IAssetService _assets;
        private readonly IMatchSession _session;
        private readonly IBattleStartGate _startGate;

        public BattleStartupSequence(
            IBattleMapLoader mapLoader,
            IMapModelObserver mapModel,
            IPlayerRegistry playerRegistry,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            ICameraService cameraService,
            IReadOnlyList<IBattleRevealStep> revealSteps,
            IFadeService fadeService, BattleDefinition battle, BattleRestoreStep restore,
            ShotPoolPrewarmer shots, IAssetService assets, IMatchSession session, IBattleStartGate startGate)
        {
            _mapLoader = mapLoader;
            _mapModel = mapModel;
            _playerRegistry = playerRegistry;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _revealSteps = revealSteps;
            _fadeService = fadeService;
            _battle = battle;
            _restore = restore;
            _shots = shots;
            _assets = assets;
            _session = session;
            _startGate = startGate;
        }

        public async Awaitable RunAsync(CancellationToken cancellationToken)
        {
            // Covered before the first rendered frame, so map building and spawning stay hidden.
            _fadeService.Cover();
            // Player contexts register their station spawners in their own kernels' Start this frame.
            await Awaitable.NextFrameAsync(cancellationToken);
            await _mapLoader.LoadAsync(cancellationToken);
            await _shots.PrewarmRosterAsync(_assets, _battle, cancellationToken);
            foreach (PlayerSlot player in _playerRoster.Players)
                await _playerRegistry.GetSquadronPrewarmer(player.Id).PrewarmAsync(cancellationToken);
            // Online clients get the host's stations as replicated spawns.
            if (!_restore.TryRestore())
            {
                foreach (PlayerSlot player in _playerRoster.Players)
                {
                    if (_session.IsAuthority && _battle.GetPlayer(player.Id).OwnsStation)
                        _playerRegistry.GetStationSpawner(player.Id).Spawn();
                }
                _cameraService.MoveTo(_mapModel.GetStationPosition(_localPlayer.Id));
            }
            // Spawned stations register their fog vision in their contexts' Start.
            await Awaitable.NextFrameAsync(cancellationToken);
            await Awaitable.NextFrameAsync(cancellationToken);
            await _shots.CompletePrewarmAsync(cancellationToken);
            foreach (IBattleRevealStep step in _revealSteps)
                step.RevealImmediately();
            // Online: every PC has loaded before the cover lifts; clients have the host's baseline by then.
            await _startGate.WaitAsync(cancellationToken);
            // Render the revealed battle once while the loading cover is still visible.
            await Awaitable.NextFrameAsync(cancellationToken);
            await _fadeService.FadeOutAsync(FADE_OUT_DURATION, cancellationToken);
        }
    }
}
