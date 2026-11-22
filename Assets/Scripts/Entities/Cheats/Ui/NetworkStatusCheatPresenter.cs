using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Diagnostics;
using EmpireAtWar.Views.Cheats;

namespace EmpireAtWar.Presenters.Cheats
{
    /// <summary>Cheats ▸ Debug: shows the network status overlay toggle in online battles and applies it.</summary>
    public sealed class NetworkStatusCheatPresenter : IInitializable, ILateDisposable
    {
        private readonly ICheatView _view;
        private readonly NetworkStatusModel _model;

        public NetworkStatusCheatPresenter(ICheatView view, NetworkStatusModel model)
        {
            _view = view;
            _model = model;
        }

        public void Initialize()
        {
            _view.SetNetworkStatusAvailable(_model.IsAvailable);
            _view.NetworkStatusToggled += Toggle;
        }

        public void LateDispose() => _view.NetworkStatusToggled -= Toggle;

        private void Toggle(bool visible)
        {
            _model.SetVisible(visible);
            _view.SetStatus(visible
                ? "Network status on. Every online battle also writes BattleTelemetry/<scene>_<time>_<role>_net.csv (+ _pools, _links)."
                : "Network status off.");
        }
    }
}
