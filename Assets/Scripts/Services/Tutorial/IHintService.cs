using System;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Read side for the hint card. At most one hint is visible.</summary>
    public interface IHintService
    {
        event Action<HintDefinition> Shown;

        /// <summary>The visible hint closed. True when the player did the action; false when dismissed or hidden.</summary>
        event Action<HintDefinition, bool> Closed;

        HintDefinition Current { get; }

        /// <summary>1-based position of the visible hint in the basics track; 0 for event hints.</summary>
        int CurrentBasicNumber { get; }

        int BasicCount { get; }

        /// <summary>Where the visible hint's trigger happened, when it has a place in the world.</summary>
        bool TryGetWorldTarget(out Vector3 position);

        /// <summary>Closes the visible hint and never shows it again. Throws when no hint is visible.</summary>
        void Dismiss();
    }
}
