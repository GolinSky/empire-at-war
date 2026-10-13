using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipUiController : UiController, IInitializable, ILateDisposable
    {
        private readonly ITooltipModelObserver _model;
        private readonly TooltipIconData _icons;
        private readonly TooltipSettings _settings;
        private TooltipUi _ui;

        public TooltipUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            ITooltipModelObserver model, TooltipIconData icons, TooltipSettings settings)
            : base(uiService, cancelRouter)
        { _model = model; _icons = icons; _settings = settings; }

        public void Initialize()
        {
            _model.Shown += Show;
            _model.ContentChanged += Render;
            _model.AnchorChanged += Place;
            _model.Hidden += Hide;
        }

        public void LateDispose()
        {
            _model.Shown -= Show;
            _model.ContentChanged -= Render;
            _model.AnchorChanged -= Place;
            _model.Hidden -= Hide;
            Hide();
        }

        private void Show()
        {
            if (_ui == null)
            {
                _ui = CreateUi<TooltipUi>();
                _ui.SetData(_icons, _settings);
            }
            Render();
            Place();
            _ui.Show();
        }

        private void Render() => _ui.Render(_model.Content);

        private void Place() => _ui.Place(_model.Anchor);

        private void Hide()
        {
            // The scene may destroy the Unity view before Zenject disposes the service.
            if (_ui != null) _ui.Hide();
        }
    }
}
