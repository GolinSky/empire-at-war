using System.Collections.Generic;
using EmpireAtWar.Services.Tutorial;
using UnityEngine;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Marks a HUD element a hint can highlight. Active anchors are listed while enabled, so the hint card finds
    /// them without scene searches; prefabs instantiated outside dependency injection cannot register otherwise.
    /// A placed anchor can also mark elements its view spawns at runtime (build buttons, ability buttons).
    /// </summary>
    public sealed class TutorialAnchor : MonoBehaviour
    {
        private const float VISIBLE_ALPHA = 0.01f;
        private static readonly List<TutorialAnchor> ACTIVE = new List<TutorialAnchor>();

        [SerializeField] private TutorialAnchorId id;
        [SerializeField] private RectTransform target;
        [Tooltip("Canvas group of the owning view; a hidden view (alpha 0) hides the highlight.")]
        [SerializeField] private CanvasGroup visibility;

        private readonly Dictionary<TutorialAnchorId, TutorialAnchor> _marks =
            new Dictionary<TutorialAnchorId, TutorialAnchor>();

        private void OnEnable() => ACTIVE.Add(this);

        private void OnDisable() => ACTIVE.Remove(this);

        /// <summary>Gives a spawned element an anchor id; moving the id to another element clears the old mark.</summary>
        public void Mark(TutorialAnchorId markId, RectTransform element)
        {
            if (_marks.TryGetValue(markId, out TutorialAnchor existing))
            {
                if (existing != null && existing.target == element) return;
                if (existing != null) Destroy(existing);
            }
            TutorialAnchor mark = element.gameObject.AddComponent<TutorialAnchor>();
            mark.id = markId;
            mark.target = element;
            mark.visibility = visibility;
            _marks[markId] = mark;
        }

        public void Unmark(TutorialAnchorId markId)
        {
            if (!_marks.TryGetValue(markId, out TutorialAnchor existing)) return;
            if (existing != null) Destroy(existing);
            _marks.Remove(markId);
        }

        /// <summary>
        /// Every visible anchor with this id; the frame covers them all (Strong against + Weak against rows).
        /// Single and group ability bars share an id but are never visible together.
        /// </summary>
        public static bool TryFindAll(TutorialAnchorId id, List<RectTransform> targets)
        {
            targets.Clear();
            if (id == TutorialAnchorId.None) return false;
            foreach (TutorialAnchor anchor in ACTIVE)
                if (anchor.id == id && anchor.visibility.alpha > VISIBLE_ALPHA && anchor.target.gameObject.activeInHierarchy)
                    targets.Add(anchor.target);
            return targets.Count > 0;
        }
    }
}
