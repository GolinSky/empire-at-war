using System;
using UnityEngine;

namespace EmpireAtWar.Utils.Random
{
    [Serializable]
    public class RandomVector3
    {
        [SerializeField] private RandomFloat x;
        [SerializeField] private RandomFloat y;
        [SerializeField] private RandomFloat z;

        public Vector3 Value => new Vector3(x.Random, y.Random, z.Random);
    }

    [Serializable]
    public class RandomFloat : RandomValue<float>
    {
        public override float Random => UnityEngine.Random.Range(Min, Max);
    }
}