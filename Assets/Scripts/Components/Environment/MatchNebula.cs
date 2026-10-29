using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    public sealed class MatchNebula : MonoBehaviour
    {
        [SerializeField] private MeshRenderer volumeRenderer;
        [SerializeField] private Material[] variants;

        private static readonly int _samplesId = Shader.PropertyToID("_Samples");
        private static int _previousVariant = -1;
        private static readonly System.Random _random = new System.Random();

        private MaterialPropertyBlock _properties;
        private int _qualityLevel = -1;

        public int SelectedVariant { get; private set; } = -1;

        private void Awake()
        {
            // Visual variation must not consume the gameplay random stream.
            int index = _random.Next(variants.Length - (_previousVariant >= 0 ? 1 : 0));
            if (_previousVariant >= 0 && index >= _previousVariant) index++;
            ApplyVariant(index);
            _previousVariant = index;
        }

        private void LateUpdate()
        {
            if (_qualityLevel != QualitySettings.GetQualityLevel()) ApplyQuality();
        }

        public void ApplyVariant(int index)
        {
            volumeRenderer.sharedMaterial = variants[index];
            SelectedVariant = index;
            ApplyQuality();
        }

        private void ApplyQuality()
        {
            _qualityLevel = QualitySettings.GetQualityLevel();
            int samples = _qualityLevel switch
            {
                0 or 1 => 48,
                2 => 64,
                3 => 96,
                _ => 128
            };
            _properties ??= new MaterialPropertyBlock();
            volumeRenderer.GetPropertyBlock(_properties);
            _properties.SetFloat(_samplesId, samples);
            volumeRenderer.SetPropertyBlock(_properties);
        }
    }
}
