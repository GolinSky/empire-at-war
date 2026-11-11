using System.Collections.Generic;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.ServerBrowser
{
    /// <summary>Join Game: open Steam lobbies (click a row to join), Refresh, and Join by IP.</summary>
    public sealed class ServerBrowserUi : BaseUi
    {
        [SerializeField] private ServerBrowserRowView rowTemplate;
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private Button refreshButton;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private Button joinIpButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text statusText;
        private readonly List<ServerBrowserRowView> _rows = new List<ServerBrowserRowView>();
        private IServerBrowserPresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(IServerBrowserPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            rowTemplate.gameObject.SetActive(false);
            refreshButton.onClick.AddListener(_presenter.Refresh);
            joinIpButton.onClick.AddListener(JoinByIp);
            backButton.onClick.AddListener(_presenter.Close);
            _isInitialized = true;
        }

        /// <param name="busy">Connecting to a host: joining and refreshing wait; Back cancels.</param>
        public void Render(IReadOnlyList<ServerBrowserRowState> rows, string status, bool busy, bool canRefresh)
        {
            while (_rows.Count < rows.Count) AddRow();
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i < rows.Count) _rows[i].Render(rows[i], !busy);
                else _rows[i].Hide();
            }

            statusText.text = status;
            refreshButton.interactable = canRefresh && !busy;
            joinIpButton.interactable = !busy;
            addressInput.interactable = !busy;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            refreshButton.onClick.RemoveListener(_presenter.Refresh);
            joinIpButton.onClick.RemoveListener(JoinByIp);
            backButton.onClick.RemoveListener(_presenter.Close);
            foreach (ServerBrowserRowView row in _rows) row.Dispose();
            _isInitialized = false;
        }

        private void AddRow()
        {
            int index = _rows.Count;
            ServerBrowserRowView row = Instantiate(rowTemplate, rowContainer);
            row.Initialize(() => _presenter.JoinLobby(index));
            _rows.Add(row);
        }

        private void JoinByIp() => _presenter.JoinByIp(addressInput.text);

        private void OnDestroy() => Dispose();
    }
}
