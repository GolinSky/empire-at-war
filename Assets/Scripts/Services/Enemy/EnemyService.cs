using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using UnityEngine;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService
    {
        private static readonly Vector3 Offset = new Vector3(250, 0, 250);
        private static readonly Vector3 Offset1 = new Vector3(-20, 0, 10);
        
        private readonly FactionType factionType;
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly MapModel mapModel;
        private readonly Dictionary<ShipType, FactionData> factionData;
        private Vector3 stationPosition;

        public EnemyService(
            ShipFacadeFactory shipFacadeFactory,
            SkirmishGameData skirmishGameData,
            SpaceStationViewFacade spaceStationViewFacade,
            FactionsModel factionsModel,
            MapModel mapModel)
        {
            factionType = skirmishGameData.EnemyFactionType;
            this.shipFacadeFactory = shipFacadeFactory;
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.mapModel = mapModel;
            factionData = factionsModel.GetFactionData(skirmishGameData.EnemyFactionType);

        }
        public void Initialize()
        {
            stationPosition = mapModel.GetStationPosition(PlayerType.Opponent);
            spaceStationViewFacade.Create(PlayerType.Opponent, factionType,  stationPosition);
            // shipFacadeFactory.Create(PlayerType.Opponent, ShipType.Providence, stationPosition+ (Offset1*1f));
            // shipFacadeFactory.Create(PlayerType.Opponent, ShipType.Providence, stationPosition+ (Offset1*2f));
            // shipFacadeFactory.Create(PlayerType.Opponent, ShipType.Providence, stationPosition+ (Offset1*3f));
            // return;
            Sequence sequence = DOTween.Sequence();
            Vector3 position = stationPosition + new Vector3(20, 0 , -20);
            foreach (var keyValuePair in factionData)
            {
                sequence.AppendInterval(keyValuePair.Value.BuildTime);
                sequence.AppendCallback(() =>
                {
                    // shipFacadeFactory.Create(PlayerType.Opponent, keyValuePair.Key, position);
                    shipFacadeFactory.Create(PlayerType.Opponent, ShipType.Providence, position);
                    position = new Vector3(Random.Range(-Offset.x, Offset.x), 0,Random.Range(-Offset.z, Offset.z));
                });
            }

            sequence.SetLoops(10, LoopType.Restart);
        }
    }
}