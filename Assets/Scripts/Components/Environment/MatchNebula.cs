using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    public sealed class MatchNebula : MonoBehaviour
    {
        [SerializeField] private MeshRenderer volumeRenderer;
        [SerializeField] private Material[] variants;
        [SerializeField] private Shader impostorShader;

        private static readonly int _samplesId = Shader.PropertyToID("_Samples");
        private static readonly int _bakingId = Shader.PropertyToID("_Baking");
        private static int _previousVariant = -1;
        private static readonly System.Random _random = new System.Random();

        private MaterialPropertyBlock _properties;
        private NebulaImpostor _impostor;
        private int _qualityLevel = -1;

        public static bool NebulaEnabled { get; set; } = true;
        public int SelectedVariant { get; private set; } = -1;

        private void Awake()
        {
            // Visual variation must not consume the gameplay random stream.
            int index = _random.Next(variants.Length - (_previousVariant >= 0 ? 1 : 0));
            if (_previousVariant >= 0 && index >= _previousVariant) index++;
            ApplyVariant(index);
            _previousVariant = index;
        }

        private void Start()
        {
            _impostor = new NebulaImpostor(volumeRenderer, impostorShader);
            Bake();
        }

        private void LateUpdate()
        {
            if (_qualityLevel != QualitySettings.GetQualityLevel()) ApplyQuality();
        }

        private void OnEnable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (_impostor != null) _impostor.SetVisible(false);
            volumeRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_impostor != null) _impostor.Dispose();
        }

        public void ApplyVariant(int index)
        {
            volumeRenderer.sharedMaterial = variants[index];
            SelectedVariant = index;
            ApplyQuality();
            if (_impostor != null) Bake();
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
            _properties.SetFloat(_samplesId, samples);
            volumeRenderer.SetPropertyBlock(_properties);
        }

        private void Bake()
        {
            _properties.SetFloat(_samplesId, 128);
            _properties.SetFloat(_bakingId, 1);
            volumeRenderer.SetPropertyBlock(_properties);
            try { _impostor.Bake(); }
            finally
            {
                _properties.SetFloat(_bakingId, 0);
                ApplyQuality();
            }
        }

        private void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (_impostor == null || _impostor.IsBaking) return;
            // Cinematic shots change yaw/pitch; retain the volume for those views.
            bool useVolume = Vector3.Angle(camera.transform.forward, _impostor.Forward) > 1f;
            volumeRenderer.enabled = NebulaEnabled && useVolume;
            _impostor.SetVisible(NebulaEnabled && !useVolume);
        }
    }
}
