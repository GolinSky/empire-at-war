using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.ScriptUtils.EditorSerialization
{
    [Serializable]
    public class DictionaryWrapper<TKey, TValue>
    {
        [SerializeField] private List<KeyValue<TKey,TValue>> keyValue;

        private Dictionary<TKey, TValue> dictionary;

        public Dictionary<TKey, TValue> Dictionary => dictionary ??= UnityDictionaryFactory.Build(keyValue);

        public List<KeyValue<TKey, TValue>> KeyValueList => keyValue;
    }
}