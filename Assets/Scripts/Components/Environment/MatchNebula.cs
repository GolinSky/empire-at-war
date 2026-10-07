using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    public sealed class MatchNebula : MonoBehaviour
    {
        [SerializeField] private MeshRenderer volumeRenderer;
        [SerializeField] private Material[] variants;

        private static int _previousVariant = -1;
        private static readonly System.Random _random = new System.Random();

        public int SelectedVariant { get; private set; } = -1;

        private void Awake()
        {
            // Visual variation must not consume the gameplay random stream.
            int index = _random.Next(variants.Length - (_previousVariant >= 0 ? 1 : 0));
            if (_previousVariant >= 0 && index >= _previousVariant) index++;
            ApplyVariant(index);
            _previousVariant = index;
        }

        public void ApplyVariant(int index)
        {
            volumeRenderer.sharedMaterial = variants[index];
            SelectedVariant = index;
        }
    }
}
