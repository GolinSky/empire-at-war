using UnityEngine;

namespace ViewComponents
{
    public interface IFogOfWarSystem
    {
        void RegisterVisionSource(Transform targetTransform, float radius, float intensity = 1.0f);

        void UnregisterVisionSource(Transform targetTransform);

        float GetVisibilityAtPosition(Vector3 worldPos);

        bool IsHidden(Vector3 worldPos, float threshold = 0.1f);
    }
}
