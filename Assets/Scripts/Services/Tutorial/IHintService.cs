using System;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Read side for the hint card. At most one step is visible.</summary>
    public interface IHintService
    {
        event Action<HintDefinition> Shown;

        /// <summary>The visible hint closed. True when the player did the action; false when dismissed or hidden.</summary>
        event Action<HintDefinition, bool> Closed;

        HintDefinition Current { get; }

        /// <summary>Step progress in the lesson, such as "2/5"; empty for one-step lessons and fix steps.</summary>
        string CurrentProgress { get; }

        /// <summary>The place the visible step marks, when it has one.</summary>
        bool TryGetWorldTarget(out Vector3 position);

        /// <summary>Skips the visible step's whole lesson and never shows it again. Throws when no step is visible.</summary>
        void Dismiss();

        /// <summary>Forgets every seen lesson and starts onboarding again, in this match.</summary>
        void Restart();

        /// <summary>Replaces [unit], [hazard], [effects] (lesson trigger) and [ability] (selected unit) placeholders.</summary>
        string ResolveText(string text);
    }
}
