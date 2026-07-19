using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    [RequireComponent(typeof(MeshRenderer))]
    public class ShieldView :MonoBehaviour
    {
        private static readonly Vector2 MaterialOffset = Vector3.up*0.1f;

        private Material[] _shieldMaterials;
        private MeshRenderer[] _meshRenderers;
        public bool IsVisibleToCamera { get; private set; }

        private void Start()
        {
            _meshRenderers = GetComponentsInChildren<MeshRenderer>();
            _shieldMaterials = new Material[_meshRenderers.Length];
            for (var i = 0; i < _meshRenderers.Length; i++)
            {
                _shieldMaterials[i] = _meshRenderers[i].material;
            }
        }

        public void SetActive(bool isActive)
        {
           gameObject.SetActive(isActive);   
        }
        
        private void OnBecameVisible()
        {
            IsVisibleToCamera = true;
        }

        private void OnBecameInvisible()
        {
            IsVisibleToCamera = false;
        }

        public void AnimateTextureOffset()
        {
            Vector2 offset = MaterialOffset * Time.deltaTime;
            foreach (Material material in _shieldMaterials)
            {
                material.mainTextureOffset += offset;
            }
        }
    }
}