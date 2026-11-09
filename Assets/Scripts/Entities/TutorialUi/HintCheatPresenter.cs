using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Views.Cheats;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>Cheats ▸ Tutorial: reset hint progress and restart the hint track in the running match.</summary>
    public sealed class HintCheatPresenter : IInitializable, ILateDisposable
    {
        private readonly ICheatView _view;
        private readonly IHintService _hints;

        public HintCheatPresenter(ICheatView view, IHintService hints)
        {
            _view = view;
            _hints = hints;
        }

        public void Initialize() => _view.RestartHintsRequested += Restart;

        private void Restart()
        {
            _hints.Restart();
            _view.SetStatus("Tutorial hints reset; starting from the first hint.");
        }

        public void LateDispose() => _view.RestartHintsRequested -= Restart;
    }
}
