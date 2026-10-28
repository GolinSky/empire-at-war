using System;
using System.Collections.Generic;
using UnityEngine;

// Authoring only: select an exposed, supported hull surface near an existing weapon mount.
public static class StationHardPointSurface
{
    private const float MIN_UP_NORMAL = .1f;
    private const float SURFACE_TOLERANCE = .03f;
    private const int TRIANGLE_SUBDIVISIONS = 4;

    public static Vector3 Place(Transform root, MeshRenderer[] hullRenderers, Vector3 preferredLocal,
        IReadOnlyList<Vector3> occupied, float spacing, MeshRenderer[] occluders = null)
    {
        var triangles = ReadTriangles(root, hullRenderers);
        var visibilityTriangles = occluders == null ? triangles : ReadTriangles(root, occluders);
        float bestDistance = float.PositiveInfinity;
        float spacingSquared = spacing * spacing;
        Vector3 best = default;
        foreach (var triangle in triangles)
        {
            Consider(ClosestPoint(preferredLocal, triangle.A, triangle.B, triangle.C));
            for (int first = 0; first <= TRIANGLE_SUBDIVISIONS; first++)
                for (int second = 0; second <= TRIANGLE_SUBDIVISIONS - first; second++)
                {
                    float u = first / (float)TRIANGLE_SUBDIVISIONS;
                    float v = second / (float)TRIANGLE_SUBDIVISIONS;
                    Consider(triangle.A * u + triangle.B * v + triangle.C * (1f - u - v));
                }
        }
        if (float.IsPositiveInfinity(bestDistance))
            throw new InvalidOperationException($"{root.name}: no exposed hull surface has {spacing:F2} units of hardpoint clearance near {preferredLocal}.");
        return best;

        void Consider(Vector3 candidate)
        {
            float distance = (candidate - preferredLocal).sqrMagnitude;
            if (distance >= bestDistance) return;
            foreach (Vector3 other in occupied)
                if ((candidate - other).sqrMagnitude < spacingSquared) return;
            // Reject inner plating and surfaces hidden beneath another part of the hull.
            foreach (var surface in visibilityTriangles)
                if (HeightAt(candidate, surface.A, surface.B, surface.C, out float height)
                    && height > candidate.y + SURFACE_TOLERANCE) return;
            best = candidate;
            bestDistance = distance;
        }
    }

    private static List<(Vector3 A, Vector3 B, Vector3 C)> ReadTriangles(Transform root, MeshRenderer[] renderers)
    {
        var triangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        foreach (MeshRenderer renderer in renderers)
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            for (int index = 0; index < vertices.Length; index++)
                vertices[index] = root.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
            int[] indices = mesh.triangles;
            for (int index = 0; index < indices.Length; index += 3)
            {
                Vector3 a = vertices[indices[index]], b = vertices[indices[index + 1]], c = vertices[indices[index + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude > .000001f && normal.normalized.y >= MIN_UP_NORMAL)
                    triangles.Add((a, b, c));
            }
        }
        return triangles;
    }

    private static bool HeightAt(Vector3 point, Vector3 a, Vector3 b, Vector3 c, out float height)
    {
        float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
        float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / denominator;
        float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / denominator;
        float w = 1f - u - v;
        height = u * a.y + v * b.y + w * c.y;
        return u >= -.00001f && v >= -.00001f && w >= -.00001f;
    }

    private static Vector3 ClosestPoint(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a, ac = c - a, ap = point - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;
        Vector3 bp = point - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));
        Vector3 cp = point - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));
        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        float inverse = 1f / (va + vb + vc);
        return a + ab * (vb * inverse) + ac * (vc * inverse);
    }
}
