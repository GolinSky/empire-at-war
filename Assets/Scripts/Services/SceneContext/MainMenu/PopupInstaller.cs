using EmpireAtWar.Entities.MenuUi.Popups;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class PopupInstaller : MonoInstaller
    {
        [SerializeField] private TeamColorPalette teamColorPalette;

        public override void InstallBindings()
        {
            Container.Bind<TeamColorPalette>().FromInstance(teamColorPalette).AsSingle();
            Container.Bind<SkirmishPopupModel>().AsSingle();
            Container.Bind<SettingsPopupModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<PopupUiController>().AsSingle();
        }
    }
}
