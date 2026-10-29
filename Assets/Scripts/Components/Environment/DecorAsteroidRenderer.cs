using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Environment
{
    /// <summary>Draws exterior rocks without GameObjects, colliders, fog entries or navigation obstacles.</summary>
    public sealed class DecorAsteroidRenderer : MonoBehaviour
    {
        private const int BATCH_SIZE = 511;
        private const float CELL_SIZE = 1000f;
        [SerializeField] private DecorAsteroidMesh[] meshes;
        [SerializeField, Min(1f)] private float debrisDistance = 2200f;
        [SerializeField, Min(1f)] private float mediumDistance = 5000f;
        [SerializeField, Min(1f)] private float largeDistance = 12000f;
        [SerializeField, Min(1f)] private float lodDistance = 1000f;
        private readonly Matrix4x4[] _batch = new Matrix4x4[BATCH_SIZE];
        private readonly Matrix4x4[] _lowBatch = new Matrix4x4[BATCH_SIZE];
        private readonly Plane[] _planes = new Plane[6];
        private DecorAsteroidCell[][] _cells = Array.Empty<DecorAsteroidCell[]>();
        private Material[][] _materials = Array.Empty<Material[]>();
        private Camera _camera;

        public void Configure(IReadOnlyList<AsteroidSpot> rocks, Camera camera)
        {
            ReleaseMaterials();
            _camera = camera;
            Dictionary<Vector3Int, DecorAsteroidCell>[] cells = new Dictionary<Vector3Int, DecorAsteroidCell>[meshes.Length];
            _materials = new Material[meshes.Length][];
            for (int i = 0; i < meshes.Length; i++)
            {
                cells[i] = new Dictionary<Vector3Int, DecorAsteroidCell>();
                _materials[i] = new Material[meshes[i].Materials.Length];
                for (int j = 0; j < _materials[i].Length; j++)
                    _materials[i][j] = new Material(meshes[i].Materials[j]) { enableInstancing = true };
            }

            for (int i = 0; i < rocks.Count; i++)
            {
                AsteroidSpot rock = rocks[i];
                int variants = 0;
                for (int j = 0; j < meshes.Length; j++)
                    if (meshes[j].Size == rock.Size) variants++;
                if (variants == 0)
                    throw new InvalidOperationException($"Decor asteroid mesh missing for {rock.Size}.");
                int variant = i % variants;
                for (int j = 0; j < meshes.Length; j++)
                {
                    if (meshes[j].Size != rock.Size || variant-- != 0) continue;
                    Matrix4x4 matrix = Matrix4x4.TRS(rock.Position, Quaternion.Euler(rock.Rotation),
                        Vector3.one * rock.Scale) * meshes[j].LocalTransform;
                    Vector3Int key = Vector3Int.FloorToInt(rock.Position / CELL_SIZE);
                    if (!cells[j].TryGetValue(key, out DecorAsteroidCell cell))
                    {
                        cell = new DecorAsteroidCell();
                        cells[j].Add(key, cell);
                    }
                    Bounds localBounds = meshes[j].Mesh.bounds;
                    if (meshes[j].LowDetailMesh != null) localBounds.Encapsulate(meshes[j].LowDetailMesh.bounds);
                    cell.Add(matrix, localBounds);
                    break;
                }
            }

            _cells = new DecorAsteroidCell[cells.Length][];
            for (int i = 0; i < cells.Length; i++)
            {
                _cells[i] = new List<DecorAsteroidCell>(cells[i].Values).ToArray();
                foreach (DecorAsteroidCell cell in _cells[i]) cell.Seal();
            }
        }

        private void LateUpdate()
        {
            Render(_camera);
        }

        public void Render(Camera camera)
        {
            if (_cells.Length == 0) return;
            GeometryUtility.CalculateFrustumPlanes(camera, _planes);
            Vector3 cameraPosition = camera.transform.position;
            for (int i = 0; i < meshes.Length; i++)
            {
                float maxDistance = meshes[i].Size switch
                {
                    AsteroidSize.Debris => debrisDistance,
                    AsteroidSize.Medium => mediumDistance,
                    _ => largeDistance
                };
                float maxDistanceSquared = maxDistance * maxDistance;
                int count = 0;
                int lowCount = 0;
                foreach (DecorAsteroidCell cell in _cells[i])
                {
                    if (cell.Bounds.SqrDistance(cameraPosition) > maxDistanceSquared ||
                        !GeometryUtility.TestPlanesAABB(_planes, cell.Bounds)) continue;
                    bool inside = cell.IsInside(_planes);
                    for (int j = 0; j < cell.Matrices.Length; j++)
                    {
                        Bounds bounds = cell.InstanceBounds[j];
                        if (bounds.SqrDistance(cameraPosition) > maxDistanceSquared ||
                            (!inside && !GeometryUtility.TestPlanesAABB(_planes, bounds))) continue;
                        bool lowDetail = meshes[i].LowDetailMesh != null &&
                            bounds.SqrDistance(cameraPosition) >= lodDistance * lodDistance;
                        if (lowDetail)
                        {
                            _lowBatch[lowCount++] = cell.Matrices[j];
                            if (lowCount != BATCH_SIZE) continue;
                            DrawBatch(i, lowCount, camera, true);
                            lowCount = 0;
                        }
                        else
                        {
                            _batch[count++] = cell.Matrices[j];
                            if (count != BATCH_SIZE) continue;
                            DrawBatch(i, count, camera, false);
                            count = 0;
                        }
                    }
                }
                if (count > 0) DrawBatch(i, count, camera, false);
                if (lowCount > 0) DrawBatch(i, lowCount, camera, true);
            }
        }

        private void DrawBatch(int index, int count, Camera camera, bool lowDetail)
        {
            for (int submesh = 0; submesh < _materials[index].Length; submesh++)
            {
                RenderParams parameters = new RenderParams(_materials[index][submesh])
                {
                    camera = camera,
                    layer = gameObject.layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    lightProbeUsage = LightProbeUsage.Off,
                    motionVectorMode = MotionVectorGenerationMode.ForceNoMotion
                };
                Graphics.RenderMeshInstanced(parameters,
                    lowDetail ? meshes[index].LowDetailMesh : meshes[index].Mesh, submesh,
                    lowDetail ? _lowBatch : _batch, count);
            }
        }

        private void OnDestroy()
        {
            ReleaseMaterials();
        }

        private void ReleaseMaterials()
        {
            foreach (Material[] materials in _materials)
                foreach (Material material in materials)
                {
                    if (Application.isPlaying) Destroy(material);
                    else DestroyImmediate(material);
                }
            _materials = Array.Empty<Material[]>();
        }
    }
}
