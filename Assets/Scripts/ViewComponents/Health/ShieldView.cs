using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    [RequireComponent(typeof(MeshRenderer))]
    public class ShieldView :MonoBehaviour
    {
        private static readonly Vector2 MaterialOffset = Vector3.up*0.1f;

        private Material[] shieldMaterials;
        private MeshRenderer[] meshRenderers;
        public bool IsVisibleToCamera { get; private set; }

        private void Start()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            shieldMaterials = new Material[meshRenderers.Length];
            for (var i = 0; i < meshRenderers.Length; i++)
            {
                shieldMaterials[i] = meshRenderers[i].material;
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
            foreach (Material material in shieldMaterials)
            {
                material.mainTextureOffset += offset;
            }
        }
    }
}