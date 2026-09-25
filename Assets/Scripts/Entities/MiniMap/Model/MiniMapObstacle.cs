namespace EmpireAtWar.Models.MiniMap
{
    // Elliptical ground footprint of a static map obstacle, in world XZ units.
    public sealed class MiniMapObstacle
    {
        public MiniMapObstacle(float centerX, float centerZ, float radiusX, float radiusZ)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            RadiusX = radiusX;
            RadiusZ = radiusZ;
        }

        public float CenterX { get; }
        public float CenterZ { get; }
        public float RadiusX { get; }
        public float RadiusZ { get; }

        public bool Contains(float x, float z)
        {
            float dx = (x - CenterX) / RadiusX;
            float dz = (z - CenterZ) / RadiusZ;
            return dx * dx + dz * dz <= 1f;
        }
    }
}
