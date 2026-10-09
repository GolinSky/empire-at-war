using System;

namespace EmpireAtWar.Utils.Random
{
    [Serializable]
    public class RandomFloat : RandomValue<float>
    {
        public override float Random => UnityEngine.Random.Range(Min, Max);
    }
}
