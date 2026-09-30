using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using Zenject;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public class SkirmishRouteController : UiController, ISkirmishRoute, ISkirmishRouteNavigation, ILateDisposable
    {
        private readonly TeamColorPalette _teamColorPalette;
        private readonly IGameCommand _gameCommand;
        private readonly SkirmishModel _model;

        private ISkirmishUi _ui;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;
        private bool _isOpen;

        public SkirmishRouteController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IGameCommand gameCommand,
            SkirmishModel model,
            TeamColorPalette teamColorPalette, ITooltipService tooltips) : base(uiService, cancelRouter)
        {
            _teamColorPalette = teamColorPalette;
            _gameCommand = gameCommand;
            _model = model;
            _tooltips = new TooltipRequests(tooltips);
        }

        public void Open()
        {
            if (_ui == null)
            {
                BaseUi ui = UiService.CreateUi(UiType.Skirmish);
                _ui = ui as ISkirmishUi
                    ?? throw new InvalidOperationException(
                        "The skirmish setup prefab does not implement ISkirmishUi.");
                _ui.SetModel(_model);
                _ui.SetNavigation(this);
                _ui.SetData(_teamColorPalette);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(((ITooltipHoverView)_ui).TooltipHover,
                    HandleTooltipHover, _tooltips);
            }

            _ui.Show();
            _isOpen = true;
            Focus();
        }

        public void Close()
        {
            _isOpen = false;
            _tooltips.HideAll();
            _ui.Hide();
            Unfocus();
        }

        // Escape closes the open menu screen and returns to the main menu.
        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void StartGame()
        {
            if (!_model.CanStart)
            {
                return;
            }

            _gameCommand.StartGame(
                _model.CreatePlayers(),
                _model.Planet,
                _model.MapSize,
                _model.VictoryCondition,
                _model.StartingMoney);
        }

        public void SelectSlotOccupant(int slotIndex, int occupantIndex)
        {
            _model.SelectSlotOccupant(slotIndex, (SkirmishSlotOccupant)occupantIndex);
        }

        public void SelectSlotFaction(int slotIndex, int factionIndex)
        {
            _model.SelectSlotFaction(slotIndex, (FactionType)factionIndex);
        }

        public void SelectSlotTeam(int slotIndex, int teamIndex)
        {
            _model.SelectSlotTeam(slotIndex, teamIndex);
        }

        public void SelectSlotColor(int slotIndex, int colorIndex)
        {
            _model.SelectSlotColor(slotIndex, colorIndex);
        }

        public void SelectPlanet(int index)
        {
            _model.SelectPlanet((PlanetType)index);
        }

        public void SelectMapSize(int index)
        {
            _model.SelectMapSize((MapSize)index);
        }

        public void SelectVictoryCondition(int index)
        {
            _model.SelectVictoryCondition((BattleVictoryCondition)index);
        }

        public void SelectStartingMoney(float amount)
        {
            _model.SelectStartingMoney(amount);
        }

        public void LateDispose()
        {
            if (_ui != null)
            {
                _ui.Dispose();
                _tooltipHover.Dispose();
            }
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isOpen, () => new TooltipContent((string)key,
                (string)key switch
                {
                    "Start battle" => _model.CanStart ? "Start the battle with the current setup." : "Choose at least two opposing teams to start a battle.",
                    "Close" => "Return to the main menu.",
                    "Planet" => $"Choose the battlefield. Current: {_model.Planet}.",
                    "Map size" => $"Choose the generated battlefield size. Current: {_model.MapSize}.",
                    "Victory condition" => $"Choose the battle objective. Current: {_model.VictoryCondition}.",
                    "Starting credits" => $"Credits per player at battle start: {_model.StartingMoney:0}.",
                    "Player" => "Choose the occupant, AI difficulty, faction, team and colour. Players on the same team are allies.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));
    }
}
