using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public class PopupUiController : IMainMenuPopupPresenter,
        ISkirmishPopupPresenter, ISettingsPopupPresenter, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly TeamColorPalette _teamColorPalette;
        private readonly IGameCommand _gameCommand;
        private readonly ISettingsService _settingsService;
        private readonly SkirmishPopupModel _skirmishModel;
        private readonly SettingsPopupModel _settingsModel;

        private ISkirmishPopupUi _skirmishUi;
        private ISettingsPopupUi _settingsUi;

        public PopupUiController(
            IUiService uiService,
            IGameCommand gameCommand,
            ISettingsService settingsService,
            SkirmishPopupModel skirmishModel,
            SettingsPopupModel settingsModel,
            TeamColorPalette teamColorPalette)
        {
            _teamColorPalette = teamColorPalette;
            _uiService = uiService;
            _gameCommand = gameCommand;
            _settingsService = settingsService;
            _skirmishModel = skirmishModel;
            _settingsModel = settingsModel;
        }

        public void OpenSkirmish()
        {
            if (_skirmishUi == null)
            {
                BaseUi ui = _uiService.CreateUi(
                    UiType.SkirmishGameSetUpPopup, _uiService.PopupCanvasTransform);
                _skirmishUi = ui as ISkirmishPopupUi
                    ?? throw new InvalidOperationException(
                        "The skirmish popup prefab does not implement ISkirmishPopupUi.");
                _skirmishUi.SetModel(_skirmishModel);
                _skirmishUi.SetPresenter(this);
                _skirmishUi.SetData(_teamColorPalette);
                _skirmishUi.Initialize();
            }

            _skirmishUi.Show();
        }

        public void OpenSettings()
        {
            _settingsModel.Configure(
                _settingsService.GetQualityPresets(),
                _settingsService.GetCurrentQualityPresetIndex());

            if (_settingsUi == null)
            {
                BaseUi ui = _uiService.CreateUi(
                    UiType.SettingsPopup, _uiService.PopupCanvasTransform);
                _settingsUi = ui as ISettingsPopupUi
                    ?? throw new InvalidOperationException(
                        "The settings popup prefab does not implement ISettingsPopupUi.");
                _settingsUi.SetModel(_settingsModel);
                _settingsUi.SetPresenter(this);
                _settingsUi.Initialize();
            }
            else
            {
                _settingsUi.Render();
            }

            _settingsUi.Show();
        }

        public void CloseSkirmish()
        {
            _skirmishUi.Hide();
        }

        public void CloseSettings()
        {
            _settingsUi.Hide();
        }

        public void StartGame()
        {
            if (!_skirmishModel.CanStart)
            {
                return;
            }

            _gameCommand.StartGame(
                _skirmishModel.CreatePlayers(),
                _skirmishModel.Planet,
                _skirmishModel.MapSize,
                _skirmishModel.VictoryCondition,
                _skirmishModel.StartingMoney);
        }

        public void SelectSlotOccupant(int slotIndex, int occupantIndex)
        {
            _skirmishModel.SelectSlotOccupant(slotIndex, (SkirmishSlotOccupant)occupantIndex);
        }

        public void SelectSlotFaction(int slotIndex, int factionIndex)
        {
            _skirmishModel.SelectSlotFaction(slotIndex, (FactionType)factionIndex);
        }

        public void SelectSlotTeam(int slotIndex, int teamIndex)
        {
            _skirmishModel.SelectSlotTeam(slotIndex, teamIndex);
        }

        public void SelectSlotColor(int slotIndex, int colorIndex)
        {
            _skirmishModel.SelectSlotColor(slotIndex, colorIndex);
        }

        public void SelectPlanet(int index)
        {
            _skirmishModel.SelectPlanet((PlanetType)index);
        }

        public void SelectMapSize(int index)
        {
            _skirmishModel.SelectMapSize((MapSize)index);
        }

        public void SelectVictoryCondition(int index)
        {
            _skirmishModel.SelectVictoryCondition((BattleVictoryCondition)index);
        }

        public void SelectStartingMoney(float amount)
        {
            _skirmishModel.SelectStartingMoney(amount);
        }

        public void SelectQualityPreset(int index)
        {
            _settingsModel.SelectQualityPreset(index);
        }

        public void ApplySettings()
        {
            _settingsService.SetQualityPreset(_settingsModel.SelectedIndex);
        }

        public void LateDispose()
        {
            if (_skirmishUi != null)
            {
                _skirmishUi.Dispose();
            }

            if (_settingsUi != null)
            {
                _settingsUi.Dispose();
            }
        }
    }
}
