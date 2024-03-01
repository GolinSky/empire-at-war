using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class SkirmishFactoryInstaller:MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindFactory<IModel, IMovable, SelectionComponent, SelectionFacade>()
                .AsSingle();
        
            Container.BindFactory<IModel, EnemySelectionComponent, EnemySelectionFacade>()
                .AsSingle();
        }
    }
}