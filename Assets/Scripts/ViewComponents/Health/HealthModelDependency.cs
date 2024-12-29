using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents
{
    public class HealthModelDependency: ModelDependency<IHealthModelObserver>
    {
        [field:SerializeField] public List<HardPointView> ShipUnits { get; set; }

        protected override void OnInit()
        {
            base.OnInit();
            Model.InjectDependency(this);// move it to the base class
        }
    }
}