using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using LightWeightFramework.Model;
using LightWeightFramework.Components.Service;

namespace EmpireAtWar.Services.ComponentHub
{
    [Obsolete]
    public interface IComponentHub
    {
        IHealthComponent GetComponent(IHealthModelObserver modelObserver);
        void Add(IHealthComponent healthComponent);
        void Remove(IHealthComponent healthComponent);
        
    }
    
    [Obsolete]
    public class ComponentHub:Service, IComponentHub
    {
        private List<IHealthComponent> _healthComponents = new List<IHealthComponent>();
        
        public IHealthComponent GetComponent(IHealthModelObserver modelObserver)
        {
            foreach (IHealthComponent healthComponent in _healthComponents)
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
            _healthComponents.Add(healthComponent);
        }

        public void Remove(IHealthComponent healthComponent)
        {
            _healthComponents.Remove(healthComponent);
        }
    }
}