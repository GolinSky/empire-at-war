using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Views.ReinforcementZones;

namespace EmpireAtWar.Presenters.ReinforcementZones
{
    public sealed class ReinforcementZonePresenter
    {
        private readonly ReinforcementZoneModel _model;
        private readonly IReinforcementZoneView _view;

        public ReinforcementZonePresenter(ReinforcementZoneModel model, IReinforcementZoneView view)
        {
            _model = model;
            _view = view;
            Render();
            SetVisibility(false, false);
        }

        public PlayerType Owner => _model.Owner;
        public bool IsCapturable => _view.IsCapturable;
        public bool IsRevealed { get; private set; }
        public UnityEngine.Vector3 Center => _view.Center;
        public float Radius => _view.Radius;

        public bool Tick(float deltaTime, int playerShipCount, int opponentShipCount)
        {
            bool ownerChanged = _model.Tick(deltaTime, playerShipCount, opponentShipCount);
            Render();
            return ownerChanged;
        }

        public void SetVisibility(bool isRevealed, bool isHovered)
        {
            IsRevealed = isRevealed;
            bool showCaptureUi = IsCapturable &&
                (isHovered || _model.CapturingPlayer != PlayerType.None || _model.IsContested);
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
            _view.Render(_model.Owner, _model.CapturingPlayer, _model.CaptureProgress, _model.IsContested);
        }
    }
}
