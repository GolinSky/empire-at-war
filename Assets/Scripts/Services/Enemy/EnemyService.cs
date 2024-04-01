using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService
    {
        private static readonly Vector3 Offset = new Vector3(250, 0, 250);
        
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly FactionsModel factionsModel;
        private readonly LazyInject<IMapModelObserver> mapModel;
        private Dictionary<ShipType, FactionData> factionData;
        private Vector3 stationPosition;
        
        [Inject(Id = PlayerType.Opponent)]
        private FactionType FactionType { get; }

        public EnemyService(
            ShipFacadeFactory shipFacadeFactory,
            SpaceStationViewFacade spaceStationViewFacade,
            FactionsModel factionsModel,
            LazyInject<IMapModelObserver> mapModel)
        {
            this.shipFacadeFactory = shipFacadeFactory;
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.factionsModel = factionsModel;
            this.mapModel = mapModel;

        }
        public void Initialize()
        {
            factionData = factionsModel.GetShipFactionData(FactionType);

            stationPosition = mapModel.Value.GetStationPosition(PlayerType.Opponent);
            spaceStationViewFacade.Create(PlayerType.Opponent, FactionType,  stationPosition);

            Sequence sequence = DOTween.Sequence();
            Vector3 position = stationPosition + new Vector3(20, 0 , -20);
            
            foreach (var keyValuePair in factionData)
            {
                sequence.AppendInterval(keyValuePair.Value.BuildTime);
                sequence.AppendCallback(() =>
                {
                    shipFacadeFactory.Create(PlayerType.Opponent, keyValuePair.Key, position);
                    position = new Vector3(Random.Range(-Offset.x, Offset.x), 0,Random.Range(-Offset.z, Offset.z));
                });
            }

            sequence.SetLoops(10, LoopType.Restart);
        }
    }
}