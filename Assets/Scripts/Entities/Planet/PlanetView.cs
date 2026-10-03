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
