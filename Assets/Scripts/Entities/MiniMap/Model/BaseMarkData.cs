using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    /// <summary>A space station on the minimap, drawn in its owner's team color.</summary>
    public sealed class BaseMarkData : MarkData
    {
        public PlayerId Owner { get; }

        public BaseMarkData(Sprite icon, Vector3 position, PlayerId owner) : base(position: position, icon: icon)
        {
            Owner = owner;
        }
    }
}
