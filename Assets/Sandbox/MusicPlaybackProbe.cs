using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using UnityEngine;

public sealed class MusicPlaybackProbe : MonoBehaviour
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioSource sourceTemplate;
    [SerializeField] private MusicAudioData musicData;
    private MusicService _service;
    private AudioSource _source;
    private int _index;

    private IEnumerator Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        _service = new MusicService(null, new ProbeAssets(sourceTemplate, musicData), null, new AudioService());
        _source = (AudioSource)typeof(MusicService).GetField("_audioSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_service);
        SetClip(clips[0]);
        typeof(MusicService).GetMethod("PlayNext", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_service, null);
        for (_index = 0; _index < clips.Length; _index++)
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while ((_source.clip != clips[_index] || _source.volume < 0.5f || _source.timeSamples == 0) && Time.realtimeSinceStartup < deadline)
            {
                _service.Tick();
                yield return null;
            }
            Log("START");
            if (_source.clip != clips[_index] || _source.volume < 0.5f || _source.timeSamples == 0)
            {
                Debug.LogError("MUSIC_PROBE FAILED");
                Application.Quit(1);
                yield break;
            }
            if (_index + 1 == clips.Length) break;
            SetClip(clips[_index + 1]);
            _source.timeSamples = _source.clip.samples - _source.clip.frequency / 4;
            Log("SEEK_END");
            while (_source.clip != clips[_index + 1] && Time.realtimeSinceStartup < deadline)
            {
                _service.Tick();
                yield return null;
            }
            Log("TRANSITION");
        }
        Debug.Log("MUSIC_PROBE PASSED " + clips.Length);
        Application.Quit(0);
    }

    private void SetClip(AudioClip clip) => typeof(MusicService).GetField("_clips", BindingFlags.Instance | BindingFlags.NonPublic)
        .SetValue(_service, new List<AudioClip> { clip });

    private void Log(string phase) => Debug.Log($"MUSIC_PROBE {phase} index={_index} clip={_source.clip.name} load={_source.clip.loadState} playing={_source.isPlaying} virtual={_source.isVirtual} volume={_source.volume} time={_source.timeSamples} samples={_source.clip.samples} frequency={_source.clip.frequency} listenerPause={AudioListener.pause}");

    private sealed class ProbeAssets : IAssetService
    {
        private readonly AudioSource _source;
        private readonly MusicAudioData _data;
        public ProbeAssets(AudioSource source, MusicAudioData data) { _source = source; _data = data; }
        public TSource Load<TSource>(string key) where TSource : UnityEngine.Object => (TSource)(UnityEngine.Object)_data;
        public TComponent LoadComponent<TComponent>(string key) where TComponent : Component => (TComponent)(Component)_source;
        public GameObject LoadPrefab(string key) => throw new NotSupportedException();
    }
}
