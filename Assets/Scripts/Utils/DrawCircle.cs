using UnityEngine;

namespace EmpireAtWar
{
    public class DrawCircle : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public int segments = 100; // Number of segments for smoothness
        public float radius = 5f;

        private float y;
        void Start()
        {
            y = transform.position.y;
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = segments + 1; // Add one to loop the circle
            CreatePoints();
        }

        void CreatePoints()
        {
            float angle = 0f;
            for (int i = 0; i < (segments + 1); i++)
            {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                // Set the position in world space, adjust Y to place it above your scene
                lineRenderer.SetPosition(i, new Vector3(x, y, z));

                angle += (360f / segments);
            }
        }
    }
}
