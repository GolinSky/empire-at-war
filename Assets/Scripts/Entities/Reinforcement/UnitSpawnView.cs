using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Reinforcement
{
    public class UnitSpawnView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] meshRenderers;
        // Slots using this material are tinted; its base color is the "can be spawned" color.
        [SerializeField] private Material hologramMaterial;
        private List<Collider> _triggeredCollider = new List<Collider>();
        // A property block tints every renderer without creating Material instances.
        private MaterialPropertyBlock _propertyBlock;
        // Material slots per renderer that use the hologram material and get tinted.
        private List<int>[] _hologramSlots;

        private Color _canBeSpawnedColor;
        private Color _blockedColor = Color.red;

        [SerializeField] private float height;

        private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

        private bool _isPlacementValid;
        private bool _useTriggerClearance = true;

        public bool CanSpawn => (!_useTriggerClearance || _triggeredCollider.Count == 0) && _isPlacementValid;
        public Vector3 Position => transform.position;

        /// <summary>Largest planar distance from the pivot to the edge of the rendered bounds.</summary>
        public float PlanarRadius
        {
            get
            {
                Vector3 pivot = transform.position;
                float radius = 0f;
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    Bounds bounds = meshRenderer.bounds;
                    radius = Mathf.Max(radius,
                        Mathf.Abs(bounds.max.x - pivot.x), Mathf.Abs(bounds.min.x - pivot.x),
                        Mathf.Abs(bounds.max.z - pivot.z), Mathf.Abs(bounds.min.z - pivot.z));
                }

                return radius;
            }
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _canBeSpawnedColor = hologramMaterial.GetColor(BASE_COLOR_ID);
            _hologramSlots = new List<int>[meshRenderers.Length];
            for (var i = 0; i < meshRenderers.Length; i++)
            {
                Material[] materials = meshRenderers[i].sharedMaterials;
                _hologramSlots[i] = new List<int>();
                for (var slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] == hologramMaterial)
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

        public void UsePlacementValidityOnly()
        {
            _useTriggerClearance = false;
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
