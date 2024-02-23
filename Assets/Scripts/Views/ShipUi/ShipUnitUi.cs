using DG.Tweening;
using EmpireAtWar.Models.Health;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar
{
    [RequireComponent(typeof(Image))]
    public class ShipUnitUi: MonoBehaviour
    {
        private const float MinTolerance = 0.01f;
        [SerializeField] private Image image;
        
        [field:SerializeField] public int Id { get; private set; }

        private IShipUnitModel shipUnitModel;

        private void OnValidate()
        {
            image = GetComponent<Image>();
        }

        public void Init(IShipUnitModel shipUnitModel)
        {
            if (this.shipUnitModel != null)
            {
                RemoveListeners();
            }
            this.shipUnitModel = shipUnitModel;
            UpdateData();
            Activate(true);
            shipUnitModel.OnShipUnitChanged += UpdateData;
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void RemoveListeners()
        {
            if (shipUnitModel != null)
            {
                shipUnitModel.OnShipUnitChanged -= UpdateData;
            }
        }

        private void UpdateData()
        {
            float healthPercentage = shipUnitModel.HealthPercentage;
            if (healthPercentage <= MinTolerance)
            {
                image.color = Color.gray;
                return;
            }

            image.color = Color.Lerp(Color.red, Color.green, healthPercentage);
            //
            // Color color = image.color;
            // color.g = 255 * healthPercentage;
            // color.r = 255 * (1 - healthPercentage);
            // image.color = color;
        }

        public void Disable()
        {
            Activate(false);
        }

        private void Activate(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}