using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
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
        private SurvivalModel _model;
        private ISurvivalPresenter _presenter;
        private bool _initialized;

        public void SetModel(SurvivalModel model) => _model = model;
        public void AssignPresenter(ISurvivalPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_initialized) return;
            Fill(playerFaction, Enum.GetNames(typeof(FactionType)));
            Fill(enemyFaction, Enum.GetNames(typeof(FactionType)));
            List<string> allies = new List<string> { "None" };
            allies.AddRange(Enum.GetNames(typeof(FactionType)));
            Fill(allyFaction, allies);
            Fill(mapSize, Enum.GetNames(typeof(MapSize)));
            Fill(difficulty, Enum.GetNames(typeof(EnemyAiDifficulty)));
            startButton.interactable = true;
            startButton.onClick.AddListener(_presenter.StartBattle);
            closeButton.onClick.AddListener(_presenter.Close);
            playerFaction.onValueChanged.AddListener(_presenter.SelectPlayer);
            allyFaction.onValueChanged.AddListener(_presenter.SelectAlly);
            enemyFaction.onValueChanged.AddListener(_presenter.SelectEnemy);
            mapSize.onValueChanged.AddListener(_presenter.SelectMapSize);
            difficulty.onValueChanged.AddListener(_presenter.SelectDifficulty);
            superWeapons.onValueChanged.AddListener(_presenter.SelectSuperWeapons);
            _initialized = true;
            Render();
        }

        public void Render()
        {
            playerFaction.SetValueWithoutNotify(Array.IndexOf((FactionType[])Enum.GetValues(typeof(FactionType)), _model.PlayerFaction));
            allyFaction.SetValueWithoutNotify(_model.AllyFaction.HasValue
                ? Array.IndexOf((FactionType[])Enum.GetValues(typeof(FactionType)), _model.AllyFaction.Value) + 1 : 0);
            enemyFaction.SetValueWithoutNotify(Array.IndexOf((FactionType[])Enum.GetValues(typeof(FactionType)), _model.EnemyFaction));
            mapSize.SetValueWithoutNotify((int)_model.MapSize);
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
            mapSize.onValueChanged.RemoveListener(_presenter.SelectMapSize);
            difficulty.onValueChanged.RemoveListener(_presenter.SelectDifficulty);
            superWeapons.onValueChanged.RemoveListener(_presenter.SelectSuperWeapons);
            _initialized = false;
        }

        private static void Fill(TMP_Dropdown dropdown, IEnumerable<string> options)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }

        private void OnDestroy() => Dispose();
    }
}
