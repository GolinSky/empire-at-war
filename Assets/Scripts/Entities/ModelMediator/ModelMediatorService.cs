using System.Collections.Generic;
using EmpireAtWar.Ship;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Entities.ModelMediator
{
    public interface IModelMediatorService: IService
    {
        List<IUnitModelObserver> Units { get; }
        void AddUnit(IUnitModelObserver unitModelObserver);
        void RemoveUnit(IUnitModelObserver unitModelObserver);
    }
    
    //todo : rename it
    public class ModelMediatorService: Service, IModelMediatorService
    {
        private readonly List<IUnitModelObserver> _units = new List<IUnitModelObserver>();

        public List<IUnitModelObserver> Units => _units;

        public void AddUnit(IUnitModelObserver unitModelObserver)
        {
            _units.Add(unitModelObserver);
        }
        
        public void RemoveUnit(IUnitModelObserver unitModelObserver)
        {
            _units.Remove(unitModelObserver);
        }
    }
}