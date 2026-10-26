using System;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class ShipUnitAudioProfile
    {
        [SerializeField] private SfxProfile engine;
        [SerializeField] private SfxProfile explosion;
        [SerializeField] private SfxProfile alarm;
        [SerializeField] private bool useAlarmOverride;
        [SerializeField] private AudioClip ready;

        public SfxProfile Engine => engine;
        public SfxProfile Explosion => explosion;
        public SfxProfile Alarm => useAlarmOverride ? alarm : null;
        public AudioClip Ready => ready;

        public ShipUnitAudioProfile(SfxProfile engine, SfxProfile explosion, SfxProfile alarm)
        {
            this.engine = engine;
            this.explosion = explosion;
            this.alarm = alarm;
            useAlarmOverride = alarm != null;
        }
    }
}
