using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>Row-major matrix layout shared by the grouping job and the per-frame projection.</summary>
    internal static class SquadronClusterLayout
    {
        public const int MAX_COLS = 4;

        public static int Columns(int count)
        {
            int columns = 1;
            while (columns < MAX_COLS && columns * columns < count) columns++;
            return columns;
        }

        public static int Rows(int count) => (count + Columns(count) - 1) / Columns(count);

        public static Vector2 Size(int count, float pitch) =>
            new Vector2(Columns(count), Rows(count)) * pitch;

        /// <summary>Cell center relative to the grid center. Row 0 is the top row; the last row is centered.</summary>
        public static Vector2 CellOffset(int rank, int count, float pitch)
        {
            int columns = Columns(count);
            int rows = Rows(count);
            int row = Mathf.Min(rank / columns, rows - 1);
            int rowCount = row == rows - 1 ? count - row * columns : columns;
            int column = rank - row * columns;
            return new Vector2((column - (rowCount - 1) * 0.5f) * pitch, ((rows - 1) * 0.5f - row) * pitch);
        }
    }
}
