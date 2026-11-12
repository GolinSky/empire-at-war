using EmpireAtWar.Mvc;
using EmpireAtWar.Utils.Random;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [CreateAssetMenu(fileName = "UiSfxData", menuName = "Data/Audio/UI SFX")]
    public sealed class UiSfxData : Data
    {
        [SerializeField] private RandomAudioClips click;
        [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;
        [SerializeField] private RandomAudioClips selectStation;
        [SerializeField, Range(0f, 1f)] private float selectStationVolume = 1f;
        [SerializeField] private RandomAudioClips selectShip;
        [SerializeField, Range(0f, 1f)] private float selectShipVolume = 1f;
        [SerializeField] private RandomAudioClips selectUnit;
        [SerializeField, Range(0f, 1f)] private float selectUnitVolume = 1f;
        [SerializeField] private RandomAudioClips selectEnemy;
        [SerializeField, Range(0f, 1f)] private float selectEnemyVolume = 1f;

        public (AudioClip Clip, float Volume) GetCue(UiSelectSfx? selection)
        {
            var (clips, volume) = selection switch
            {
                null => (click, clickVolume),
                UiSelectSfx.Station => (selectStation, selectStationVolume),
                UiSelectSfx.Ship => (selectShip, selectShipVolume),
                UiSelectSfx.Unit => (selectUnit, selectUnitVolume),
                UiSelectSfx.Enemy => (selectEnemy, selectEnemyVolume),
                _ => throw new System.ArgumentOutOfRangeException(nameof(selection), selection, "Unknown UI selection cue.")
            };
            return (clips.GetRandom(), volume);
        }
    }
}
