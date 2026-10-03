using System;
using UnityEngine;

namespace EmpireAtWar.Models.Players
{
    [Serializable]
    public struct TeamColorEntry
    {
        [SerializeField] private string name;

        [SerializeField] private Color color;

        public string Name => name;
        public Color Color => color;

        public TeamColorEntry(string name, Color color)
        {
            this.name = name;
            this.color = color;
        }
    }
}
