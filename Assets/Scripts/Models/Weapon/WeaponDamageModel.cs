using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Weapon
{
    [CreateAssetMenu(fileName = "WeaponDamageModel", menuName = "Model/Weapon/WeaponDamageModel")]
    public class WeaponDamageModel:Model
    {
        [SerializeField] private DictionaryWrapper<WeaponType, DamageModel> damageDictionary;


        public Dictionary<WeaponType, DamageModel> DamageDictionary => damageDictionary.Dictionary;
    }

    [Serializable]
    public class DamageModel
    {
        [SerializeField] private float damage;
        [field:SerializeField] public float Distance { get; private set; }
        [field:SerializeField] public AnimationCurve DistanceCurve { get; private set; }

        public float GetDamage(float distance)
        {
            float coefficient = distance / Distance;
            return DistanceCurve.Evaluate(coefficient)*damage;
        }
        
    }
}