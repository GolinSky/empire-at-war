using System;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Camera;

namespace EmpireAtWar.Services.Saves
{
    public sealed class BattleRestoreStep
    {
        private readonly GameData _game;
        private readonly BattleRestorer _restorer;
        private readonly BattleClock _clock;
        private readonly SkirmishStateModel _state;
        private readonly ICameraService _camera;

        public BattleRestoreStep(GameData game, BattleRestorer restorer, BattleClock clock,
            SkirmishStateModel state, ICameraService camera)
        {
            _game = game;
            _restorer = restorer;
            _clock = clock;
            _state = state;
            _camera = camera;
        }

        public bool TryRestore()
        {
            BattleSave save = _game.PendingBattleSave;
            if (save == null) return false;
            _game.PendingBattleSave = null;
            try
            {
                _restorer.Restore(save);
                _clock.Restore(save.Header.BattleTime);
                _state.SetSpeed(save.Header.GameSpeed);
                _camera.RestorePose(save.Camera.Position, save.Camera.Rotation, save.Camera.ZoomTargetHeight);
                return true;
            }
            catch (Exception exception)
            {
                _game.PendingLoadError = $"Load failed: {exception.Message}";
                throw;
            }
        }
    }
}
