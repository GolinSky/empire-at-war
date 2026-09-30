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
            text.color = requirement.IsMet ? new Color(0.58f, 0.64f, 0.72f) : new Color(1f, 0.72f, 0.35f);
        }
    }
}
