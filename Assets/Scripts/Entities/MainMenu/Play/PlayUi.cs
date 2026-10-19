using TMPro;
using System.Collections.Generic;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Play
{
    public sealed class PlayUi : BaseUi
    {
        [SerializeField] private Button skirmishButton;
        [SerializeField] private Button survivalButton;
        [SerializeField] private Button simulationButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Dropdown saveMode;
        [SerializeField] private TMP_Dropdown saveSlots;
        [SerializeField] private Button loadButton;
        [SerializeField] private TMP_Text loadStatus;
        private IPlayPresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(IPlayPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            skirmishButton.onClick.AddListener(_presenter.OpenSkirmish);
            survivalButton.onClick.AddListener(_presenter.OpenSurvival);
            backButton.onClick.AddListener(_presenter.Close);
            simulationButton.interactable = true;
            simulationButton.onClick.AddListener(_presenter.OpenSimulation);
            saveMode.onValueChanged.AddListener(_presenter.SelectSaveMode);
            saveSlots.onValueChanged.AddListener(_presenter.SelectSave);
            loadButton.onClick.AddListener(_presenter.LoadSave);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            simulationButton.onClick.RemoveListener(_presenter.OpenSimulation);
            saveMode.onValueChanged.RemoveListener(_presenter.SelectSaveMode);
            saveSlots.onValueChanged.RemoveListener(_presenter.SelectSave);
            loadButton.onClick.RemoveListener(_presenter.LoadSave);
            skirmishButton.onClick.RemoveListener(_presenter.OpenSkirmish);
            survivalButton.onClick.RemoveListener(_presenter.OpenSurvival);
            backButton.onClick.RemoveListener(_presenter.Close);
            _isInitialized = false;
        }

        public void SetSaves(int modeIndex, List<string> labels)
        {
            saveMode.SetValueWithoutNotify(modeIndex);
            saveSlots.ClearOptions();
            saveSlots.AddOptions(labels.Count == 0 ? new List<string> { "No saved battles" } : labels);
            saveSlots.SetValueWithoutNotify(0);
            saveSlots.RefreshShownValue();
            saveSlots.interactable = labels.Count > 0;
        }

        public void SetLoadState(bool canLoad, string status)
        {
            loadButton.interactable = canLoad;
            loadStatus.text = status;
        }

        private void OnDestroy() => Dispose();
    }
}
