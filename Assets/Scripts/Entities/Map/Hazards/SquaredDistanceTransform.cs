namespace EmpireAtWar.Entities.Map.Hazards
{
    /// <summary>Exact nearest-target search on a grid (Felzenszwalb–Huttenlocher distance transform).</summary>
    public static class SquaredDistanceTransform
    {
        private const float FAR = 1e20f;

        /// <summary>Writes, for every sample, the index of the nearest target sample, or -1 when there is none.</summary>
        public static void FindNearest(bool[] targets, int columns, int rows, int[] nearest)
        {
            int longest = columns > rows ? columns : rows;
            float[] line = new float[longest];
            float[] transformed = new float[longest];
            int[] arguments = new int[longest];
            int[] parabolas = new int[longest];
            float[] boundaries = new float[longest + 1];
            float[] columnDistance = new float[targets.Length];
            int[] columnRow = new int[targets.Length];
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++) line[row] = targets[row * columns + column] ? 0f : FAR;
                Transform(line, rows, transformed, arguments, parabolas, boundaries);
                for (int row = 0; row < rows; row++)
                {
                    columnDistance[row * columns + column] = transformed[row];
                    columnRow[row * columns + column] = arguments[row];
                }
            }
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++) line[column] = columnDistance[row * columns + column];
                Transform(line, columns, transformed, arguments, parabolas, boundaries);
                for (int column = 0; column < columns; column++)
                {
                    int source = arguments[column];
                    nearest[row * columns + column] = transformed[column] >= FAR * 0.5f
                        ? -1 : columnRow[row * columns + source] * columns + source;
                }
            }
        }

        private static void Transform(float[] values, int count, float[] result, int[] arguments,
            int[] parabolas, float[] boundaries)
        {
            int last = 0;
            parabolas[0] = 0;
            boundaries[0] = float.NegativeInfinity;
            boundaries[1] = float.PositiveInfinity;
            for (int q = 1; q < count; q++)
            {
                float intersection = Intersect(values, q, parabolas[last]);
                while (intersection <= boundaries[last])
                {
                    last--;
                    intersection = Intersect(values, q, parabolas[last]);
                }
                last++;
                parabolas[last] = q;
                boundaries[last] = intersection;
                boundaries[last + 1] = float.PositiveInfinity;
            }
            last = 0;
            for (int q = 0; q < count; q++)
            {
                while (boundaries[last + 1] < q) last++;
                float offset = q - parabolas[last];
                result[q] = offset * offset + values[parabolas[last]];
                arguments[q] = parabolas[last];
            }
        }

        private static float Intersect(float[] values, int q, int p)
        {
            return (values[q] + q * q - (values[p] + p * p)) / (2f * (q - p));
        }
    }
}
