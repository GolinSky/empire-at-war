using System;
using EmpireAtWar.Entities.Planet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Popups
{
    public sealed class PlanetSelectionCardView : MonoBehaviour
    {
        [SerializeField] private PlanetType planet;
        [SerializeField] private Button button;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Graphic selectionOutline;
        private Action<int> _selected;

        public PlanetType Planet => planet;

        public void Initialize(Texture2D preview, Action<int> selected, string archetypeName)
        {
            previewImage.texture = preview;
            nameText.text = $"{PlanetSelectionPopupUi.GetPlanetName(planet)}\n<size=70%>{archetypeName}</size>";
            _selected = selected;
            button.onClick.AddListener(Select);
        }

        public void Render(PlanetType selected)
        {
            bool isSelected = planet == selected;
            selectedIndicator.SetActive(isSelected);
            selectionOutline.enabled = isSelected;
            nameText.color = isSelected ? new Color32(210, 169, 94, 255) : new Color32(242, 240, 230, 255);
        }

        public void Dispose()
        {
            button.onClick.RemoveListener(Select);
            _selected = null;
        }

        private void Select() => _selected((int)planet);
    }
}
