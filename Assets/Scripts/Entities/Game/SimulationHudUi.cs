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
        private (bool Spawning, int Second, int CountA, int CountB)? _displayedStatus;
        private SimulationResult _displayedResult;

        public void Initialize(ISimulationHudPresenter presenter)
        {
            _presenter = presenter;
            _displayedStatus = null;
            _displayedResult = null;
            // The main battle HUD already provides pause and speed controls.
            pauseButton.gameObject.SetActive(false);
            speedButton.gameObject.SetActive(false);
            pauseButton.onClick.AddListener(presenter.TogglePause); speedButton.onClick.AddListener(presenter.ToggleSpeed);
            restartButton.onClick.AddListener(presenter.Restart); newButton.onClick.AddListener(presenter.New);
        }
        public void Render(SimulationModel model, BattleState state, GameSpeed speed)
        {
            (bool Spawning, int Second, int CountA, int CountB) next = model.IsSpawning
                ? (true, 0, 0, 0)
                : (false, (int)model.Elapsed, model.CountA, model.CountB);
            if (_displayedStatus != next)
            {
                _displayedStatus = next;
                status.text = next.Spawning ? "Deploying fleets…" :
                    $"{BattleDuration.Format(next.Second)}   •   Side A: {next.CountA}   •   Side B: {next.CountB}";
            }
            pauseLabel.text = state == BattleState.Paused ? "RESUME" : "PAUSE";
            speedLabel.text = speed == GameSpeed.Fast ? "SPEED 4×" : "SPEED 1×";
            pauseButton.interactable = speedButton.interactable = state == BattleState.Running || state == BattleState.Paused;
            restartButton.interactable = newButton.interactable = state != BattleState.Loading;
            SimulationResult outcome = model.Result;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, outcome == null ? 108 : 204);
            result.gameObject.SetActive(outcome != null);
            if (ReferenceEquals(_displayedResult, outcome)) return;
            _displayedResult = outcome;
            if (outcome == null) return;
            string winner = outcome.Outcome == BattleOutcome.PlayerVictory ? "SIDE A WINS" :
                outcome.Outcome == BattleOutcome.EnemyVictory ? "SIDE B WINS" : "DRAW";
            result.text = $"{winner} · {BattleDuration.Format((int)outcome.Elapsed)}\nA: {outcome.SurvivorsA} survivors · {outcome.HullA:0} hull HP\nB: {outcome.SurvivorsB} survivors · {outcome.HullB:0} hull HP";
        }
        public void Dispose()
        {
            pauseButton.onClick.RemoveListener(_presenter.TogglePause); speedButton.onClick.RemoveListener(_presenter.ToggleSpeed);
            restartButton.onClick.RemoveListener(_presenter.Restart); newButton.onClick.RemoveListener(_presenter.New);
        }
    }
}
