using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar
{
    public class ShipInfoUi:MonoBehaviour
    {
        [SerializeField] private ShipUnitUi[] shipUnitArray;
        
        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
        public void Init(IShipUnitModel[] shipUnitModels)
        {
            for (var i = 0; i < shipUnitArray.Length; i++)
            {
                shipUnitArray[i].Init(shipUnitModels[i]);

            }
        }

        public void SetParent(Transform rootTransform)
        {
            transform.SetParent(rootTransform, false);
        }
    }
}