using System;
using UnityEngine;

namespace EmpireAtWar.Utils.Random
{
    [Serializable]
    public abstract class RandomFromArray<T>
    {
        [SerializeField] protected T[] array;

        protected System.Random random = new System.Random();
        
        public T GetRandom()
        {
            return array[random.Next(array.Length)];
        } 
    }

    [Serializable]
    public class RandomAudioClips : RandomFromArray<AudioClip> {}
}