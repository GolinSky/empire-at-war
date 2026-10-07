using UnityEngine;

namespace EmpireAtWar.ViewComponents.Station
{
    public sealed class StationLevelModel : MonoBehaviour
    {
        [field: SerializeField] public Renderer[] HullRenderers { get; private set; }
        [Tooltip("Visible hull in station space. Levels share one source pivot, so the center is not zero.")]
        [field: SerializeField] public Bounds HullBounds { get; private set; }
        [Tooltip("Hardpoints installed at this level; locked hardpoints have no mount.")]
        [field: SerializeField] public StationMount[] Mounts { get; private set; }
        [field: SerializeField] public Transform LaunchExit { get; private set; }
        [field: SerializeField] public Mesh ShieldMesh { get; private set; }
        [field: SerializeField] public Vector3 ShieldCenter { get; private set; }
        [field: SerializeField] public Vector4[] ShieldPlanes { get; private set; }
    }
}
