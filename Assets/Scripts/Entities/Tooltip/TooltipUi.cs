using System.Collections.Generic;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipUi : BaseUi, ITooltipUi
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TooltipHeaderView header;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private TextMeshProUGUI status;
        [SerializeField] private Transform statsParent;
        [SerializeField] private Transform strongParent;
        [SerializeField] private Transform weakParent;
        [SerializeField] private Transform requirementsParent;
        [SerializeField] private TooltipStatRowView statPrefab;
        [SerializeField] private TooltipMatchupRowView matchupPrefab;
        [SerializeField] private TooltipRequirementRowView requirementPrefab;
        private readonly List<TooltipStatRowView> _stats = new List<TooltipStatRowView>();
        private readonly List<TooltipMatchupRowView> _strong = new List<TooltipMatchupRowView>();
        private readonly List<TooltipMatchupRowView> _weak = new List<TooltipMatchupRowView>();
        private readonly List<TooltipRequirementRowView> _requirements = new List<TooltipRequirementRowView>();
        private TooltipIconData _icons;
        private TooltipSettings _settings;

        private TooltipAnchor _anchor;

        [Inject]
        public void Construct(TooltipIconData icons, TooltipSettings settings)
        { _icons = icons; _settings = settings; }

        public void Render(TooltipContent content)
        {
            header.Render(content, _icons);
            description.text = content.Description;
            description.gameObject.SetActive(!string.IsNullOrEmpty(content.Description));
            status.text = content.Status;
            status.gameObject.SetActive(!string.IsNullOrEmpty(content.Status));
            Resize(_stats, content.Stats.Count, statPrefab, statsParent);
            for (int i = 0; i < content.Stats.Count; i++) _stats[i].Render(content.Stats[i]);
            Resize(_strong, content.StrongAgainst.Count, matchupPrefab, strongParent);
            for (int i = 0; i < content.StrongAgainst.Count; i++) _strong[i].Render(content.StrongAgainst[i], _icons);
            Resize(_weak, content.WeakAgainst.Count, matchupPrefab, weakParent);
            for (int i = 0; i < content.WeakAgainst.Count; i++) _weak[i].Render(content.WeakAgainst[i], _icons);
            Resize(_requirements, content.Requirements.Count, requirementPrefab, requirementsParent);
            for (int i = 0; i < content.Requirements.Count; i++) _requirements[i].Render(content.Requirements[i]);
            Place(_anchor);
        }

        private static void Resize<T>(List<T> rows, int count, T prefab, Transform parent) where T : MonoBehaviour
        {
            while (rows.Count < count) rows.Add(Instantiate(prefab, parent));
            for (int i = 0; i < rows.Count; i++) rows[i].gameObject.SetActive(i < count);
            parent.gameObject.SetActive(count > 0);
        }

        public void Place(TooltipAnchor anchor)
        {
            _anchor = anchor;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            RectTransform canvas = (RectTransform)transform.parent;
            float scale = canvas.rect.width / Screen.width;
            float width = panel.rect.width / scale;
            float height = panel.rect.height / scale;
            float padding = _settings.ScreenEdgePadding;
            Vector2 offset = anchor.Kind == TooltipAnchorKind.Cursor ? _settings.CursorOffset : Vector2.zero;
            float x = anchor.X + offset.x;
            float y = anchor.Y + anchor.Height + offset.y;
            if (y + height > Screen.height - padding) y = anchor.Y - height - offset.y;
            if (x + width > Screen.width - padding) x = anchor.X + anchor.Width - width;
            x = Mathf.Clamp(x, padding, Mathf.Max(padding, Screen.width - width - padding));
            y = Mathf.Clamp(y, padding, Mathf.Max(padding, Screen.height - height - padding));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, new Vector2(x, y), null, out Vector2 point);
            panel.anchoredPosition = point;
            transform.SetAsLastSibling();
        }

        private void LateUpdate() { if (IsVisible) Place(_anchor); }
    }
}
