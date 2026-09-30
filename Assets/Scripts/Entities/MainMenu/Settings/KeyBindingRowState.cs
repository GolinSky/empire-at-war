namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public readonly struct KeyBindingRowState
    {
        public string ActionLabel { get; }
        public string BindingLabel { get; }

        public KeyBindingRowState(string actionLabel, string bindingLabel)
        {
            ActionLabel = actionLabel;
            BindingLabel = bindingLabel;
        }
    }
}
