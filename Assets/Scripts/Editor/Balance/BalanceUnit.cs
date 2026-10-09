using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Editor.Balance
{
    public sealed class BalanceUnit
    {
        public string Id;
        public string Name;
        public string Faction;
        public string Kind;
        public string Class;
        public Object Data;
        public GameObject Prefab;
        public readonly Dictionary<string, string> Fields = new Dictionary<string, string>();
        public readonly List<Object> Mounts = new List<Object>();
        public readonly List<Object> Components = new List<Object>();
        public string Caption => $"{Name} · {Faction} · {Class}";
    }
}
