using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    /// <summary>
    /// Tutorial setup on the Skirmish setup screen: the player picks faction and colour; every other control shows the
    /// fixed tutorial battle and stays locked, so its presenter intents can never arrive.
    /// </summary>
    public sealed class TutorialRouteUiController : UiController, ITutorialRoute, ISkirmishPresenter, ILateDisposable
    {
        private readonly IGameCommand _gameCommand;
        private readonly TutorialSetupModel _model;
        private readonly TeamColorPalette _palette;
        private readonly IMenuFactionSfxService _factionAudio;
        private readonly TutorialSetupHintsController _hints;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;
        private SkirmishUi _ui;
        private bool _isOpen;

        public TutorialRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter, IGameCommand gameCommand,
            TutorialSetupModel model, TeamColorPalette palette, IMenuFactionSfxService factionAudio,
            TutorialSetupHintsController hints, ITooltipService tooltipService)
            : base(uiService, cancelRouter)
        {
            _gameCommand = gameCommand;
            _model = model;
            _palette = palette;
            _factionAudio = factionAudio;
            _hints = hints;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SkirmishUi>();
                _ui.SetModel(_model);
                _ui.AssignPresenter(this);
                _ui.SetData(_palette);
                _ui.Initialize();
                _model.Changed += Render;
                _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover, HandleTooltipHover, _tooltips);
            }

            _ui.Show();
            _isOpen = true;
            Focus();
            _hints.Begin();
        }

        public void Close()
        {
            _isOpen = false;
            _hints.End();
            _tooltips.HideAll();
            _ui.Hide();
            Unfocus();
        }

        public void StartGame()
        {
            _hints.End();
            _gameCommand.StartBattle(_model.CreateBattle());
        }

        public void SelectSlotFaction(int slotIndex, int factionIndex)
        {
            RequirePlayerSlot(slotIndex);
            FactionType faction = (FactionType)factionIndex;
            bool changed = faction != _model.PlayerFaction;
            _model.SelectPlayerFaction(faction);
            if (changed) _factionAudio.PlaySelection(faction);
            _hints.Complete(TutorialAnchorId.SetupFaction);
        }

        public void SelectSlotColor(int slotIndex, int colorIndex)
        {
            RequirePlayerSlot(slotIndex);
            _model.SelectPlayerColor(colorIndex);
            _hints.Complete(TutorialAnchorId.SetupColor);
        }

        public void SelectSlotOccupant(int slotIndex, int occupantIndex) => throw Locked();
        public void SelectSlotTeam(int slotIndex, int teamIndex) => throw Locked();
        public void SelectPlanet(int index) => throw Locked();
        public void SelectMapSize(int index) => throw Locked();
        public void SelectVictoryCondition(int index) => throw Locked();
        public void SelectStartingMoney(float amount) => throw Locked();
        public void SelectSuperWeapons(bool enabled) => throw Locked();

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void LateDispose()
        {
            _model.Changed -= Render;
            if (_tooltipHover != null) _tooltipHover.Dispose();
            if (_ui != null) _ui.Dispose();
            _tooltipHover = null;
            _ui = null;
            Unfocus();
        }

        private void Render() => _ui.Render();

        private static void RequirePlayerSlot(int slotIndex)
        {
            if (slotIndex != TutorialSetupModel.PLAYER_SLOT)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Only the player's row is editable.");
        }

        private static InvalidOperationException Locked() =>
            new InvalidOperationException("The tutorial battle setup is fixed.");

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isOpen, () => new TooltipContent(title: (string)key,
                description: (string)key switch
                {
                    "Start battle" => "Start the tutorial battle.",
                    "Close" => "Return to mode selection.",
                    "Planet" => $"Fixed for the tutorial: {_model.Planet}.",
                    "Map size" => $"Fixed for the tutorial: {_model.MapSize}.",
                    "Victory condition" => "Fixed for the tutorial: destroy the opponent's base.",
                    "Starting credits" => $"Fixed for the tutorial: {_model.StartingMoney:0} credits.",
                    "Player" => "Choose your faction and colour. The tutorial sets your opponent and the teams.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));
    }
}
