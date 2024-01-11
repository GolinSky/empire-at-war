using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUnitUi : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private Image unitIconImage;


        public void SetData(Sprite icon, string unitName, string price)
        {
            unitIconImage.sprite = icon;
            unitNameText.text = unitName;
            unitPriceText.text = price;
        }
    }
}
