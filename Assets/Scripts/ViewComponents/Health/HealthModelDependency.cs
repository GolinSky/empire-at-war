using System.Collections.Generic;
using System.Linq;
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

#if UNITY_EDITOR
        [ContextMenu("Assign hard points")]
        private void ExampleMethod()
        {
            ShipUnits = transform.root.GetComponentsInChildren<HardPointView>().ToList();
        }
    }
#endif
       
}