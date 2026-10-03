using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Views.ReinforcementZones;

namespace EmpireAtWar.Presenters.ReinforcementZones
{
    public sealed class ReinforcementZonePresenter
    {
        private readonly IReinforcementZoneView _view;
        private readonly ILocalPlayer _localPlayer;

        private readonly ReinforcementZoneModel _model;

        public PlayerId Owner => _model.Owner;
        public bool IsCapturable => _view.IsCapturable;
        public bool IsRevealed { get; private set; }
        public UnityEngine.Vector3 Center => _view.Center;
        public float Radius => _view.Radius;

        public ReinforcementZonePresenter(
            IReinforcementZoneView view,
            ILocalPlayer localPlayer,
            ReinforcementZoneModel model)
        {
            _localPlayer = localPlayer;
            _model = model;
            _view = view;
            Render();
            SetVisibility(false, false);
        }

        public bool Tick(float deltaTime, CaptureStrength tally)
        {
            bool ownerChanged = _model.Tick(deltaTime, tally);
            Render();
            return ownerChanged;
        }

        public void SetVisibility(bool isRevealed, bool isHovered)
        {
            IsRevealed = isRevealed;
            bool showCaptureUi = IsCapturable &&
                (isHovered || _model.CapturingPlayer != PlayerId.None || _model.IsContested);
            _view.SetVisibility(true, isRevealed && showCaptureUi);
        }

        public bool Contains(UnityEngine.Vector3 position)
        {
            float x = position.x - Center.x;
            float z = position.z - Center.z;
            return x * x + z * z <= Radius * Radius;
        }

        private void Render()
        {
            _view.Render(
                _localPlayer.GetRelation(_model.Owner),
                _localPlayer.GetRelation(_model.CapturingPlayer),
                _model.CaptureProgress,
                _model.IsContested);
        }
    }
}
