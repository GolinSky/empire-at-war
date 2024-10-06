using UnityEngine;

namespace EmpireAtWar
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHelper : MonoBehaviour
    {
        public bool forceUpdate;
        private RectTransform rectTransform;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea)
            {
                lastSafeArea = Screen.safeArea;
                RefreshWithDelay();
            }

            if (forceUpdate)
            {
                forceUpdate = false;
                Refresh();
            }
        }

        private void RefreshWithDelay()
        {
            Invoke(nameof(Refresh), 0.1f);
        }
        private void Refresh()
        {
            var anchorMin = lastSafeArea.position;
            var anchorMax = lastSafeArea.position + lastSafeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}