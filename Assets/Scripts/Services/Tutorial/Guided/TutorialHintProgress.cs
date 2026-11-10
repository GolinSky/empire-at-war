using System;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>
    /// The guided tutorial always starts from chapter 1: progress lives only for this battle and is never written
    /// to the player's saved hint progress. Instead of writing a file, each save tells the director that a step or
    /// lesson finished.
    /// </summary>
    public sealed class TutorialHintProgress : IHintProgressRepository
    {
        private readonly HintProgress _progress = new HintProgress();

        public event Action Changed;

        public HintProgress Load() => _progress;

        public void Save(HintProgress progress) => Changed?.Invoke();

        public void Reset()
        {
            _progress.Clear();
            Changed?.Invoke();
        }
    }
}
