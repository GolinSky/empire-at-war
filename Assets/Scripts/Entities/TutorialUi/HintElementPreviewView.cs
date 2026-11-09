using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Shows a still copy of a HUD element on the hint card, so the player sees what to look for. The copy keeps
    /// only visual components; its scripts are removed before it wakes, so it never reacts to clicks or events.
    /// </summary>
    public sealed class HintElementPreviewView : MonoBehaviour
    {
        [SerializeField] private RectTransform slot;

        private GameObject _copy;

        public void Show(RectTransform source)
        {
            Hide();
            GameObject holder = new GameObject("PreviewHolder", typeof(RectTransform));
            holder.SetActive(false);
            holder.transform.SetParent(slot, false);
            GameObject copy = Instantiate(source.gameObject, holder.transform, false);
            foreach (Behaviour behaviour in copy.GetComponentsInChildren<Behaviour>(true))
                if (!IsVisual(behaviour)) DestroyImmediate(behaviour);
            RectTransform rect = (RectTransform)copy.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = source.rect.size;
            Vector2 room = slot.rect.size;
            float scale = Mathf.Min(1f, room.x / Mathf.Max(1f, source.rect.width), room.y / Mathf.Max(1f, source.rect.height));
            rect.localScale = Vector3.one * scale;
            holder.SetActive(true);
            _copy = holder;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_copy != null) Destroy(_copy);
            _copy = null;
            gameObject.SetActive(false);
        }

        private static bool IsVisual(Behaviour behaviour) =>
            behaviour is Graphic || behaviour is LayoutGroup || behaviour is LayoutElement ||
            behaviour is ContentSizeFitter || behaviour is Mask || behaviour is RectMask2D ||
            behaviour is BaseMeshEffect;
    }
}
