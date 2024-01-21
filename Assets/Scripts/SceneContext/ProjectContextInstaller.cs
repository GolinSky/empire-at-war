using EmpireAtWar.Repository;
using EmpireAtWar.Services.SceneService;
using Zenject;

public class ProjectContextInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<AddressableRepository>()
            .AsSingle();

        Container.BindInterfacesAndSelfTo<SceneService>()
            .AsSingle();
    }
}