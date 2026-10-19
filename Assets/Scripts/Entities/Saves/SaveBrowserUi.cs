using System.Collections.Generic;
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
        private readonly List<SaveBrowserRow> _rows = new List<SaveBrowserRow>();
        private ISaveBrowserPresenter _presenter;
        private bool _isInitialized;
        private int _modeIndex;

        public void AssignPresenter(ISaveBrowserPresenter presenter) => _presenter = presenter;

        public override void Show()
        {
            transform.SetAsLastSibling();
            base.Show();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            closeButton.onClick.AddListener(_presenter.Close);
            skirmishToggle.onValueChanged.AddListener(SelectSkirmish);
            survivalToggle.onValueChanged.AddListener(SelectSurvival);
            saveAsNewButton.onClick.AddListener(_presenter.SaveAsNew);
            confirmNameButton.onClick.AddListener(ConfirmName);
            cancelNameButton.onClick.AddListener(_presenter.CancelName);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            closeButton.onClick.RemoveListener(_presenter.Close);
            skirmishToggle.onValueChanged.RemoveListener(SelectSkirmish);
            survivalToggle.onValueChanged.RemoveListener(SelectSurvival);
            saveAsNewButton.onClick.RemoveListener(_presenter.SaveAsNew);
            confirmNameButton.onClick.RemoveListener(ConfirmName);
            cancelNameButton.onClick.RemoveListener(_presenter.CancelName);
            _isInitialized = false;
        }

        public void SetTitle(string text) => title.text = text;
        public void SetStatus(string text)
        {
            status.text = text;
            nameStatus.text = text;
        }
        public void SetSaveAsNewVisible(bool visible) => saveAsNewButton.gameObject.SetActive(visible);

        public void SetMode(int index, bool showTabs)
        {
            _modeIndex = index;
            modeTabs.SetActive(showTabs);
            skirmishToggle.SetIsOnWithoutNotify(index == 0);
            survivalToggle.SetIsOnWithoutNotify(index == 1);
        }

        public void SetSaves(List<string> labels, List<bool> enabled)
        {
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

        public void SetNameEntryVisible(bool visible)
        {
            namePanel.SetActive(visible);
            if (!visible) return;
            saveName.SetTextWithoutNotify("");
            Canvas.ForceUpdateCanvases();
            saveName.Select();
            saveName.ActivateInputField();
        }

        private void SelectSkirmish(bool selected) { if (selected && _modeIndex != 0) _presenter.SelectMode(0); }
        private void SelectSurvival(bool selected) { if (selected && _modeIndex != 1) _presenter.SelectMode(1); }
        private void ConfirmName() => _presenter.ConfirmName(saveName.text);
        private void OnDestroy() => Dispose();
    }
}
