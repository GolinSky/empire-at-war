using EmpireAtWar.Models.SkirmishGame;
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
        void SetModel(ISkirmishSessionModelObserver model);
        void SetPresenter(ICoreGamePresenter presenter);
        void Initialize();
        void Dispose();
        void SetContentVisible(bool isVisible);
        void SetContentLayout(bool isFactionSelection, bool isShipGroupSelection);
        Transform GetRouteParent(SkirmishUiRoutePosition position);
        IEndGameView PrepareEndGameView(Transform parent);
    }
}
