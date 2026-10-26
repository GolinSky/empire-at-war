using System;
using UnityEngine;

namespace EmpireAtWar.Utils.Random
{
    [Serializable]
    public class RandomAudioClips : RandomFromArray<AudioClip>
    {
        public bool HasClips => array != null && array.Length > 0;
    }
}
