using EmpireAtWar.Services.Tooltip;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipStatRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI value;

        public void Render(TooltipStat stat) { label.text = stat.Label; value.text = stat.Display; }
    }
}
