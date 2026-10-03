using UnityEngine;

namespace EmpireAtWar
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHelper : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private Rect _lastSafeArea;

        public bool forceUpdate;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea)
            {
                _lastSafeArea = Screen.safeArea;
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
            var anchorMin = _lastSafeArea.position;
            var anchorMax = _lastSafeArea.position + _lastSafeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}