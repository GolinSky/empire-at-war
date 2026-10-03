using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class IonPulseView : MonoBehaviour, IIonPulseView
    {
        private const int SEGMENTS = 64;
        [SerializeField] private LineRenderer wave;

        public void Show(Vector3 position, Vector3 direction, float radius)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            wave.positionCount = SEGMENTS;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float angle = i * Mathf.PI * 2f / SEGMENTS;
                wave.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
        }

        public void Release()
        {
            // The scene may have removed this view before ability-service disposal.
            if (this != null) Destroy(gameObject);
        }
    }
}
