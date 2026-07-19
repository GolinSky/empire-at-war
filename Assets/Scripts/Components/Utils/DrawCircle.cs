using UnityEngine;

namespace EmpireAtWar
{
    public class DrawCircle : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private int segments = 100; // Number of segments for smoothness
        [SerializeField] private float radius = 5f;

        private float _y;
        private void Start()
        {
            CreatePoints();
        }

        private void CreatePoints()
        {
            _y = transform.position.y;
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = segments + 1; 
            float angle = 0f;
            for (int i = 0; i < (segments + 1); i++)
            {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                lineRenderer.SetPosition(i, new Vector3(x, _y, z));

                angle += (360f / segments);
            }
        }
    }
}
