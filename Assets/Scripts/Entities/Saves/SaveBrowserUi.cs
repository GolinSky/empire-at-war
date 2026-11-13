using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Saves
{
    public sealed class SaveBrowserUi : BaseUi
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject modeTabs;
        [SerializeField] private Toggle skirmishToggle;
        [SerializeField] private Toggle survivalToggle;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private SaveBrowserRow rowTemplate;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button saveAsNewButton;
        [SerializeField] private GameObject namePanel;
        [SerializeField] private TMP_InputField saveName;
        [SerializeField] private TMP_Text nameStatus;
        [SerializeField] private Button confirmNameButton;
        [SerializeField] private Button cancelNameButton;
        [SerializeField] private GameObject loadPanel;
        [SerializeField] private RectTransform saveList;
        [SerializeField] private GameObject columnHeaders;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text selectedName;
        [SerializeField] private TMP_Text selectedKind;
        [SerializeField] private TMP_Text selectedSavedAt;
        [SerializeField] private TMP_Text selectedBattleTime;
        [SerializeField] private TMP_Text selectedMode;
        [SerializeField] private TMP_Text selectedCompatibility;
        private readonly List<SaveBrowserRow> _rows = new List<SaveBrowserRow>();
        private ISaveBrowserPresenter _presenter;
        private ISaveCreationPresenter _creation;
        private bool _isInitialized;
        private int _modeIndex;

        public event Action<int> ModeSelected;
        public event Action LoadRequested;
        public event Action DeleteRequested;

        public void AssignPresenter(ISaveBrowserPresenter presenter) => _presenter = presenter;

        /// <summary>Save route only: binds save-as-new and naming. The load route never assigns it.</summary>
        public void AssignCreationPresenter(ISaveCreationPresenter creation) => _creation = creation;

        public override void Show()
        {
            transform.SetAsLastSibling();
            base.Show();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            closeButton.onClick.AddListener(_presenter.Close);
            backButton.onClick.AddListener(_presenter.Close);
            loadButton.onClick.AddListener(RequestLoad);
            deleteButton.onClick.AddListener(RequestDelete);
            skirmishToggle.onValueChanged.AddListener(SelectSkirmish);
            survivalToggle.onValueChanged.AddListener(SelectSurvival);
            if (_creation != null)
            {
                saveAsNewButton.onClick.AddListener(_creation.SaveAsNew);
                confirmNameButton.onClick.AddListener(ConfirmName);
                cancelNameButton.onClick.AddListener(_creation.CancelName);
            }
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            closeButton.onClick.RemoveListener(_presenter.Close);
            backButton.onClick.RemoveListener(_presenter.Close);
            loadButton.onClick.RemoveListener(RequestLoad);
            deleteButton.onClick.RemoveListener(RequestDelete);
            skirmishToggle.onValueChanged.RemoveListener(SelectSkirmish);
            survivalToggle.onValueChanged.RemoveListener(SelectSurvival);
            if (_creation != null)
            {
                saveAsNewButton.onClick.RemoveListener(_creation.SaveAsNew);
                confirmNameButton.onClick.RemoveListener(ConfirmName);
                cancelNameButton.onClick.RemoveListener(_creation.CancelName);
            }
            _isInitialized = false;
        }

        public void SetTitle(string text) => title.text = text;
        public void SetStatus(string text)
        {
            status.text = text;
            nameStatus.text = text;
        }
        public void SetSaveAsNewVisible(bool visible)
        {
            saveAsNewButton.gameObject.SetActive(visible);
            saveList.anchorMax = new Vector2(visible ? 1 : 0.64f, 1);
            saveList.offsetMax = new Vector2(visible ? -40 : -30, -190);
            columnHeaders.SetActive(!visible);
            loadPanel.SetActive(!visible);
        }

        public void SetMode(int index, bool showTabs)
        {
            _modeIndex = index;
            modeTabs.SetActive(showTabs);
            title.rectTransform.sizeDelta = new Vector2(showTabs ? 480 : 1400, title.rectTransform.sizeDelta.y);
            skirmishToggle.SetIsOnWithoutNotify(index == 0);
            survivalToggle.SetIsOnWithoutNotify(index == 1);
        }

        public void SetSaves(List<string> labels, List<bool> enabled)
        {
            loadPanel.SetActive(false);
            foreach (var row in _rows) row.gameObject.SetActive(false);
            for (int i = 0; i < labels.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(rowTemplate, content));
                var row = _rows[i];
                row.Initialize(i, labels[i], enabled[i], _presenter.SelectSave, _presenter.RemoveSave);
                row.gameObject.SetActive(true);
            }
            scrollRect.verticalNormalizedPosition = 1;
        }

        public void SetLoadSaves(IReadOnlyList<BattleSaveHeader> saves)
        {
            loadPanel.SetActive(true);
            foreach (var row in _rows) row.gameObject.SetActive(false);
            for (int i = 0; i < saves.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(rowTemplate, content));
                BattleSaveHeader save = saves[i];
                _rows[i].InitializeLoad(i, save.Slot.Name, KindLabel(save.Slot.Kind),
                    save.SavedAtUtc.ToLocalTime().ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture), BattleTimeLabel(save.BattleTime),
                    save.IsCompatible, _presenter.SelectSave, _presenter.RemoveSave);
                _rows[i].gameObject.SetActive(true);
            }
            ClearLoadSelection();
            scrollRect.verticalNormalizedPosition = 1;
        }

        public void SetLoadSelection(int index, BattleSaveHeader save)
        {
            for (int i = 0; i < _rows.Count; i++) _rows[i].SetSelected(i == index);
            selectedName.text = save.Slot.Name;
            selectedKind.text = KindLabel(save.Slot.Kind) + " SAVE";
            selectedSavedAt.text = save.SavedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
            selectedBattleTime.text = BattleTimeLabel(save.BattleTime);
            selectedMode.text = save.Mode.ToString();
            selectedCompatibility.text = save.IsCompatible ? "Compatible" : "Unsupported version";
            selectedCompatibility.color = save.IsCompatible ? new Color32(109, 184, 226, 255) : new Color32(232, 110, 108, 255);
            loadButton.interactable = save.IsCompatible;
            deleteButton.interactable = true;
        }

        public void ClearLoadSelection()
        {
            foreach (var row in _rows) row.SetSelected(false);
            selectedName.text = "No save selected";
            selectedKind.text = "";
            selectedSavedAt.text = "—";
            selectedBattleTime.text = "—";
            selectedMode.text = "—";
            selectedCompatibility.text = "Select a save to view details";
            selectedCompatibility.color = new Color32(109, 184, 226, 255);
            loadButton.interactable = false;
            deleteButton.interactable = false;
        }

        private static string KindLabel(BattleSaveSlotKind kind) => kind switch
        {
            BattleSaveSlotKind.Autosave => "AUTO",
            BattleSaveSlotKind.Quicksave => "QUICK",
            _ => "MANUAL"
        };

        private static string BattleTimeLabel(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        public void SetNameEntryVisible(bool visible)
        {
            namePanel.SetActive(visible);
            if (!visible) return;
            saveName.SetTextWithoutNotify("");
            Canvas.ForceUpdateCanvases();
            saveName.Select();
            saveName.ActivateInputField();
        }

        private void SelectSkirmish(bool selected) { if (selected && _modeIndex != 0) ModeSelected?.Invoke(0); }
        private void SelectSurvival(bool selected) { if (selected && _modeIndex != 1) ModeSelected?.Invoke(1); }
        private void ConfirmName() => _creation.ConfirmName(saveName.text);
        private void RequestLoad() => LoadRequested.Invoke();
        private void RequestDelete() => DeleteRequested.Invoke();
        private void OnDestroy() => Dispose();
    }
}
