using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    [RequireComponent(typeof(MeshRenderer))]
    public class ShieldView :MonoBehaviour
    {
        private static readonly Vector2 MaterialOffset = Vector3.up*0.1f;

        private Material shieldMaterial;
        private MeshRenderer meshRenderer;
        public bool IsVisibleToCamera { get; private set; }

        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            shieldMaterial = meshRenderer.material;
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
            shieldMaterial.mainTextureOffset += MaterialOffset * Time.deltaTime;
        }
    }
}