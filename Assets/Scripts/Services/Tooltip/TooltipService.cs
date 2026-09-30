using Zenject;

namespace EmpireAtWar.Services.Tooltip
{
    public sealed class TooltipService : ITooltipService, ITickable, ILateDisposable
    {
        private readonly TooltipModel _model;
        private readonly TooltipTiming _timing;
        private readonly ITooltipClock _clock;
        private ITooltipContentProvider _provider;
        private TooltipHandle _handle;
        private long _nextId;
        private float _elapsed;

        public TooltipService(TooltipModel model, TooltipTiming timing, ITooltipClock clock)
        { _model = model; _timing = timing; _clock = clock; }

        public TooltipHandle Show(ITooltipContentProvider provider, TooltipAnchor anchor)
        {
            if (_provider != null && Equals(_provider.Source, provider.Source) && Equals(_provider.Key, provider.Key))
            {
                _provider = provider;
                _model.SetAnchor(anchor);
                return _handle;
            }

            _model.Hide();
            _provider = provider;
            _handle = new TooltipHandle(++_nextId);
            _elapsed = 0f;
            _model.SetAnchor(anchor);
            return _handle;
        }

        public void Hide(TooltipHandle handle)
        {
            if (_provider != null && handle.Id == _handle.Id) HideAll();
        }

        public void HideAll()
        {
            _provider = null;
            _elapsed = 0f;
            _model.Hide();
        }

        public void Tick()
        {
            if (_provider == null) return;
            if (!_provider.IsValid) { HideAll(); return; }
            _elapsed += _clock.DeltaTime;
            if (!_model.IsVisible)
            {
                if (_elapsed < _timing.ShowDelay) return;
                _elapsed = 0f;
                _model.Show(_provider.Build(), _model.Anchor);
            }
            else if (_elapsed >= _timing.RefreshInterval)
            {
                _elapsed = 0f;
                _model.Refresh(_provider.Build());
            }
        }

        public void LateDispose() => HideAll();
    }
}
