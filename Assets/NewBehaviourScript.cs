    using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class NewBehaviourScript : MonoBehaviour
    {
        private ShipFacadeFactory shipFacadeFactory;
        
        [Inject]
        public void Construct(ShipFacadeFactory customShipFacadeFactory)
        {
            shipFacadeFactory = customShipFacadeFactory;
        }
        
        public void SpawnVenatorShip()
        {
            shipFacadeFactory.Create(ShipType.Venator);
        }
        
        public void SpawnArquitensShip()
        {
            shipFacadeFactory.Create(ShipType.Arquitens);
        }
    }
}
