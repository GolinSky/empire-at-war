using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>World coordinates and nearest-sample indexing for the shared navigation hazard raster.</summary>
    internal readonly struct NavigationGridCells
    {
        public float2 Origin { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Height { get; }
        public int Count => Width * Height;

        public NavigationGridCells(float2 origin, float cellSize, int width, int height)
        {
            Origin = origin;
            CellSize = cellSize;
            Width = width;
            Height = height;
        }

        public float2 GetNodePosition(int cell)
        {
            return Origin + new float2(cell % Width, cell / Width) * CellSize;
        }

        public int GetCell(float2 position)
        {
            int2 cell = (int2)math.round((position - Origin) / CellSize);
            cell = math.clamp(cell, int2.zero, new int2(Width - 1, Height - 1));
            return cell.y * Width + cell.x;
        }
    }
}
