using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public sealed class FactionTypeFilterButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject allTypesGlyph;

        public Button Button => button;
        public TMP_Text Label => label;

        public void Render(string text, Sprite sprite, bool isAllTypes)
        {
            label.text = text;
            icon.sprite = sprite;
            icon.enabled = !isAllTypes && sprite != null;
            allTypesGlyph.SetActive(isAllTypes);
        }
    }
}
