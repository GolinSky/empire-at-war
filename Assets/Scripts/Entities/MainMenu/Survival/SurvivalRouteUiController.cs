using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    public sealed class SurvivalRouteUiController : UiController, ISurvivalRoute, ISurvivalPresenter, ILateDisposable
    {
        private readonly SurvivalModel _model;
        private readonly IGameCommand _gameCommand;
        private readonly IMenuFactionSfxService _factionAudio;
        private SurvivalUi _ui;
        public SurvivalRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter, SurvivalModel model,
            IGameCommand gameCommand, IMenuFactionSfxService factionAudio) : base(uiService, cancelRouter)
        {
            _model = model;
            _gameCommand = gameCommand;
            _factionAudio = factionAudio;
        }

        public void StartBattle() => _gameCommand.StartBattle(_model.CreateBattle());

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SurvivalUi>();
                _ui.SetModel(_model);
                _ui.AssignPresenter(this);
                _ui.Initialize();
                _model.Changed += Render;
            }
            _ui.Show();
            Focus();
        }

        public void Close() { _ui.Hide(); Unfocus(); }
        protected override bool HandleCancel() { Close(); return true; }
        public void SelectPlayer(int index)
        {
            FactionType faction = FactionAt(index);
            bool changed = _model.PlayerFaction != faction;
            _model.SelectPlayer(faction);
            if (changed) _factionAudio.PlaySelection(faction);
        }
        public void SelectAlly(int index) => _model.SelectAlly(index == 0 ? null : FactionAt(index - 1));
        public void SelectEnemy(int index) => _model.SelectEnemy(FactionAt(index));
        public void SelectMapSize(int index) => _model.SelectMapSize((MapSize)index);
        public void SelectDifficulty(int index) => _model.SelectDifficulty((EnemyAiDifficulty)index);
        public void SelectSuperWeapons(bool enabled) => _model.SelectSuperWeapons(enabled);
        private static FactionType FactionAt(int index) => (FactionType)Enum.GetValues(typeof(FactionType)).GetValue(index);
        private void Render() => _ui.Render();

        public void LateDispose()
        {
            _model.Changed -= Render;
            if (_ui != null) _ui.Dispose();
            _ui = null;
            Unfocus();
        }
    }
}
