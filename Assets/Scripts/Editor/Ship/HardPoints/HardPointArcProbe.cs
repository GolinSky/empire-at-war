using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Editor.Ship.HardPoints
{
    /// <summary>
    /// Casts one flat ray per degree of yaw from a weapon hardpoint against its ship's hull and reports the clear
    /// arcs. The hull is copied into a hidden preview scene as mesh colliders; only front faces block, so rays
    /// leaving the hull from inside do not count. Results are cached until the hardpoint moves or the selection
    /// changes; reselect the hardpoint to re-probe after editing hull geometry.
    /// </summary>
    [InitializeOnLoad]
    public static class HardPointArcProbe
    {
        private const string SHIELD_NAME_TAG = "Shield";
        private const float RAY_START_OFFSET = 0.1f;
        private const float RAY_LENGTH_FACTOR = 2f;

        private static readonly Dictionary<int, (Vector3 Position, Quaternion Frame, HardPointArcProbeResult Result)>
            _results = new Dictionary<int, (Vector3, Quaternion, HardPointArcProbeResult)>();
        private static readonly List<Mesh> _bakedMeshes = new List<Mesh>();
        private static Scene _hullScene;
        private static Transform _hullRoot;
        private static float _rayLength;

        static HardPointArcProbe()
        {
            Selection.selectionChanged += Reset;
            AssemblyReloadEvents.beforeAssemblyReload += Reset;
        }

        public static HardPointArcProbeResult Probe(WeaponHardPoint weapon)
        {
            Transform transform = weapon.transform;
            Quaternion frame = transform.parent == null ? Quaternion.identity : transform.parent.rotation;
            Transform shipRoot = FindShipRoot(transform);
            int id = weapon.GetInstanceID();
            if (_hullRoot == shipRoot && _results.TryGetValue(id, out var cached) &&
                cached.Position == transform.position && cached.Frame == frame)
                return cached.Result;

            if (_hullRoot != shipRoot) BuildHull(shipRoot);
            HardPointArcProbeResult result = Cast(transform.position, frame);
            _results[id] = (transform.position, frame, result);
            return result;
        }

        public static void Reset()
        {
            _results.Clear();
            if (_hullScene.IsValid()) EditorSceneManager.ClosePreviewScene(_hullScene);
            _hullScene = default;
            foreach (Mesh mesh in _bakedMeshes) Object.DestroyImmediate(mesh);
            _bakedMeshes.Clear();
            _hullRoot = null;
        }

        // Editor-only hull lookup: the ship view root owns EntityBindings.
        private static Transform FindShipRoot(Transform hardPoint)
        {
            EntityBindings bindings = hardPoint.GetComponentInParent<EntityBindings>(true);
            return bindings == null ? hardPoint.root : bindings.transform;
        }

        private static void BuildHull(Transform shipRoot)
        {
            Reset();
            _hullRoot = shipRoot;
            _hullScene = EditorSceneManager.NewPreviewScene();
            Bounds bounds = new Bounds(shipRoot.position, Vector3.zero);
            // Editor-only hull scan of every visible renderer in the ship view.
            foreach (Renderer renderer in shipRoot.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer.name.Contains(SHIELD_NAME_TAG)) continue;
                if (!TryGetMesh(renderer, out Mesh mesh, out Vector3 scale)) continue;
                GameObject hull = new GameObject(renderer.name) { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(hull, _hullScene);
                hull.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                hull.transform.localScale = scale;
                hull.AddComponent<MeshCollider>().sharedMesh = mesh;
                bounds.Encapsulate(renderer.bounds);
            }
            Physics.SyncTransforms();
            _rayLength = bounds.size.magnitude * RAY_LENGTH_FACTOR;
        }

        private static bool TryGetMesh(Renderer renderer, out Mesh mesh, out Vector3 scale)
        {
            scale = renderer.transform.lossyScale;
            mesh = null;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                skinned.BakeMesh(mesh, true);
                _bakedMeshes.Add(mesh);
                scale = Vector3.one;
            }
            else if (renderer is MeshRenderer)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null) mesh = filter.sharedMesh;
            }
            return mesh != null;
        }

        private static HardPointArcProbeResult Cast(Vector3 position, Quaternion frame)
        {
            PhysicsScene physics = _hullScene.GetPhysicsScene();
            Vector3 up = frame * Vector3.up;
            Vector3 forward = frame * Vector3.forward;
            bool[] blocked = new bool[HardPointArcCalculator.RAY_COUNT];
            bool hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = false;
            try
            {
                for (int index = 0; index < blocked.Length; index++)
                {
                    Vector3 direction = Quaternion.AngleAxis(HardPointArcCalculator.YawOf(index), up) * forward;
                    blocked[index] = physics.Raycast(position + direction * RAY_START_OFFSET, direction, out _,
                        _rayLength);
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBackfaces;
            }
            return new HardPointArcProbeResult(blocked, HardPointArcCalculator.FindClearArcs(blocked));
        }
    }
}
