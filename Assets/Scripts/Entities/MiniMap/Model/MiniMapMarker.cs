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

        public MiniMapMarker(MarkType markType, PlayerId owner)
        {
            MarkType = markType;
            Owner = owner;
        }

        public void SetPosition(float x, float z)
        {
            X = x;
            Z = z;
        }

        public void SetOwner(PlayerId owner)
        {
            Owner = owner;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
        }

        public void SetWorldDiameter(float worldDiameter)
        {
            WorldDiameter = worldDiameter;
        }
    }
}
