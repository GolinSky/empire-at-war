using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.SpaceStation
{
    public class SpaceStationViewFacade:PlaceholderFactory<PlayerType, FactionType, Vector3, SpaceStationView>
    {
        private readonly DiContainer container;
        private readonly IRepository repository;

        public SpaceStationViewFacade(DiContainer container, IRepository repository)
        {
            this.container = container;
            this.repository = repository;
        }
        // public override SpaceStationView Create(PlayerType playerType, FactionType factionType, Vector3 startPoint)
        // {
        //     base.Create(playerType);
        //     // container.BindEntity(startPoint).MoveIntoDirectSubContainers();
        //     // container.BindEntity(playerType).MoveIntoDirectSubContainers();
        //     // container.BindEntity(factionType).MoveIntoDirectSubContainers();
        //     //
        //     // container
        //     //     .Bind<SpaceStationView>()
        //     //     .FromSubContainerResolve()
        //     //     .ByNewContextPrefab(repository.Load<GameObject>($"{factionType}{nameof(SpaceStationView)}"))
        //     //     .AsTransient();
        //     // return container.Resolve<SpaceStationView>();// base.Create(playerType, factionType, startPoint);
        // }
    }

    public class SpaceStationViewFactory:IFactory<PlayerType, FactionType, Vector3, SpaceStationView>
    {
        private readonly DiContainer container;
        private readonly IRepository repository;

        public SpaceStationViewFactory(DiContainer container, IRepository repository)
        {
            this.container = container;
            this.repository = repository;
        }
        public SpaceStationView Create(PlayerType playerType, FactionType factionType, Vector3 startPoint)
        {
            container.BindEntity(startPoint).MoveIntoDirectSubContainers();
            container.BindEntity(playerType).MoveIntoDirectSubContainers();
            container.BindEntity(factionType).MoveIntoDirectSubContainers();
            SpaceStationView spaceStationView = container
                .InstantiatePrefabForComponent<SpaceStationView>(
                    repository.LoadComponent<SpaceStationView>($"{factionType}{nameof(SpaceStationView)}"));

            container
                .Bind<SpaceStationView>()
                .FromSubContainerResolve()
                .ByNewContextPrefab(spaceStationView)
                .AsTransient();
            return spaceStationView;
        }
    }
}