using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>
    /// Player preferences persisted as one versioned JSON document. Holds values only, never Unity object references.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CURRENT_SCHEMA_VERSION = 1;

        [SerializeField] private DisplaySettingsData display = new DisplaySettingsData();
        [SerializeField] private GraphicsSettingsData graphics = new GraphicsSettingsData();
        [SerializeField] private AudioSettingsData audio = new AudioSettingsData();
        [SerializeField] private CameraSettingsData camera = new CameraSettingsData();
        [SerializeField] private InputSettingsData input = new InputSettingsData();

        [SerializeField] private int schemaVersion = CURRENT_SCHEMA_VERSION;

        public int SchemaVersion => schemaVersion;
        public DisplaySettingsData Display => display;
        public GraphicsSettingsData Graphics => graphics;
        public AudioSettingsData Audio => audio;
        public CameraSettingsData Camera => camera;
        public InputSettingsData Input => input;

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>Fields missing from <paramref name="json"/> keep their defaults.</summary>
        public static SettingsData FromJson(string json)
        {
            SettingsData data = new SettingsData();
            JsonUtility.FromJsonOverwrite(json, data);
            data.schemaVersion = CURRENT_SCHEMA_VERSION;
            data.display.Sanitize();
            data.graphics.Sanitize();
            data.audio.Sanitize();
            data.camera.Sanitize();
            return data;
        }

        /// <summary>Field-by-field comparison; cheap enough to run on every edit.</summary>
        public bool Matches(SettingsData other)
        {
            return display.Matches(other.display) && graphics.Matches(other.graphics) &&
                   audio.Matches(other.audio) && camera.Matches(other.camera) && input.Matches(other.input);
        }

        public SettingsData Clone()
        {
            return FromJson(ToJson());
        }

        public void SetDisplay(DisplaySettingsData value)
        {
            display = value;
        }
    }
}
