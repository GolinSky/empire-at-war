using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Models.MiniMap
{
    public sealed class MiniMapMarker
    {
        public MiniMapMarker(MarkType markType, PlayerType relation)
        {
            MarkType = markType;
            Relation = relation;
        }

        public float X { get; private set; }
        public float Z { get; private set; }
        public MarkType MarkType { get; }
        public PlayerType Relation { get; private set; }
        public bool Visible { get; private set; }
        public float WorldDiameter { get; private set; }

        public void SetPosition(float x, float z)
        {
            X = x;
            Z = z;
        }

        public void SetRelation(PlayerType relation)
        {
            Relation = relation;
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
