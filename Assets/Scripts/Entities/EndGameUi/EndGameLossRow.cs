using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.EndGameUi
{
    public sealed class EndGameLossRow : MonoBehaviour
    {
        [SerializeField] private Image unitIcon;
        [SerializeField] private TMP_Text unitName;
        [SerializeField] private TMP_Text unitCount;

        public void Show(string name, Sprite icon, int count)
        {
            unitIcon.sprite = icon;
            unitIcon.enabled = icon != null;
            unitName.text = name;
            unitCount.text = count > 0 ? $"×{count}" : string.Empty;
        }
    }
}
