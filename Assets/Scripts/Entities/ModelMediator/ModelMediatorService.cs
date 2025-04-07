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
        private readonly List<IUnitModelObserver> units = new List<IUnitModelObserver>();

        public List<IUnitModelObserver> Units => units;

        public void AddUnit(IUnitModelObserver unitModelObserver)
        {
            units.Add(unitModelObserver);
        }
        
        public void RemoveUnit(IUnitModelObserver unitModelObserver)
        {
            units.Remove(unitModelObserver);
        }
    }
}