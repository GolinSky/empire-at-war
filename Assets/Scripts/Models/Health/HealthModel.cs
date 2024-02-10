using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthModelObserver:IModelObserver
    {
        event Action OnDestroy;
        event Action OnValueChanged;

        float Armor { get; }
        float Shields { get; }

    }
    [Serializable]
    public class HealthModel:InnerModel, IHealthModelObserver
    {
        public event Action OnValueChanged;
        public event Action OnDestroy;
        [field:SerializeField] public float Armor { get; private set; }
        [field:SerializeField] public float Shields { get; private set; }
        public bool IsDestroyed { get; private set; }


        public void ApplyDamage(float damage)
        {
            if (Shields >  damage)
            {
                Shields -= damage;
            }
            else
            {
                if (Shields > 0)
                {
                    damage -= Shields;
                    Shields = 0;
                }
                Armor -= damage;
                if (Armor <= 0)
                {
                    IsDestroyed = true;
                    OnDestroy?.Invoke();
                }
            }
            OnValueChanged?.Invoke();
            
        }
        //hull value - armor only safe hull by coefficient - same as shields safe armor+hull by cofficient
    }
}