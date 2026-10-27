using EmpireAtWar.Services.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipMatchupRowView : MonoBehaviour
    {
        [SerializeField] private Image[] icons;
        [SerializeField] private TextMeshProUGUI label;

        public void Render(TooltipIcon content, TooltipIconData iconData)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                bool used = i < content.IconKeys.Count;
                icons[i].gameObject.SetActive(used);
                if (used) icons[i].sprite = iconData.Resolve(content.IconKeys[i]);
            }
            label.text = content.Label;
        }
    }
}
