using System.Collections.Generic;

namespace EmpireAtWar.Models.MiniMap
{
    public sealed class CameraMarkData
    {
        private readonly List<(float X, float Z)> _vertices = new List<(float X, float Z)>(10);

        public IReadOnlyList<(float X, float Z)> Vertices => _vertices;

        public void Clear()
        {
            _vertices.Clear();
        }

        public void AddVertex(float x, float z)
        {
            _vertices.Add((x, z));
        }
    }
}