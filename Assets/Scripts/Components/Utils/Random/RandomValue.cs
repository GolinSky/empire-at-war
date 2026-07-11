using System;
using UnityEngine;

namespace EmpireAtWar.Utils.Random
{
    [Serializable]
    public abstract class RandomValue<TValue>
    {
        [field:SerializeField] public TValue Min { get; protected set; }
        [field:SerializeField] public TValue Max { get; protected set; }

        public abstract TValue Random { get; }
    }
}