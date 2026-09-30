namespace EmpireAtWar.Services.Settings
{
    public enum SettingsApplyResult
    {
        Committed = 0,
        AwaitingDisplayConfirmation = 1,
        /// <summary>Values are active but could not be written; the draft stays dirty so Apply can be retried.</summary>
        SaveFailed = 2,
    }
}
