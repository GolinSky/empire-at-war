using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Views.Reinforcement
{
    public class UnitSpawnView : MonoBehaviour
    {
        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        [SerializeField] private float height;
        [SerializeField] private MeshRenderer[] meshRenderers;

        private List<Collider> _triggeredCollider = new List<Collider>();
        // A property block tints every renderer without creating Material instances.
        private MaterialPropertyBlock _propertyBlock;
        private Color _canBeSpawnedColor;
        private Color _blockedColor = Color.red;
        private bool _isPlacementValid;

        public bool CanSpawn => _triggeredCollider.Count == 0 && _isPlacementValid;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _canBeSpawnedColor = meshRenderers[0].sharedMaterial.GetColor(BASE_COLOR_ID);
            UpdateColor();
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void UpdatePosition(Vector3 position)
        {
            position.y = height;
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetPlacementValidity(bool isPlacementValid)
        {
            _isPlacementValid = isPlacementValid;
            UpdateColor();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!_triggeredCollider.Contains(other))
                _triggeredCollider.Add(other);

            UpdateColor();
        }

        private void OnTriggerExit(Collider other)
        {
            if(_triggeredCollider.Contains(other))
                _triggeredCollider.Remove(other);

            UpdateColor();
        }

        private void UpdateColor()
        {
            _propertyBlock.SetColor(BASE_COLOR_ID, CanSpawn ? _canBeSpawnedColor : _blockedColor);
            for (var i = 0; i < meshRenderers.Length; i++)
            {
                // Slot 0 is the hologram material; later slots keep their own look.
                meshRenderers[i].SetPropertyBlock(_propertyBlock, 0);
            }
        }
    }
}
