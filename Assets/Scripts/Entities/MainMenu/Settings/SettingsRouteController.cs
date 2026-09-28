using System;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsRouteController : ISettingsRoute, ISettingsRouteNavigation, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly ISettingsService _settingsService;
        private readonly SettingsModel _model;

        private ISettingsUi _ui;

        public SettingsRouteController(
            IUiService uiService,
            ISettingsService settingsService,
            SettingsModel model)
        {
            _uiService = uiService;
            _settingsService = settingsService;
            _model = model;
        }

        public void Open()
        {
            _model.Configure(
                _settingsService.GetQualityPresets(),
                _settingsService.GetCurrentQualityPresetIndex());

            if (_ui == null)
            {
                BaseUi ui = _uiService.CreateUi(UiType.Settings);
                _ui = ui as ISettingsUi
                    ?? throw new InvalidOperationException(
                        "The settings prefab does not implement ISettingsUi.");
                _ui.SetModel(_model);
                _ui.SetNavigation(this);
                _ui.Initialize();
            }
            else
            {
                _ui.Render();
            }

            _ui.Show();
        }

        public void Close()
        {
            _ui.Hide();
        }

        public void SelectQualityPreset(int index)
        {
            _model.SelectQualityPreset(index);
        }

        public void ApplySettings()
        {
            _settingsService.SetQualityPreset(_model.SelectedIndex);
        }

        public void LateDispose()
        {
            if (_ui != null)
            {
                _ui.Dispose();
            }
        }
    }
}
