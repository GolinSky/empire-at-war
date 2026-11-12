using System;
using EmpireAtWar.Entities.TutorialUi;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    /// <summary>
    /// Coach cards on the tutorial setup screen, on the battle hint card: faction, then colour, then Start.
    /// Picking a faction or colour completes its card; a step done early is skipped. Dismiss skips a card and
    /// Turn off ends the cards for this visit.
    /// </summary>
    public sealed class TutorialSetupHintsController : UiController, IHintPresenter, ILateTickable, ILateDisposable
    {
        // Long enough for the card's done mark.
        private const float STEP_GAP_SECONDS = 0.6f;
        private const int NO_STEP = -1;

        private static readonly TutorialSetupHint[] STEPS =
        {
            new TutorialSetupHint(TutorialAnchorId.SetupFaction, "Choose your faction",
                "Pick the faction you command. Your opponent takes its rival."),
            new TutorialSetupHint(TutorialAnchorId.SetupColor, "Choose your colour",
                "Pick the colour of your fleet on the battlefield and the minimap."),
            new TutorialSetupHint(TutorialAnchorId.SetupStart, "Start the battle",
                "The other settings are fixed for the tutorial. Press Start to begin.")
        };

        private readonly TutorialAnchorTracker _anchors = new TutorialAnchorTracker();
        private readonly bool[] _done = new bool[STEPS.Length];
        private HintUi _ui;
        private bool _isRunning;
        private int _step = NO_STEP;
        private float _nextStepAt;

        public TutorialSetupHintsController(IUiService uiService) : base(uiService)
        {
        }

        public void Begin()
        {
            if (_ui == null)
            {
                _ui = CreateUi<HintUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }

            _ui.Show();
            Array.Clear(_done, 0, _done.Length);
            _isRunning = true;
            _step = NO_STEP;
            _nextStepAt = Time.unscaledTime;
        }

        public void End()
        {
            if (!_isRunning) return;
            _isRunning = false;
            CloseStep(false);
        }

        /// <summary>The player used the control <paramref name="anchor"/> marks.</summary>
        public void Complete(TutorialAnchorId anchor)
        {
            int index = Array.FindIndex(STEPS, step => step.Anchor == anchor);
            _done[index] = true;
            if (_isRunning && index == _step) CloseStep(true);
        }

        public void Dismiss() => CloseStep(false);

        public void TurnOffHints() => End();

        public void LateTick()
        {
            if (!_isRunning) return;
            if (_step == NO_STEP)
            {
                if (Time.unscaledTime >= _nextStepAt) OpenNextStep();
                return;
            }

            bool hasAnchor = _anchors.Track(STEPS[_step].Anchor, TutorialAnchorId.None, out Rect screenRect);
            _ui.SetSpotlight(hasAnchor, screenRect);
        }

        public void LateDispose()
        {
            _anchors.Clear();
            if (_ui != null) _ui.Dispose();
            _ui = null;
        }

        private void OpenNextStep()
        {
            int next = Array.IndexOf(_done, false);
            if (next < 0)
            {
                _isRunning = false;
                return;
            }

            _step = next;
            TutorialSetupHint hint = STEPS[next];
            _ui.OpenHint(hint.Title, hint.Body, $"{next + 1}/{STEPS.Length}");
        }

        private void CloseStep(bool completed)
        {
            if (_step == NO_STEP) return;
            _done[_step] = true;
            _step = NO_STEP;
            _nextStepAt = Time.unscaledTime + STEP_GAP_SECONDS;
            _anchors.Clear();
            _ui.CloseHint(completed);
        }
    }
}
