using EmpireAtWar.Services.Tutorial;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    /// <summary>One coach card on the tutorial setup screen, pointing at one control.</summary>
    public readonly struct TutorialSetupHint
    {
        public TutorialSetupHint(TutorialAnchorId anchor, string title, string body)
        {
            Anchor = anchor;
            Title = title;
            Body = body;
        }

        public TutorialAnchorId Anchor { get; }
        public string Title { get; }
        public string Body { get; }
    }
}
