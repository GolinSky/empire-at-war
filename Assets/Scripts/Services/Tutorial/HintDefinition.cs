using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>One small step of a lesson: one action, one marked place or button, one short line.</summary>
    [CreateAssetMenu(menuName = "EmpireAtWar/Tutorial/Hint Definition", fileName = "HintDefinition")]
    public sealed class HintDefinition : ScriptableObject
    {
        [Tooltip("Stable id stored in hint progress. Never rename after release.")]
        [SerializeField] private string id;
        [SerializeField] private string title;
        [Tooltip("{0}, {1}… are replaced by the current key binding of each Input Action below. " +
                 "[unit], [hazard], [effects] come from the lesson trigger; [ability] from the selected unit.")]
        [TextArea(2, 4)]
        [SerializeField] private string body;
        [Tooltip("Input action paths such as Camera/Move or Battle/Command.")]
        [SerializeField] private string[] inputActions = new string[0];
        [Tooltip("Fixed controls for UI gestures, such as <Mouse>/leftButton. These do not follow battle bindings.")]
        [SerializeField] private string[] inputControls = new string[0];
        [SerializeField] private TutorialAnchorId anchor;
        [Tooltip("Spotlight target while the anchor is unavailable, such as the unit button to hover before its tooltip opens.")]
        [SerializeField] private TutorialAnchorId anchorFallback;
        [Tooltip("Block pointer input outside the UI spotlight. Keyboard shortcuts remain available.")]
        [SerializeField] private bool blockPointerOutsideSpotlight;
        [Tooltip("Place in the world to mark (and mirror on the minimap).")]
        [SerializeField] private HintTargetId target;
        [Tooltip("Closes the step as done; a step done before it is shown is skipped. None: only Dismiss or auto close.")]
        [SerializeField] private HintConditionId completion;
        [Tooltip("How many the completion needs, for counting completions (Ships Queued).")]
        [Min(1)] [SerializeField] private int completionCount = 1;
        [Tooltip("Start listening for the completion only when the step shows, for actions the player repeats " +
                 "(move, queue a unit).")]
        [SerializeField] private bool listenFromShown;
        [Tooltip("States the step needs; while one is missing its small fix step shows first.")]
        [SerializeField] private HintRequirement[] requires = new HintRequirement[0];
        [Tooltip("Shown only while this state holds when the step comes up; otherwise skipped. None: always shown.")]
        [SerializeField] private HintStateId skipUnless;
        [Tooltip("Shown while the player's units are fighting (attack, abilities). Others wait for a quiet moment.")]
        [SerializeField] private bool showDuringCombat;
        [Tooltip("Information step that closes by itself after this many seconds; 0 keeps it until its completion.")]
        [Min(0f)] [SerializeField] private float autoCloseSeconds;
        [Tooltip("Waiting step (capture, construction): never shows a card; other lessons run and this lesson " +
                 "resumes once the step is done.")]
        [SerializeField] private bool waitSilently;
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
        public TutorialAnchorId AnchorFallback => anchorFallback;
        public bool BlockPointerOutsideSpotlight => blockPointerOutsideSpotlight;
        public HintTargetId Target => target;
        public HintConditionId Completion => completion;
        public int CompletionCount => completionCount;
        public bool ListenFromShown => listenFromShown;
        public IReadOnlyList<HintRequirement> Requires => requires;
        public HintStateId SkipUnless => skipUnless;
        public bool ShowDuringCombat => showDuringCombat;
        public float AutoCloseSeconds => autoCloseSeconds;
        public bool WaitSilently => waitSilently;
        public HintDemo Demo => demo;
        public bool PreviewAnchor => previewAnchor;
    }
}
