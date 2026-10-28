using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Construction
{
    public sealed class ConstructionComponent : MonoBehaviour, IMonoComponent, ITickable, IConstructionFacade
    {
        private static readonly int BUILD_PROGRESS = Shader.PropertyToID("_BuildProgress");
        private static readonly int BUILD_MIN_Y = Shader.PropertyToID("_BuildMinY");
        private static readonly int BUILD_MAX_Y = Shader.PropertyToID("_BuildMaxY");

        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material constructionMaterial;
        private Material[][] _normalMaterials;
        private MaterialPropertyBlock _properties;
        private bool _started;
        private bool _released;

        public float Progress { get; private set; }
        public float BuildTime { get; private set; }
        public bool IsConstructing => Progress < 1f;
        public event Action<float> ProgressChanged;
        public event Action Completed;

        public void Begin(float buildTime)
        {
            _properties = new MaterialPropertyBlock();
            _normalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                _normalMaterials[i] = renderers[i].sharedMaterials;
            _started = true;
            Restore(0f, buildTime);
        }

        public void Restore(float progress, float buildTime)
        {
            BuildTime = buildTime;
            Progress = buildTime <= 0f ? 1f : Mathf.Clamp01(progress);
            ApplyMaterials();
            PublishProgress();
        }

        public void Tick()
        {
            if (!_started || _released || !IsConstructing) return;
            Progress = Mathf.Min(1f, Progress + Time.deltaTime / BuildTime);
            if (!IsConstructing) ApplyMaterials();
            PublishProgress();
        }

        public void Release()
        {
            _released = true;
        }

        private void ApplyMaterials()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!IsConstructing)
                {
                    renderers[i].sharedMaterials = _normalMaterials[i];
                    continue;
                }
                Material[] materials = new Material[_normalMaterials[i].Length];
                Array.Fill(materials, constructionMaterial);
                renderers[i].sharedMaterials = materials;
            }
        }

        private void PublishProgress()
        {
            if (IsConstructing)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                foreach (Renderer renderer in renderers)
                {
                    renderer.GetPropertyBlock(_properties);
                    _properties.SetFloat(BUILD_PROGRESS, Progress);
                    _properties.SetFloat(BUILD_MIN_Y, bounds.min.y);
                    _properties.SetFloat(BUILD_MAX_Y, bounds.max.y);
                    renderer.SetPropertyBlock(_properties);
                }
            }
            ProgressChanged?.Invoke(Progress);
            if (!IsConstructing) Completed?.Invoke();
        }
    }
}
