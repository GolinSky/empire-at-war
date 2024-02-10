using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Models.Factions;
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
        private static readonly Vector3 Offset = new Vector3(-30, 0, 30);
        
        private readonly FactionType factionType;
        private readonly SkirmishGameData skirmishGameData;
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly FactionsModel factionsModel;
        private Vector3 stationPosition;
        private readonly Dictionary<ShipType, FactionData> factionData;

        public EnemyService(
            SkirmishGameData skirmishGameData,
            ShipFacadeFactory shipFacadeFactory,
            SpaceStationViewFacade spaceStationViewFacade,
            FactionsModel factionsModel)
        {
            factionType = skirmishGameData.EnemyFactionType;
            this.skirmishGameData = skirmishGameData;
            this.shipFacadeFactory = shipFacadeFactory;
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.factionsModel = factionsModel;
            factionData = factionsModel.GetFactionData(skirmishGameData.EnemyFactionType);

        }
        public void Initialize()
        {
            stationPosition = skirmishGameData.GetStationPosition(PlayerType.Opponent);
            spaceStationViewFacade.Create(PlayerType.Opponent, factionType,  stationPosition);
            Sequence sequence = DOTween.Sequence();
            Vector3 position = stationPosition + Offset;
            foreach (var keyValuePair in factionData)
            {
                sequence.AppendInterval(keyValuePair.Value.BuildTime);
                sequence.AppendCallback(() =>
                {
                    shipFacadeFactory.Create(PlayerType.Opponent, keyValuePair.Key, position);
                    position += Offset;
                });
            }
        }
    }
}