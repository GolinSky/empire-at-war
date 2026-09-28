using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    /// <summary>A space station on the minimap, drawn in its owner's team color.</summary>
    public sealed class BaseMarkData : MarkData
    {
        public BaseMarkData(Vector3 position, Sprite icon, PlayerId owner) : base(position, icon)
        {
            Owner = owner;
        }

        public PlayerId Owner { get; }
    }
}
