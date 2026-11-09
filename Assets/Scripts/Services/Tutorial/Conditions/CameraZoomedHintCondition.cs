using EmpireAtWar.Services.Camera;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class CameraZoomedHintCondition : IHintCondition
    {
        private readonly ICameraInput _input;

        public bool IsMet { get; private set; }

        public CameraZoomedHintCondition(ICameraInput input)
        {
            _input = input;
            _input.Zoomed += HandleZoom;
            _input.ZoomScrolled += HandleZoom;
        }

        private void HandleZoom(float amount) => IsMet = true;

        public void Dispose()
        {
            _input.Zoomed -= HandleZoom;
            _input.ZoomScrolled -= HandleZoom;
        }
    }
}
