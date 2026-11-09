using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    [CreateAssetMenu(menuName = "EmpireAtWar/Tutorial/Hint Definition", fileName = "HintDefinition")]
    public sealed class HintDefinition : ScriptableObject
    {
        [Tooltip("Stable id stored in hint progress. Never rename after release.")]
        [SerializeField] private string id;
        [SerializeField] private string title;
        [Tooltip("{0}, {1}… are replaced by the current key binding of each Input Action below. " +
                 "[unit], [hazard], [effects] come from the trigger.")]
        [TextArea(2, 4)]
        [SerializeField] private string body;
        [Tooltip("Input action paths such as Camera/Move or Battle/Command.")]
        [SerializeField] private string[] inputActions = new string[0];
        [Tooltip("Fixed controls for UI gestures, such as <Mouse>/leftButton. These do not follow battle bindings.")]
        [SerializeField] private string[] inputControls = new string[0];
        [SerializeField] private TutorialAnchorId anchor;
        [Tooltip("Event hints only: shows the hint the first time this happens. None: as soon as pacing allows.")]
        [SerializeField] private HintConditionId trigger;
        [Tooltip("Closes the hint as done; a hint completed before it is shown is never shown. None: only Dismiss closes it.")]
        [SerializeField] private HintConditionId completion;
        [Tooltip("Higher shows first when several event hints are ready.")]
        [SerializeField] private int priority;
        [Tooltip("Shown while the player's units are fighting (attack, abilities). Others wait for a quiet moment.")]
        [SerializeField] private bool showDuringCombat;
        [Tooltip("Event hint that may jump ahead of unfinished basics (enemy contact, Survival). Others wait for the basics.")]
        [SerializeField] private bool interruptsBasics;
        [Tooltip("The anchor lives in the station's build panel: until the station is selected, mark the station instead.")]
        [SerializeField] private bool needsStationSelected;
        [Tooltip("Event hints only: waits until this hint is done, so several hints form one lesson step by step.")]
        [SerializeField] private HintDefinition after;
        [Tooltip("Information hint that closes by itself after this many seconds; 0 keeps it until its completion.")]
        [Min(0f)] [SerializeField] private float autoCloseSeconds;
        [Tooltip("Short animation shown on the card.")]
        [SerializeField] private HintDemo demo;
        [Tooltip("Shows a copy of the highlighted HUD element on the card, so the player knows what it looks like.")]
        [SerializeField] private bool previewAnchor;

        public string Id => id;
        public string Title => title;
        public string Body => body;
        public IReadOnlyList<string> InputActions => inputActions;
        public IReadOnlyList<string> InputControls => inputControls;
        public TutorialAnchorId Anchor => anchor;
        public HintConditionId Trigger => trigger;
        public HintConditionId Completion => completion;
        public int Priority => priority;
        public bool ShowDuringCombat => showDuringCombat;
        public bool InterruptsBasics => interruptsBasics;
        public bool NeedsStationSelected => needsStationSelected;
        public HintDefinition After => after;
        public float AutoCloseSeconds => autoCloseSeconds;
        public HintDemo Demo => demo;
        public bool PreviewAnchor => previewAnchor;
    }
}
