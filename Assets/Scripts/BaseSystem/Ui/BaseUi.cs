using System;
using UnityEngine;

namespace EmpireAtWar.Ui.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUi : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public bool IsVisible { get; private set; } = true;

        public virtual void SetParent(Transform parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            transform.SetParent(parent, false);
        }

        public virtual void Show()
        {
            SetVisibility(true);
        }

        public virtual void Hide()
        {
            SetVisibility(false);
        }

        // Skip unchanged writes: a CanvasGroup change notifies every UI behaviour below the group, and the
        // ship panels call Show/Hide several times per selection change.
        internal void SetInteraction(bool isInteractive)
        {
            if (canvasGroup.interactable != isInteractive) canvasGroup.interactable = isInteractive;
            if (canvasGroup.blocksRaycasts != isInteractive) canvasGroup.blocksRaycasts = isInteractive;
        }

        private void SetVisibility(bool isVisible)
        {
            float alpha = isVisible ? 1f : 0f;
            if (canvasGroup.alpha != alpha) canvasGroup.alpha = alpha;
            SetInteraction(isVisible);
            IsVisible = isVisible;
        }
    }

}
