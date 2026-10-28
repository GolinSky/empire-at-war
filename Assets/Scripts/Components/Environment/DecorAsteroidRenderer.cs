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
        [SerializeField] private DecorAsteroidMesh[] meshes;
        [SerializeField, Min(1f)] private float debrisDistance = 2200f;
        [SerializeField, Min(1f)] private float mediumDistance = 5000f;
        private readonly Matrix4x4[] _batch = new Matrix4x4[BATCH_SIZE];
        private Matrix4x4[][] _instances = Array.Empty<Matrix4x4[]>();
        private Material[][] _materials = Array.Empty<Material[]>();
        private Camera _camera;

        public void Configure(IReadOnlyList<AsteroidSpot> rocks, Camera camera)
        {
            ReleaseMaterials();
            _camera = camera;
            List<Matrix4x4>[] instances = new List<Matrix4x4>[meshes.Length];
            _materials = new Material[meshes.Length][];
            for (int i = 0; i < meshes.Length; i++)
            {
                instances[i] = new List<Matrix4x4>();
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
                    instances[j].Add(Matrix4x4.TRS(rock.Position, Quaternion.Euler(rock.Rotation),
                        Vector3.one * rock.Scale) * meshes[j].LocalTransform);
                    break;
                }
            }

            _instances = new Matrix4x4[instances.Length][];
            for (int i = 0; i < instances.Length; i++)
                _instances[i] = instances[i].ToArray();
        }

        private void LateUpdate()
        {
            Render(_camera);
        }

        public void Render(Camera camera)
        {
            if (_instances.Length == 0) return;
            Vector3 cameraPosition = camera.transform.position;
            for (int i = 0; i < meshes.Length; i++)
            {
                float maxDistance = meshes[i].Size switch
                {
                    AsteroidSize.Debris => debrisDistance,
                    AsteroidSize.Medium => mediumDistance,
                    _ => camera.farClipPlane
                };
                int count = 0;
                foreach (Matrix4x4 instance in _instances[i])
                {
                    Vector3 position = instance.GetColumn(3);
                    if ((position - cameraPosition).sqrMagnitude > maxDistance * maxDistance) continue;
                    _batch[count++] = instance;
                    if (count != BATCH_SIZE) continue;
                    DrawBatch(i, count, camera);
                    count = 0;
                }
                if (count > 0) DrawBatch(i, count, camera);
            }
        }

        private void DrawBatch(int index, int count, Camera camera)
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
                Graphics.RenderMeshInstanced(parameters, meshes[index].Mesh, submesh, _batch, count);
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
