using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Initialiaze;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ship
{
    public interface IShipEntity
    {
        IShipModelObserver ModelObserver { get; }
    }

    public class Ship : View<IShipModelObserver>, IController, IShipEntity, ILateIInitializable
    {
        private HardPointModel _enginesUnitModel;

        [Inject] private IShipService ShipService { get; }
        [Inject] private IShipData Data { get; }
        [Inject] private ShipType ShipType { get; }
        [Inject] private ShipModel RootModel { get; }

        public event Action<ShipType> OnRelease;

        public string Id => GetType().Name;
        IShipModelObserver IShipEntity.ModelObserver => Model;

        public IModel GetModel()
        {
            return RootModel;
        }

        protected override void OnInitialize()
        {
            ShipService.Add(this);
        }

        public void LateInitialize()
        {
            HealthModel healthModel = RootModel.GetModel<HealthModel>();
            foreach (HardPointModel hardPointModel in healthModel.HardPointModels)
            {
                if (hardPointModel.HardPointType == HardPointType.Engines)
                {
                    _enginesUnitModel = hardPointModel;
                    break;
                }
            }

            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged += HandleEnginesData;
            }
        }

        protected override void OnDispose()
        {
            ShipService.Remove(this);

            if (_enginesUnitModel != null)
            {
                _enginesUnitModel.OnHardPointHealthChanged -= HandleEnginesData;
            }

            if (gameObject.activeInHierarchy)
            {
                OnRelease?.Invoke(ShipType);
                Instantiate(Data.DeathExplosionVfx, transform.position, Quaternion.identity);
            }
        }

        private void HandleEnginesData()
        {
            if (_enginesUnitModel.IsDestroyed)
            {
                RootModel.GetModel<ShipMoveModel>().ApplyMoveCoefficient(Data.MinMoveCoefficient);
            }
        }
    }
}
