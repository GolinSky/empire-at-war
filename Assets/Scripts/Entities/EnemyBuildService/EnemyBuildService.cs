using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Components.Service;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.EnemyReinforcement
{
    public interface IBuildService : IService
    {
    }


    public class EnemyBuildService : Service, IBuildService, IBuildShipChain
    {
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly MiningFacilityFacade miningFacilityFacade;
        private readonly DefendPlatformFacade defendPlatformFacade;
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly LazyInject<IMapModelObserver> mapModel;
        private IChainHandler<UnitRequest> nextChain;

        private PlayerType PlayerType => PlayerType.Opponent;
        

        public EnemyBuildService(
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade,
            ITimerPoolWrapperService timerPoolWrapperService, 
            LazyInject<IMapModelObserver> mapModel)
        {
            this.shipFacadeFactory = shipFacadeFactory;
            this.miningFacilityFacade = miningFacilityFacade;
            this.defendPlatformFacade = defendPlatformFacade;
            this.timerPoolWrapperService = timerPoolWrapperService;
            this.mapModel = mapModel;
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(UnitRequest request)
        {
            switch (request)
            {
                case ShipUnitRequest shipUnitRequest:
                    timerPoolWrapperService.Invoke(() =>
                        {
                            shipFacadeFactory.Create(PlayerType, shipUnitRequest.Key, GenerateShipCoordinates());
                        },
                        shipUnitRequest.FactionData.BuildTime);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    break;
            }
          
            if (nextChain != null)
            {
                nextChain.Handle(request);
            }
        }

        private Vector3 GenerateShipCoordinates()
        {
            Vector3 startPosition = mapModel.Value.GetStationPosition(PlayerType);
            startPosition += new Vector3(Random.Range(-50, 50), 0, Random.Range(-50, 50));
            return startPosition;
        }
    }
}