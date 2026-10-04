using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Presenters.Game;
using EmpireAtWar.Services.UiRouting;
using UnityEngine;

namespace EmpireAtWar.Views.Game
{
    public interface ICoreGameUi
    {
        IUnitActionsView UnitActionsView { get; }
        ISuperWeaponsView SuperWeaponsView { get; }

        void Initialize();

        void Dispose();

        void SetTimeControls(bool isPaused, GameSpeed speed);

        void SetPresenter(ICoreGamePresenter presenter);

        void SetContentVisible(bool isVisible);

        void SetContentLayout(bool isFactionSelection, bool isShipGroupSelection);

        void SetHudStatus(string faction, int level, int selectionCount, bool battleEnded);

        Transform GetRouteParent(SkirmishUiRoutePosition position);

        IEndGameView PrepareEndGameView(Transform parent);
    }
}
