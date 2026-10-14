using EmpireAtWar.Ui.Base;
using EmpireAtWar.Controllers.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationHudUi : BaseUi
    {
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text result;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text pauseLabel;
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button newButton;
        private ISimulationHudPresenter _presenter;
        public void Initialize(ISimulationHudPresenter presenter)
        {
            _presenter = presenter;
            pauseButton.onClick.AddListener(presenter.TogglePause); speedButton.onClick.AddListener(presenter.ToggleSpeed);
            restartButton.onClick.AddListener(presenter.Restart); newButton.onClick.AddListener(presenter.New);
        }
        public void Render(SimulationModel model, BattleState state, GameSpeed speed)
        {
            status.text = model.IsSpawning ? "Deploying fleets…" : $"{model.Elapsed:0.0}s   •   Side A: {model.CountA}   •   Side B: {model.CountB}";
            pauseLabel.text = state == BattleState.Paused ? "RESUME" : "PAUSE";
            speedLabel.text = speed == GameSpeed.Fast ? "SPEED 4×" : "SPEED 1×";
            pauseButton.interactable = speedButton.interactable = state == BattleState.Running || state == BattleState.Paused;
            restartButton.interactable = newButton.interactable = state != BattleState.Loading;
            SimulationResult outcome = model.Result;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, outcome == null ? 108 : 204);
            result.gameObject.SetActive(outcome != null);
            if (outcome == null) return;
            string winner = outcome.Outcome == BattleOutcome.PlayerVictory ? "SIDE A WINS" :
                outcome.Outcome == BattleOutcome.EnemyVictory ? "SIDE B WINS" : "DRAW";
            result.text = $"{winner} · {outcome.Elapsed:0.0}s\nA: {outcome.SurvivorsA} survivors · {outcome.HullA:0} hull HP\nB: {outcome.SurvivorsB} survivors · {outcome.HullB:0} hull HP";
        }
        public void Dispose()
        {
            pauseButton.onClick.RemoveListener(_presenter.TogglePause); speedButton.onClick.RemoveListener(_presenter.ToggleSpeed);
            restartButton.onClick.RemoveListener(_presenter.Restart); newButton.onClick.RemoveListener(_presenter.New);
        }
    }
}
