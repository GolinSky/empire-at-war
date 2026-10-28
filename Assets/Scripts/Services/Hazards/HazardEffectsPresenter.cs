using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Hazards
{
    public sealed class HazardEffectsPresenter : IInitializable, ILateDisposable
    {
        private readonly IHazardService _hazards;
        private readonly IHazardEffectsView _view;

        public HazardEffectsPresenter(IHazardService hazards, IHazardEffectsView view)
        {
            _hazards = hazards;
            _view = view;
        }

        public void Initialize() => _hazards.Entered += _view.ShowHazardEntry;

        public void LateDispose() => _hazards.Entered -= _view.ShowHazardEntry;
    }
}
