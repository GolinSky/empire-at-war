using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.Ship
{
    public class ShipView : View<IShipModelObserver, IShipCommand>
    {

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
                Instantiate(Model.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }
    }
}