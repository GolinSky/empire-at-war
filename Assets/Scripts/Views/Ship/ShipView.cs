using System;
using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.Ship
{
    public class ShipView : View<IShipModelObserver, IShipCommand>
    {
        public event Action<ShipType> OnRelease;
        
        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
        }

        public override void Release()
        {
            base.Release();
            if (gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(Model.ShipType);
                Instantiate(Model.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }
    }
}