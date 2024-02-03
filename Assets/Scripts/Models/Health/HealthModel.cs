using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthModelObserver:IModelObserver
    {
        event Action OnValueChanged;

        float Armor { get; }
        float Shields { get; }

    }
    [Serializable]
    public class HealthModel:InnerModel, IHealthModelObserver
    {
        public event Action OnValueChanged;
        [field:SerializeField] public float Armor { get; private set; }
        [field:SerializeField] public float Shields { get; private set; }


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
            }
            OnValueChanged?.Invoke();
        }
        //hull value - armor only safe hull by coefficient - same as shields safe armor+hull by cofficient
    }
}