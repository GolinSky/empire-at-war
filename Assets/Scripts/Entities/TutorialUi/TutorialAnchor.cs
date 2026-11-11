using System.Collections.Generic;
using EmpireAtWar.Components.Ui;
using EmpireAtWar.Services.Tutorial;
using UnityEngine;

namespace EmpireAtWar.Entities.TutorialUi
{
    /// <summary>
    /// Marks a HUD element a hint can highlight. Active anchors are listed while enabled, so the hint card finds
    /// them without scene searches; prefabs instantiated outside dependency injection cannot register otherwise.
    /// A placed anchor can also mark elements its view spawns at runtime (build buttons, ability buttons).
    /// </summary>
    public sealed class TutorialAnchor : MonoBehaviour, IUiHighlight
    {
        private const float VISIBLE_ALPHA = 0.01f;
        private static readonly List<TutorialAnchor> ACTIVE = new List<TutorialAnchor>();

        [SerializeField] private TutorialAnchorId id;
        [SerializeField] private RectTransform target;
        [SerializeField] private UiHighlight[] highlights = new UiHighlight[0];
        [Tooltip("Canvas group of the owning view; a hidden view (alpha 0) hides the highlight.")]
        [SerializeField] private CanvasGroup visibility;

        private readonly Dictionary<TutorialAnchorId, TutorialAnchor> _marks =
            new Dictionary<TutorialAnchorId, TutorialAnchor>();

        private void OnEnable() => ACTIVE.Add(this);

        public RectTransform Target => target;

        public void Highlight()
        {
            foreach (IUiHighlight highlight in highlights) highlight.Highlight();
        }

        public void StopHighlight()
        {
            foreach (IUiHighlight highlight in highlights) highlight.StopHighlight();
        }

        private void OnDisable()
        {
            StopHighlight();
            ACTIVE.Remove(this);
        }

        /// <summary>Gives a spawned element an anchor id; moving the id to another element clears the old mark.</summary>
        public void Mark(TutorialAnchorId markId, RectTransform element, UiHighlight highlight)
        {
            if (_marks.TryGetValue(markId, out TutorialAnchor existing))
            {
                if (existing != null && existing.target == element) return;
                if (existing != null) Destroy(existing);
            }
            TutorialAnchor mark = element.gameObject.AddComponent<TutorialAnchor>();
            mark.id = markId;
            mark.target = element;
            mark.highlights = new[] { highlight };
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
        /// Every visible anchor with this id; the spotlight covers them all (Strong against + Weak against rows).
        /// Single and group ability bars share an id but are never visible together.
        /// </summary>
        public static bool TryFindAll(TutorialAnchorId id, List<TutorialAnchor> targets)
        {
            targets.Clear();
            if (id == TutorialAnchorId.None) return false;
            foreach (TutorialAnchor anchor in ACTIVE)
                if (anchor.id == id && anchor.visibility.alpha > VISIBLE_ALPHA && anchor.target.gameObject.activeInHierarchy)
                    targets.Add(anchor);
            return targets.Count > 0;
        }
    }
}
