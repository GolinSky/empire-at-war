using EmpireAtWar.Services.Tooltip;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipRequirementRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        public void Render(TooltipRequirement requirement)
        {
            text.text = (requirement.IsMet ? "[Met] " : "[Required] ") + requirement.Text;
            text.color = requirement.IsMet ? new Color32(242, 240, 230, 255) : new Color32(210, 169, 94, 255);
        }
    }
}
