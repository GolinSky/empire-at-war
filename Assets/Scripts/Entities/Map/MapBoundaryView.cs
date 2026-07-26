using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    [ExecuteAlways]
    public sealed class MapBoundaryView : MonoBehaviour
    {
        [SerializeField] private MapModel mapModel;
        [SerializeField] private Color borderColor = new Color(0f, 0.8f, 1f, 0.9f);
        [SerializeField] private float height;

        private void OnDrawGizmos()
        {
            if (mapModel == null)
            {
                return;
            }

            Vector2 min = mapModel.SizeRange.Min;
            Vector2 max = mapModel.SizeRange.Max;
            Vector3 center = new Vector3((min.x + max.x) * 0.5f, height, (min.y + max.y) * 0.5f);
            Vector3 size = new Vector3(max.x - min.x, 0f, max.y - min.y);
            Gizmos.color = borderColor;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
