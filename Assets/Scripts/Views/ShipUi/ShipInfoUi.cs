using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar
{
    public class ShipInfoUi:MonoBehaviour
    {
        [SerializeField] private ShipUnitUi[] shipUnitArray;

        public void Init(IShipUnitModel[] shipUnitModels)
        {
            for (var i = 0; i < shipUnitModels.Length; i++)
            {
                shipUnitArray[i].Init(shipUnitModels[i]);
            }
        }

        public void SetParent(Transform rootTransform)
        {
            transform.SetParent(rootTransform, false);
        }

        public void Disable()
        {
            foreach (ShipUnitUi shipUnitUi in shipUnitArray)
            {
                shipUnitUi.Disable();
            }
        }
    }
}