using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Survival
{
    public sealed class SurvivalUi : BaseUi
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Dropdown playerFaction;
        [SerializeField] private TMP_Dropdown allyFaction;
        [SerializeField] private TMP_Dropdown enemyFaction;
        [SerializeField] private TMP_Dropdown mapSize;
        [SerializeField] private TMP_Dropdown difficulty;
        [SerializeField] private Toggle superWeapons;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text readinessText;
        [SerializeField] private TMP_Text matchSummaryText;
        [SerializeField] private CanvasGroup startContent;
        [SerializeField] private Button inviteButton;
        [SerializeField] private LobbyModeSwitchView lobbyModeSwitch;
        [SerializeField] private SurvivalLobbyMemberView[] memberViews;
        private SurvivalModel _model;
        private ISurvivalPresenter _presenter;
        private MapSize[] _mapSizes = (MapSize[])Enum.GetValues(typeof(MapSize));
        private bool _initialized;

        /// <summary>Online lobby route only.</summary>
        public event Action InviteRequested;

        /// <summary>Online lobby route only.</summary>
        public LobbyModeSwitchView LobbyModeSwitch => lobbyModeSwitch;


        public void SetModel(SurvivalModel model) => _model = model;
        public void AssignPresenter(ISurvivalPresenter presenter) => _presenter = presenter;

        /// <summary>Map sizes offered in the dropdown; call before <see cref="Initialize"/>.</summary>
        public void SetMapSizes(MapSize[] sizes) => _mapSizes = sizes;

        public void Initialize()
        {
            if (_initialized) return;
            Fill(playerFaction, Enum.GetNames(typeof(FactionType)));
            Fill(enemyFaction, Enum.GetNames(typeof(FactionType)));
            List<string> allies = new List<string> { "None" };
            allies.AddRange(Enum.GetNames(typeof(FactionType)));
            Fill(allyFaction, allies);
            Fill(mapSize, Array.ConvertAll(_mapSizes, size => size.ToString()));
            Fill(difficulty, Enum.GetNames(typeof(EnemyAiDifficulty)));
            startButton.interactable = true;
            startButton.onClick.AddListener(_presenter.StartBattle);
            closeButton.onClick.AddListener(_presenter.Close);
            playerFaction.onValueChanged.AddListener(_presenter.SelectPlayer);
            allyFaction.onValueChanged.AddListener(_presenter.SelectAlly);
            enemyFaction.onValueChanged.AddListener(_presenter.SelectEnemy);
            mapSize.onValueChanged.AddListener(SelectMapSize);
            difficulty.onValueChanged.AddListener(_presenter.SelectDifficulty);
            superWeapons.onValueChanged.AddListener(_presenter.SelectSuperWeapons);
            inviteButton.onClick.AddListener(RaiseInviteRequested);
            SetInviteAvailable(false);
            lobbyModeSwitch.Initialize();
            foreach (SurvivalLobbyMemberView view in memberViews) view.Hide();
            _initialized = true;
            Render();
        }

        public void Render()
        {
            playerFaction.SetValueWithoutNotify(Array.IndexOf((FactionType[])Enum.GetValues(typeof(FactionType)), _model.PlayerFaction));
            allyFaction.SetValueWithoutNotify(_model.AllyFaction.HasValue
                ? Array.IndexOf((FactionType[])Enum.GetValues(typeof(FactionType)), _model.AllyFaction.Value) + 1 : 0);
            enemyFaction.SetValueWithoutNotify(Array.IndexOf((FactionType[])Enum.GetValues(typeof(FactionType)), _model.EnemyFaction));
            mapSize.SetValueWithoutNotify(Array.IndexOf(_mapSizes, _model.MapSize));
            difficulty.SetValueWithoutNotify((int)_model.Difficulty);
            superWeapons.SetIsOnWithoutNotify(_model.SuperWeapons);
        }

        public void Dispose()
        {
            if (!_initialized) return;
            startButton.onClick.RemoveListener(_presenter.StartBattle);
            closeButton.onClick.RemoveListener(_presenter.Close);
            playerFaction.onValueChanged.RemoveListener(_presenter.SelectPlayer);
            allyFaction.onValueChanged.RemoveListener(_presenter.SelectAlly);
            enemyFaction.onValueChanged.RemoveListener(_presenter.SelectEnemy);
            mapSize.onValueChanged.RemoveListener(SelectMapSize);
            difficulty.onValueChanged.RemoveListener(_presenter.SelectDifficulty);
            superWeapons.onValueChanged.RemoveListener(_presenter.SelectSuperWeapons);
            inviteButton.onClick.RemoveListener(RaiseInviteRequested);
            lobbyModeSwitch.Dispose();
            _initialized = false;
        }

        public void SetInviteAvailable(bool available) => inviteButton.gameObject.SetActive(available);

        /// <summary>
        /// Online lobby: settings are the host's; the start button starts (host) or toggles ready (client); one chip
        /// per member (avatar, name, ready).
        /// </summary>
        public void RenderLobby(bool canEditSettings, string startText, bool startInteractable, string readiness,
            string summary, IReadOnlyList<SurvivalLobbyMemberState> members)
        {
            for (int i = 0; i < memberViews.Length; i++)
            {
                if (i < members.Count) memberViews[i].Render(members[i]);
                else memberViews[i].Hide();
            }

            playerFaction.interactable = canEditSettings;
            allyFaction.interactable = canEditSettings;
            enemyFaction.interactable = canEditSettings;
            mapSize.interactable = canEditSettings;
            difficulty.interactable = canEditSettings;
            superWeapons.interactable = canEditSettings;
            startLabel.text = startText;
            startButton.interactable = startInteractable;
            startContent.alpha = startInteractable ? 1f : 0.38f;
            readinessText.text = readiness;
            matchSummaryText.text = summary;
        }

        private void RaiseInviteRequested() => InviteRequested?.Invoke();

        private void SelectMapSize(int index) => _presenter.SelectMapSize(_mapSizes[index]);

        private static void Fill(TMP_Dropdown dropdown, IEnumerable<string> options)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }

        private void OnDestroy() => Dispose();
    }
}
