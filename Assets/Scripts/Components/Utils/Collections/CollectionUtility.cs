using System;
using System.Collections.Generic;

namespace EmpireAtWar.Collections
{
    public static class CollectionUtility
    {
        private static Random _random = new Random();

        public static List<T> GetShuffledCollection<T>(this List<T> listToShuffle)
        {
            for (int i = listToShuffle.Count - 1; i > 0; i--)
            {
                var k = _random.Next(i + 1);
                var value = listToShuffle[k];
                listToShuffle[k] = listToShuffle[i];
                listToShuffle[i] = value;
            }

            listToShuffle.Reverse();
            return listToShuffle;
        }
    }
}