using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public sealed class ResearchBuffIcon : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text tier;
        [SerializeField] private TooltipTrigger tooltipTrigger;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        public void Render(ResearchType type, Sprite sprite, int completedTiers)
        {
            tooltipTrigger.SetKey(type);
            icon.sprite = sprite;
            icon.color = new Color(1f, 1f, 1f, completedTiers > 0 ? 1f : 0.5f);
            tier.text = completedTiers switch
            {
                0 => "–",
                1 => "I",
                2 => "II",
                3 => "III",
                _ => completedTiers.ToString()
            };
        }
    }
}
