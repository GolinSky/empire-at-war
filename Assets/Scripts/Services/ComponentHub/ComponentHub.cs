using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using LightWeightFramework.Model;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.ComponentHub
{
    public interface IComponentHub
    {
        IHealthComponent GetComponent(IModelObserver modelObserver);
        void Add(IHealthComponent healthComponent);
        void Remove(IHealthComponent healthComponent);
        
    }
    public class ComponentHub:Service, IComponentHub
    {
        private List<IHealthComponent> healthComponents = new List<IHealthComponent>();
        
        public IHealthComponent GetComponent(IModelObserver modelObserver)
        {
            foreach (IHealthComponent healthComponent in healthComponents)
            {
                if (healthComponent.Equal(modelObserver))
                {
                    return healthComponent;
                }
            }

            return null;
        }

        public void Add(IHealthComponent healthComponent)
        {
            healthComponents.Add(healthComponent);
        }

        public void Remove(IHealthComponent healthComponent)
        {
            healthComponents.Remove(healthComponent);
        }
    }
}