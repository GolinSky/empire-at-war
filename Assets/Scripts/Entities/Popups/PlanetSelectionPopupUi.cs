using System;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Popups
{
    public sealed class PlanetSelectionPopupUi : BaseUi
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private PlanetSelectionCardView[] cards;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text currentPlanetText;
        [SerializeField] private GameObject emptyState;
        private ISkirmishModelObserver _model;
        private bool _isInitialized;

        public event Action<int> PlanetSelected;
        public event Action CloseRequested;

        public void Initialize(ISkirmishModelObserver model, Func<PlanetType, Texture2D> preview)
        {
            _model = model;
            foreach (PlanetSelectionCardView card in cards)
                card.Initialize(preview(card.Planet), SelectPlanet);
            closeButton.onClick.AddListener(Close);
            searchInput.onValueChanged.AddListener(Filter);
            _model.Changed += Render;
            _isInitialized = true;
            Render();
            Filter(string.Empty);
        }

        public override void Show()
        {
            base.Show();
            searchInput.Select();
            searchInput.ActivateInputField();
        }

        public void SetCovered(bool covered)
        {
            if (covered) searchInput.DeactivateInputField();
            else searchInput.Select();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _model.Changed -= Render;
            closeButton.onClick.RemoveListener(Close);
            searchInput.onValueChanged.RemoveListener(Filter);
            foreach (PlanetSelectionCardView card in cards) card.Dispose();
            _isInitialized = false;
        }

        public static string GetPlanetName(PlanetType planet) => planet switch
        {
            PlanetType.AbregadoRae => "Abregado-Rae",
            PlanetType.AetenII => "Aeten II",
            PlanetType.AlzocIII => "Alzoc III",
            PlanetType.MonCalamari => "Mon Calamari",
            PlanetType.NalHutta => "Nal Hutta",
            _ => planet.ToString()
        };

        private void Render()
        {
            currentPlanetText.text = $"Current planet: {GetPlanetName(_model.Planet)}";
            foreach (PlanetSelectionCardView card in cards) card.Render(_model.Planet);
        }

        private void Filter(string query)
        {
            string term = query.Trim();
            int visible = 0;
            foreach (PlanetSelectionCardView card in cards)
            {
                bool matches = GetPlanetName(card.Planet).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
                card.gameObject.SetActive(matches);
                if (matches) visible++;
            }
            countText.text = $"{visible} / {cards.Length} planets";
            emptyState.SetActive(visible == 0);
            scrollRect.StopMovement();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void SelectPlanet(int index) => PlanetSelected(index);
        private void Close() => CloseRequested();
        private void OnDestroy() => Dispose();
    }
}
