using EmpireAtWar.Services.Settings;

namespace EmpireAtWar.Services.Audio
{
    /// <summary>Plays draft volumes live while the settings screen edits them; Apply or Discard sets the final values.</summary>
    public interface IAudioSettingsPreview
    {
        void Preview(AudioSettingsData audio);
    }
}
