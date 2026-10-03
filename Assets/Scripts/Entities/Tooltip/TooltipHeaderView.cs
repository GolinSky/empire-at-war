using EmpireAtWar.Services.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipHeaderView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI subtitle;
        [SerializeField] private TextMeshProUGUI shortcut;

        public void Render(TooltipContent content, TooltipIconData icons)
        {
            icon.gameObject.SetActive(!string.IsNullOrEmpty(content.IconKey));
            if (!string.IsNullOrEmpty(content.IconKey)) icon.sprite = icons.Resolve(content.IconKey);
            title.text = content.Title;
            subtitle.text = content.Subtitle;
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(content.Subtitle));
            shortcut.text = content.Shortcut;
            shortcut.gameObject.SetActive(!string.IsNullOrEmpty(content.Shortcut));
        }
    }
}
