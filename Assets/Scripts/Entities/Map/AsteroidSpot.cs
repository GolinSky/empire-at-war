using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>A purely visual rock of an asteroid field; navigation uses the field volumes instead.</summary>
    public readonly struct AsteroidSpot
    {
        public AsteroidSpot(Vector3 position, Vector3 rotation, float scale, AsteroidSize size)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Size = size;
        }

        public Vector3 Position { get; }
        public Vector3 Rotation { get; }
        public float Scale { get; }
        public AsteroidSize Size { get; }
    }
}
