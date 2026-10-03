using EmpireAtWar.Services.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipMatchupRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI label;

        public void Render(TooltipIcon content, TooltipIconData icons)
        { icon.sprite = icons.Resolve(content.IconKey); label.text = content.Label; }
    }
}
