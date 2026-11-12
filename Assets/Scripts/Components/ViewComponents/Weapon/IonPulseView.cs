using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class IonPulseView : MonoBehaviour, IIonPulseView
    {
        private const int SEGMENTS = 64;
        [SerializeField] private LineRenderer wave;
        [SerializeField] private IonStunView ionStunField;

        private float _radius = -1f;

        public void Show(Vector3 position, Vector3 direction, float radius)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            if (_radius == radius) return;

            _radius = radius;
            wave.positionCount = SEGMENTS;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float angle = i * Mathf.PI * 2f / SEGMENTS;
                wave.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            // Configure expands these bounds. Keep the arcs inside the ring with a shallow depth.
            ionStunField.Configure(new Bounds(Vector3.zero, new Vector3(1.3f, 1.3f, 0.3f) * radius));
            ionStunField.SetActive(true);
        }

        public void Release()
        {
            // The scene may have removed this view before ability-service disposal.
            if (this != null) Destroy(gameObject);
        }
    }
}
