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
        // Material slots per renderer that use the hologram material and get tinted.
        private List<int>[] _hologramSlots;
        private Color _canBeSpawnedColor;
        private Color _blockedColor = Color.red;
        private bool _isPlacementValid;

        public bool CanSpawn => _triggeredCollider.Count == 0 && _isPlacementValid;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            Material hologram = meshRenderers[0].sharedMaterial;
            _canBeSpawnedColor = hologram.GetColor(BASE_COLOR_ID);
            _hologramSlots = new List<int>[meshRenderers.Length];
            for (var i = 0; i < meshRenderers.Length; i++)
            {
                Material[] materials = meshRenderers[i].sharedMaterials;
                _hologramSlots[i] = new List<int>();
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] == hologram)
                        _hologramSlots[i].Add(slot);
                }
            }

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

        public void SetHeight(float value)
        {
            height = value;
            UpdatePosition(transform.position);
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
                // Only hologram slots are tinted; other materials keep their own look.
                foreach (int slot in _hologramSlots[i])
                    meshRenderers[i].SetPropertyBlock(_propertyBlock, slot);
            }
        }
    }
}
