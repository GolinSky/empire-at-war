using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Views.Reinforcement
{
    public class UnitSpawnView : MonoBehaviour
    {
        [SerializeField] private float height;
        
        private List<Collider> _triggeredCollider = new List<Collider>();
        private MeshRenderer[] _meshRendererList;
        private List<Material> _meshMaterials = new List<Material>();
        private Color _canBeSpawnedColor;
        private Color _blockedColor = Color.red;

        public bool CanSpawn => _triggeredCollider.Count == 0;
        public Vector3 Position => transform.position;

        private void Start()
        {
            _meshRendererList = GetComponentsInChildren<MeshRenderer>();
            for (var i = 0; i < _meshRendererList.Length; i++)
            {
                _meshMaterials.Add(_meshRendererList[i].material);
            }
            _canBeSpawnedColor = _meshMaterials[0].color;
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

        private void OnTriggerEnter(Collider other)
        {
            for (var i = 0; i < _meshMaterials.Count; i++)
            {
                _meshMaterials[i].color = _blockedColor;
            }
            
            if(!_triggeredCollider.Contains(other))
                _triggeredCollider.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if(_triggeredCollider.Contains(other))
                _triggeredCollider.Remove(other);
            
            if (_triggeredCollider.Count == 0)
            {
                for (var i = 0; i < _meshMaterials.Count; i++)
                {
                    _meshMaterials[i].color = _canBeSpawnedColor;
                }
            }
        }
    }
}