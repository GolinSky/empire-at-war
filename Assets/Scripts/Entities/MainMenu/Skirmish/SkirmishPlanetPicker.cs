using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public sealed class SkirmishPlanetPicker
    {
        private readonly IUiService _uiService;
        private readonly IPopupService _popups;
        private PopupHandle _handle;

        public SkirmishPlanetPicker(IUiService uiService, IPopupService popups)
        {
            _uiService = uiService;
            _popups = popups;
        }

        public void Open(ISkirmishModelObserver model, ISkirmishPresenter presenter, SkirmishUi view)
        {
            if (!model.CanEditSettings || _popups.IsOpen(_handle)) return;
            _handle = _popups.Open(new PlanetSelectionPopupUiController(_uiService, model, presenter,
                view.GetPlanetPreview, view.RestorePlanetFocus));
        }

        public void Close()
        {
            _popups.Close(_handle);
            _handle = null;
        }
    }
}
