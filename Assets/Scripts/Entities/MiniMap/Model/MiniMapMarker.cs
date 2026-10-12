using System;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Models.MiniMap
{
    public sealed class MiniMapMarker
    {
        public float X { get; private set; }
        public float Z { get; private set; }
        public MarkType MarkType { get; }
        /// <summary>Who the marker shows; the minimap draws it in this owner's team color.</summary>
        public PlayerId Owner { get; private set; }
        public bool Visible { get; private set; }
        public float WorldDiameter { get; private set; }

        /// <summary>Raised when anything the minimap draws for this marker changes.</summary>
        public event Action Changed;

        public MiniMapMarker(MarkType markType, PlayerId owner)
        {
            MarkType = markType;
            Owner = owner;
        }

        public void SetPosition(float x, float z)
        {
            if (X == x && Z == z) return;
            X = x;
            Z = z;
            Changed?.Invoke();
        }

        public void SetOwner(PlayerId owner)
        {
            if (Owner.Equals(owner)) return;
            Owner = owner;
            Changed?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (Visible == visible) return;
            Visible = visible;
            Changed?.Invoke();
        }

        public void SetWorldDiameter(float worldDiameter)
        {
            if (WorldDiameter == worldDiameter) return;
            WorldDiameter = worldDiameter;
            Changed?.Invoke();
        }
    }
}
