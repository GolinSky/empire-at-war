using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using EmpireAtWar.Components.Ui.Tooltip;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.EndGameUi
{
    public sealed class EndGameUi : BaseUi
    {
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text battlefieldText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text battleDurationText;
        [SerializeField] private ScrollRect playerLosses;
        [SerializeField] private ScrollRect enemyLosses;
        [SerializeField] private EndGameLossRow lossRowTemplate;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private TooltipHoverView tooltipHover;

        private IEndGamePresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(IEndGamePresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("EndGameUi presenter can only be assigned once.");
            _presenter = presenter;
        }

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();

        public void ShowResult(BattleResult result,
            IReadOnlyList<(string Name, Sprite Icon, int Count)> friendlyLosses,
            IReadOnlyList<(string Name, Sprite Icon, int Count)> hostileLosses)
        {
            outcomeText.text = result.Outcome switch
            {
                BattleOutcome.PlayerVictory => "VICTORY",
                BattleOutcome.EnemyVictory => "DEFEAT",
                BattleOutcome.Draw => "DRAW",
                _ => throw new ArgumentOutOfRangeException(nameof(result.Outcome))
            };

            bool fleetObjective = result.VictoryCondition == BattleVictoryCondition.DestroyEnemyFleet;
            reasonText.text = result.Outcome switch
            {
                BattleOutcome.PlayerVictory => fleetObjective
                    ? "The enemy fleet has been destroyed." : "The enemy base has been destroyed.",
                BattleOutcome.EnemyVictory => fleetObjective
                    ? "Your fleet has been destroyed." : "Your base has been destroyed.",
                _ => fleetObjective ? "Both fleets have been destroyed." : "Both bases have been destroyed."
            };

            battlefieldText.text = PlanetSelectionPopupUi.GetPlanetName(result.Planet).ToUpperInvariant();
            battleDurationText.text = $"Battle time  <color=#6DB8E2>{BattleDuration.Format(result.DurationSeconds)}</color>";
            objectiveText.text = fleetObjective ? "Destroy enemy fleet" : "Destroy opponent base";
            if (result.Mode == GameMode.Survival)
            {
                objectiveText.text = "Survive all five waves";
                reasonText.text = result.Outcome == BattleOutcome.PlayerVictory
                    ? "All five waves have been defeated." : "Your station has been destroyed.";
            }
            RenderLosses(playerLosses, friendlyLosses);
            RenderLosses(enemyLosses, hostileLosses);
            transform.SetAsLastSibling();
            Show();
            returnToMenuButton.Select();
        }

        private void RenderLosses(ScrollRect list, IReadOnlyList<(string Name, Sprite Icon, int Count)> losses)
        {
            foreach (Transform child in list.content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (losses.Count == 0)
                AddRow(list.content, "No units lost", null, 0);
            else
                foreach (var loss in losses)
                    AddRow(list.content, loss.Name, loss.Icon, loss.Count);

            LayoutRebuilder.ForceRebuildLayoutImmediate(list.content);
            list.verticalNormalizedPosition = 1f;
        }

        private void AddRow(RectTransform parent, string name, Sprite icon, int count)
        {
            EndGameLossRow row = Instantiate(lossRowTemplate, parent);
            row.Show(name, icon, count);
            row.gameObject.SetActive(true);
        }

        private void ReturnToMenu()
        {
            _presenter.ReturnToMenu();
        }
    }
}
