using System.Collections.Generic;
using EmpireAtWar.Services.Tutorial;
using UnityEngine;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Marks a HUD element a hint can highlight. Active anchors are listed while enabled, so the hint card finds
    /// them without scene searches; prefabs instantiated outside dependency injection cannot register otherwise.
    /// </summary>
    public sealed class TutorialAnchor : MonoBehaviour
    {
        private const float VISIBLE_ALPHA = 0.01f;
        private static readonly List<TutorialAnchor> ACTIVE = new List<TutorialAnchor>();

        [SerializeField] private TutorialAnchorId id;
        [SerializeField] private RectTransform target;
        [Tooltip("Canvas group of the owning view; a hidden view (alpha 0) hides the highlight.")]
        [SerializeField] private CanvasGroup visibility;

        private void OnEnable() => ACTIVE.Add(this);

        private void OnDisable() => ACTIVE.Remove(this);

        /// <summary>First visible anchor with this id; several prefabs may share one (single and group ability bars).</summary>
        public static bool TryFind(TutorialAnchorId id, out RectTransform target)
        {
            foreach (TutorialAnchor anchor in ACTIVE)
                if (anchor.id == id && anchor.visibility.alpha > VISIBLE_ALPHA)
                {
                    target = anchor.target;
                    return true;
                }
            target = null;
            return false;
        }
    }
}
