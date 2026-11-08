using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;

namespace EmpireAtWar.Entities.OnlineMenu
{
    /// <summary>Main menu: adds the online panel. Mirror stays untouched until the player hosts or joins.</summary>
    public sealed class OnlineMenuLauncher : IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly SkirmishRouteUiController _skirmish;
        private readonly SurvivalRouteUiController _survival;
        private GameObject _panel;

        public OnlineMenuLauncher(OnlineMatchService online, SkirmishRouteUiController skirmish,
            SurvivalRouteUiController survival)
        {
            _online = online;
            _skirmish = skirmish;
            _survival = survival;
        }

        public void Initialize()
        {
            _panel = new GameObject(nameof(OnlineMenuPanel));
            _panel.AddComponent<OnlineMenuPanel>().Bind(_online, _skirmish, _survival);
        }

        public void LateDispose() => Object.Destroy(_panel);
    }
}
