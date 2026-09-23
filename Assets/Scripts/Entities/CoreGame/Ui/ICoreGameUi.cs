using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Presenters.Game;
using EmpireAtWar.Services.UiRouting;
using UnityEngine;

namespace EmpireAtWar.Views.Game
{
    public interface ICoreGameUi
    {
        void SetModel(ISkirmishSessionModelObserver model);
        void SetPresenter(ICoreGamePresenter presenter);
        void Initialize();
        void Dispose();
        void SetContentVisible(bool isVisible);
        void SetShipGroupLayout(bool isShipSelection);
        Transform GetRouteParent(SkirmishUiRoutePosition position);
        IEndGameView PrepareEndGameView(Transform parent);
    }
}
