using EmpireAtWar.Repository;
using EmpireAtWar.SceneContext;
using WorkShop.LightWeightFramework.Factory;
using WorkShop.LightWeightFramework.Game;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        //game
        Container.BindInterfacesAndSelfTo<MyCustomGameContext>()
            .AsSingle();
        
        Container.BindInterfacesAndSelfTo<AddressableRepository>()
            .AsSingle();
        
        Container.BindInterfacesAndSelfTo<FeatureAbstractFactory>()
            .AsCached();
        
        Container.BindInterfacesAndSelfTo<Game>()
            .AsCached();
        //game
        ///////////////////////////////////////////////////////////////
    }
}