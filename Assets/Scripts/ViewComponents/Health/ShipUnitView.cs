using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Views.ShipUi;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public class ShipUnitView : MonoBehaviour, IShipUnitView, INotifier<float>
    {
        [field: SerializeField] public ShipUnitType ShipUnitType { get; private set; }
        [field: SerializeField] public int Id { get; private set; }

        private List<IObserver<float>> observers = new List<IObserver<float>>();
        private IShipUnitModel shipUnitModel;
        private float healthPercentage;

        public Vector3 Position => transform.position;
        public bool IsDestroyed => healthPercentage <= 0f;

        public void Init(IShipUnitModel shipUnitModel)
        {
            this.shipUnitModel = shipUnitModel;
            UpdateData();
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
            healthPercentage = shipUnitModel.HealthPercentage;
            foreach (IObserver<float> observer in observers)
            {
                observer.UpdateState(healthPercentage);
            }
        }

        void INotifier<float>.AddObserver(IObserver<float> observer)
        {
            observers.Add(observer);
        }

        void INotifier<float>.RemoveObserver(IObserver<float> observer)
        {
            observers.Remove(observer);
        }
    }
}