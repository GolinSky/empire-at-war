using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Views.Game
{
    public sealed class EndGameUi : BaseUi, IEndGameView
    {
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text battlefieldText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text playerFactionText;
        [SerializeField] private TMP_Text enemyFactionText;
        [SerializeField] private TMP_Text playerFleetText;
        [SerializeField] private TMP_Text enemyFleetText;
        [SerializeField] private TMP_Text playerBaseText;
        [SerializeField] private TMP_Text enemyBaseText;
        [SerializeField] private UnityEngine.UI.Button returnToMenuButton;

        public event Action ReturnToMenuRequested = delegate { };

        private void Awake()
        {
            if (outcomeText == null || reasonText == null || battlefieldText == null ||
                objectiveText == null || playerFactionText == null || enemyFactionText == null ||
                playerFleetText == null || enemyFleetText == null || playerBaseText == null ||
                enemyBaseText == null || returnToMenuButton == null)
            {
                throw new InvalidOperationException("EndGameUi requires all serialized view references.");
            }

            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }

        private void OnDestroy()
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
        }

        public void ShowResult(BattleResult result)
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

            battlefieldText.text = result.Planet.ToString().ToUpperInvariant();
            objectiveText.text = fleetObjective ? "Destroy enemy fleet" : "Destroy opponent base";
            playerFactionText.text = result.PlayerFaction.ToString();
            enemyFactionText.text = result.EnemyFaction.ToString();
            playerFleetText.text = result.PlayerShipCount.ToString();
            enemyFleetText.text = result.EnemyShipCount.ToString();
            playerBaseText.text = result.IsPlayerBaseAlive ? "Operational" : "Destroyed";
            enemyBaseText.text = result.IsEnemyBaseAlive ? "Operational" : "Destroyed";
            transform.SetAsLastSibling();
            Show();
            returnToMenuButton.Select();
        }

        private void ReturnToMenu()
        {
            ReturnToMenuRequested.Invoke();
        }
    }
}
