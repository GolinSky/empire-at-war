using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.SpaceStation
{
    public class SpaceStationViewFacade:PlaceholderFactory<PlayerType, FactionType, Vector3, SpaceStationView>
    {
        private readonly DiContainer _container;
        private readonly IRepository _repository;

        public SpaceStationViewFacade(DiContainer container, IRepository repository)
        {
            _container = container;
            _repository = repository;
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
        private readonly DiContainer _container;
        private readonly IRepository _repository;

        public SpaceStationViewFactory(DiContainer container, IRepository repository)
        {
            _container = container;
            _repository = repository;
        }
        public SpaceStationView Create(PlayerType playerType, FactionType factionType, Vector3 startPoint)
        {
            _container.BindEntity(startPoint).MoveIntoDirectSubContainers();
            _container.BindEntity(playerType).MoveIntoDirectSubContainers();
            _container.BindEntity(factionType).MoveIntoDirectSubContainers();
            SpaceStationView spaceStationView = _container
                .InstantiatePrefabForComponent<SpaceStationView>(
                    _repository.LoadComponent<SpaceStationView>($"{factionType}{nameof(SpaceStationView)}"));

            _container
                .Bind<SpaceStationView>()
                .FromSubContainerResolve()
                .ByNewContextPrefab(spaceStationView)
                .AsTransient();
            return spaceStationView;
        }
    }
}