using EmpireAtWar.Entities.Map.Hazards;
using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public readonly struct MiniMapHazard
    {
        public float CenterX { get; }
        public float CenterZ { get; }
        public HazardShape Shape { get; }
        public Color Color { get; }
        public Sprite Icon { get; }

        public MiniMapHazard(HazardShape shape)
        {
            CenterX = shape.Spot.Center.x;
            CenterZ = shape.Spot.Center.z;
            Shape = shape;
            Color = shape.Data.MinimapColor;
            Icon = shape.Data.MinimapIcon;
        }
    }
}
