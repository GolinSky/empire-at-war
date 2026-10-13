using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Loading
{
    public sealed class LoadingUiController : UiController, IInitializable, ILateDisposable
    {
        private readonly LoadingUi _ui;

        public LoadingUiController(LoadingUi ui) { _ui = ui; }

        public void Initialize() => _ui.Show();

        public void LateDispose()
        {
            if (_ui != null) _ui.Hide();
        }
    }
}
