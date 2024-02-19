using System;
using EmpireAtWar.Models.Health;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar
{
    [RequireComponent(typeof(Image))]
    public class ShipUnitUi: MonoBehaviour
    {
        [SerializeField] private Image image;
        private IShipUnitModel shipUnitModel;

        [field:SerializeField] public int Id { get; private set; }

        private void OnValidate()
        {
            image = GetComponent<Image>();
        }

        public void Init(IShipUnitModel shipUnitModel)
        {
            this.shipUnitModel = shipUnitModel;
            shipUnitModel.OnShipUnitChanged += UpdateData;
        }

        private void OnDestroy()
        {
            if (shipUnitModel != null)
            {
                shipUnitModel.OnShipUnitChanged -= UpdateData;
            }
        }

        private void UpdateData()
        {
            float healthPercentage = shipUnitModel.HealthPercentage;
            // hardcode
            image.color = healthPercentage > 0.9f
                ? Color.green
                : healthPercentage > 0.5f
                    ? Color.yellow
                    : healthPercentage > 0f
                        ? Color.red
                        : Color.black;
        }
    }
}