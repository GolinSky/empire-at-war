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

        [SerializeField] private int schemaVersion = CURRENT_SCHEMA_VERSION;
        [SerializeField] private DisplaySettingsData display = new DisplaySettingsData();
        [SerializeField] private GraphicsSettingsData graphics = new GraphicsSettingsData();
        [SerializeField] private CameraSettingsData camera = new CameraSettingsData();
        [SerializeField] private InputSettingsData input = new InputSettingsData();

        public int SchemaVersion => schemaVersion;
        public DisplaySettingsData Display => display;
        public GraphicsSettingsData Graphics => graphics;
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
            data.camera.Sanitize();
            return data;
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
