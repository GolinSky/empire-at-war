using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipUiController : IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly ITooltipModelObserver _model;
        private ITooltipUi _ui;
        public TooltipUiController(IUiService uiService, ITooltipModelObserver model)
        { _uiService = uiService; _model = model; }
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
            if (_ui == null) _ui = (ITooltipUi)_uiService.CreateUi(UiType.Tooltip, _uiService.PopupCanvasTransform);
            Render();
            Place();
            _ui.Show();
        }
        private void Render() => _ui.Render(_model.Content);
        private void Place() => _ui.Place(_model.Anchor);
        private void Hide()
        {
            // The scene may destroy the Unity view before Zenject disposes the service.
            if (_ui is BaseUi view && view != null) _ui.Hide();
        }
    }
}
