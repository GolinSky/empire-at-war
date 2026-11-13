using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Entities.Planet
{
    public class PlanetView : View<IPlanetModelObserver>
    {
        [SerializeField] private MeshRenderer[] planetRenderers;
        [SerializeField] private MeshRenderer[] cloudRenderers;

        private static readonly int _rotationId = Shader.PropertyToID("_PlanetRotation");
        private static readonly int _rotationStartTimeId = Shader.PropertyToID("_PlanetRotationStartTime");

        public void FitObstacle(Vector3 center, float radius)
        {
            Vector3 visualCenter = planetRenderers[0].transform.TransformPoint(planetRenderers[0].localBounds.center);
            float visualRadius = GetVisualRadius(planetRenderers, visualCenter);
            visualRadius = Mathf.Max(visualRadius, GetVisualRadius(cloudRenderers, visualCenter));
            Vector3 localCenter = transform.InverseTransformPoint(visualCenter);
            float fitScale = radius / visualRadius;
            // Preserve the authored clearance below gameplay as the planet changes size.
            float height = (visualCenter.y - center.y) * fitScale;
            transform.localScale *= fitScale;
            transform.position += center + Vector3.up * height - transform.TransformPoint(localCenter);
        }

        private static float GetVisualRadius(MeshRenderer[] renderers, Vector3 center)
        {
            float radius = 0f;
            foreach (MeshRenderer renderer in renderers)
            {
                // Planet and cloud meshes are spheres; local bounds remain available while creation is inactive.
                Vector3 extent = renderer.localBounds.extents;
                Vector3 scale = renderer.transform.lossyScale;
                float sphereRadius = Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z)) *
                    Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                Vector3 sphereCenter = renderer.transform.TransformPoint(renderer.localBounds.center);
                radius = Mathf.Max(radius, Vector3.Distance(center, sphereCenter) + sphereRadius);
            }
            return radius;
        }

        protected override void OnInitialize()
        {
            var properties = new MaterialPropertyBlock();
            SetRotation(planetRenderers, Model.PlanetOrbitSpeed, properties);
            SetRotation(cloudRenderers, Model.CloudOrbitSpeed, properties);
        }

        protected override void OnDispose()
        {
        }

        private static void SetRotation(MeshRenderer[] renderers, float speed, MaterialPropertyBlock properties)
        {
            foreach (var renderer in renderers)
            {
                var axis = renderer.transform.InverseTransformDirection(Vector3.forward);
                renderer.GetPropertyBlock(properties);
                properties.SetVector(_rotationId, new Vector4(axis.x, axis.y, axis.z, speed));
                properties.SetFloat(_rotationStartTimeId, Time.time);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
