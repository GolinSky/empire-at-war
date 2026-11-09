namespace EmpireAtWar.Entities.TutorialUi
{
    public readonly struct HintInputPrompt
    {
        public string ControlPath { get; }
        public string Label { get; }
        public string SeparatorBefore { get; }

        public HintInputPrompt(string controlPath, string label, string separatorBefore)
        {
            ControlPath = controlPath;
            Label = label;
            SeparatorBefore = separatorBefore;
        }
    }
}
