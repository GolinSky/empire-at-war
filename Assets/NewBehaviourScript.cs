using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class NewBehaviourScript : MonoBehaviour
    {
        private ShipView.ShipFactory ShipFactory;
        
        [Inject]
        public void Construct(ShipView.ShipFactory customShipFactory)
        {
            ShipFactory = customShipFactory;
        }
        
        public void SpawnVenatorShip()
        {
            ShipFactory.Create(RepublicShipType.Venator);
        }
        
        public void SpawnArquitensShip()
        {
            ShipFactory.Create(RepublicShipType.Arquitens);
        }
    }
}
