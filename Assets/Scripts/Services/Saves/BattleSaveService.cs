using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CinematicCamera.Controller;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Camera;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Takes a snapshot of the running battle on the main thread and writes it on a worker thread.
    /// Refuses Battle Simulation, a battle that is loading or ended, and a second save while one is being written.
    /// </summary>
    public sealed class BattleSaveService
    {
        private readonly BattleDefinition _battle;
        private readonly SkirmishStateModel _state;
        private readonly ICameraService _camera;
        private readonly ICinematicReturnPose _cinematic;
        private readonly BattleClock _clock;
        private readonly IBattleSaveRepository _repository;
        private readonly UnitSaveService _units;
        private readonly IReadOnlyList<IBattleSaveSource> _sources;

        public bool IsSaving { get; private set; }
        public bool CanSave => GetRefusal() == null;
        public bool CanLoad => _battle.Outcome != OutcomeRule.Simulation && !IsSaving &&
            (_state.State == BattleState.Running || _state.State == BattleState.Paused ||
             _state.State == BattleState.Ended);

        public BattleSaveService(BattleDefinition battle, SkirmishStateModel state, ICameraService camera,
            ICinematicReturnPose cinematic, BattleClock clock, IBattleSaveRepository repository, UnitSaveService units,
            IReadOnlyList<IBattleSaveSource> sources)
        {
            _battle = battle;
            _state = state;
            _camera = camera;
            _cinematic = cinematic;
            _clock = clock;
            _repository = repository;
            _units = units;
            _sources = sources;
        }

        /// <summary>The battle as it is now; throws <see cref="InvalidOperationException"/> when saving is refused.</summary>
        public BattleSave Capture(BattleSaveSlot slot)
        {
            string refusal = GetRefusal();
            if (refusal != null) throw new InvalidOperationException(refusal);
            BattleSave save = BattleSave.Create(
                BattleSaveHeader.Create(_battle.Mode, slot, DateTime.UtcNow, _clock.Elapsed, _state.Speed),
                BattleSetupSave.From(_battle),
                CaptureCamera(),
                _units.CaptureUnits(), _units.CaptureStructures());
            foreach (IBattleSaveSource source in _sources) source.Capture(save);
            return save;
        }

        public async Awaitable SaveAsync(BattleSaveSlot slot)
        {
            BattleSave save = Capture(slot);
            IsSaving = true;
            try
            {
                await Awaitable.BackgroundThreadAsync();
                _repository.Save(save);
            }
            finally
            {
                await Awaitable.MainThreadAsync();
                IsSaving = false;
            }
        }

        // A cinematic shot is not a gameplay view: the save keeps the pose the mode returns to, at the zoom its exit
        // resets to.
        private CameraPoseSave CaptureCamera() =>
            _cinematic.TryGetReturnPose(out Vector3 position, out Quaternion rotation)
                ? CameraPoseSave.Create(position, rotation, position.y)
                : CameraPoseSave.Create(_camera.CameraPosition, _camera.CameraTransform.rotation, _camera.ZoomTargetHeight);

        private string GetRefusal()
        {
            if (_battle.Outcome == OutcomeRule.Simulation) return "Battle Simulation is not saved.";
            if (_state.State != BattleState.Running && _state.State != BattleState.Paused)
                return $"A battle cannot be saved while it is {_state.State}.";
            return IsSaving ? "A save is already being written." : null;
        }
    }
}
