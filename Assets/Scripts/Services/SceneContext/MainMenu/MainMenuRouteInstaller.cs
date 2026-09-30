using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class MainMenuRouteInstaller : MonoInstaller
    {
        [SerializeField] private TeamColorPalette teamColorPalette;

        public override void InstallBindings()
        {
            Container.Bind<TeamColorPalette>().FromInstance(teamColorPalette).AsSingle();
            Container.Bind<SkirmishModel>().AsSingle();
            Container.Bind<SettingsModel>().AsSingle();
            Container.Bind<SettingsDraftEditor>().AsSingle();
            Container.Bind<KeyBindingEditor>().AsSingle();
            Container.BindInterfacesTo<SkirmishRouteController>().AsSingle();
            Container.BindInterfacesTo<SettingsRouteController>().AsSingle();
        }
    }
}
