using System;
using UnityEngine;

namespace EmpireAtWar.Utils.Random
{
    [Serializable]
    public class RandomAudioClips : RandomFromArray<AudioClip>
    {
        public bool HasClips => array != null && array.Length > 0;

        public AudioClip GetRandomExcept(AudioClip previous)
        {
            if (previous == null || array.Length == 1) return GetRandom();
            AudioClip[] alternatives = Array.FindAll(array, clip => clip != previous);
            return alternatives.Length > 0 ? alternatives[_random.Next(alternatives.Length)] : GetRandom();
        }
    }
}
