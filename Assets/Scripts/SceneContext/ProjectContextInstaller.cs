using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Services.Settings;
using WorkShop.LightWeightFramework.Repository;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<AddressableRepository>()
            .AsSingle();

        Container.BindInterfacesAndSelfTo<SceneService>()
            .AsSingle();

        Container.BindInstance(new SkirmishGameData())
            .AsSingle();

        Container.BindInterfacesAndSelfTo<SettingsService>()
            .AsSingle()
            .NonLazy();

        Container.BindInterfacesAndSelfTo<AudioService>()
            .AsSingle()
            .NonLazy();
    }
}