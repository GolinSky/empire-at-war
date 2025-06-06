using System;
using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Ship
{
    public class ShipView : View<IShipModelObserver, IShipCommand>
    {
        public event Action<ShipType> OnRelease;
        
        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
            if (gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(Model.ShipType);
                Instantiate(Model.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }
    }
}