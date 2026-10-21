using System;
using System.Threading.Tasks;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Services.Saves
{
    public sealed class BattleSaveActions : IDisposable
    {
        private readonly BattleSaveService _saves;
        private readonly IBattleSaveRepository _repository;
        private readonly IGameCommand _game;
        private readonly BattleDefinition _battle;
        private bool _disposed;

        public event Action<string> StatusChanged;
        public string Status { get; private set; } = "";
        public bool IsSupported => _battle.Outcome != OutcomeRule.Simulation;
        public bool CanSave => !_disposed && _saves.CanSave;

        public BattleSaveActions(BattleSaveService saves, IBattleSaveRepository repository,
            IGameCommand game, BattleDefinition battle)
        {
            _saves = saves;
            _repository = repository;
            _game = game;
            _battle = battle;
        }

        public void SaveNamed(string name)
        {
            try { Save(BattleSaveSlot.Named(name)); }
            catch (ArgumentException exception) { SetStatus(exception.Message); }
        }

        public void Save(BattleSaveSlot slot) => _ = SaveAsync(slot);

        public async Task SaveAsync(BattleSaveSlot slot)
        {
            if (_disposed) return;
            try
            {
                SetStatus($"Saving {slot.Name}...");
                await _saves.SaveAsync(slot);
                SetStatus($"Saved {slot.Name}.");
            }
            catch (Exception exception)
            {
                SetStatus($"Save failed: {exception.Message}");
            }
        }

        public void QuickLoad()
        {
            if (_disposed) return;
            try
            {
                if (!_saves.CanLoad) throw new InvalidOperationException("Loading is unavailable before battle startup completes, during a save, or in Battle Simulation.");
                _game.LoadBattle(_repository.Load(_battle.Mode, BattleSaveSlot.Quicksave));
            }
            catch (Exception exception) { SetStatus($"Load failed: {exception.Message}"); }
        }

        private void SetStatus(string status)
        {
            if (_disposed) return;
            Status = status;
            if (StatusChanged != null) StatusChanged(status);
        }

        public void Dispose()
        {
            // Let an in-flight file write finish, but detach the battle UI before its continuation.
            _disposed = true;
            StatusChanged = null;
        }
    }
}
