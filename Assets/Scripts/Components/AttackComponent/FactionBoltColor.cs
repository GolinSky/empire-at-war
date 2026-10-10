using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [Serializable]
    public sealed class FactionBoltColor
    {
        [SerializeField] private FactionType faction;
        [SerializeField] private Color color = Color.white;

        public FactionType Faction => faction;
        public Color Color => color;
    }
}
