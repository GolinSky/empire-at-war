using EmpireAtWar.Repository;
using EmpireAtWar.SceneContext;
using EmpireAtWar.Services.SceneService;
using WorkShop.LightWeightFramework.Factory;
using WorkShop.LightWeightFramework.Game;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<MyCustomGameContext>()
            .AsSingle();//delete
        
        Container.BindInterfacesAndSelfTo<AddressableRepository>()
            .AsSingle();
        
        Container.BindInterfacesAndSelfTo<FeatureAbstractFactory>()
            .AsCached();//delete
        
        Container.BindInterfacesAndSelfTo<Game>()
            .AsCached();//rework

        Container.BindInterfacesAndSelfTo<SceneService>()
            .AsSingle();
    }
}