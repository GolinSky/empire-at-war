using UnityEngine;

namespace EmpireAtWar.ViewComponents.Station
{
    public sealed class StationLevelModel : MonoBehaviour
    {
        [field: SerializeField] public Renderer[] HullRenderers { get; private set; }
        [field: SerializeField] public Bounds HullBounds { get; private set; }
        [field: SerializeField] public Transform[] AttachmentPoints { get; private set; }
        [field: SerializeField] public Mesh ShieldMesh { get; private set; }
        [field: SerializeField] public Vector4[] ShieldPlanes { get; private set; }
    }
}
