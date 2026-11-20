#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace EmpireAtWar.Entities.ShipUi
{
    // TEMPORARY diagnostic for the "select all ships" FPS drop. Logs once per second:
    // per-frame averages of selection-UI script work and Unity canvas rebuild timings.
    // Remove after the investigation.
    public sealed class SelectionPerfProbe : MonoBehaviour
    {
        private const float LOG_INTERVAL = 1f;
        private const string LOG_PREFIX = "[SelectionPerf]";

        public static readonly ProfilerMarker ABILITY_RENDER_MARKER = new ProfilerMarker("SelectionUi.AbilityButton.Render");
        public static readonly ProfilerMarker STATE_UPDATE_MARKER = new ProfilerMarker("SelectionUi.ShipState.Update");
        public static readonly ProfilerMarker SUMMARY_HEALTH_MARKER = new ProfilerMarker("SelectionUi.SummaryHealth");

        private static readonly string[] RECORDED_MARKERS =
        {
            "PlayerLoop",
            "Update.ScriptRunBehaviourUpdate",
            "PostLateUpdate.PlayerUpdateCanvases",
            "Canvas.SendWillRenderCanvases",
            "Canvas.BuildBatch",
            "UGUI.Rendering.UpdateBatches",
            "Canvas.RenderOverlays",
            "SelectionUi.AbilityButton.Render",
            "SelectionUi.ShipState.Update",
            "SelectionUi.SummaryHealth",
        };

        public static int SelectedRings;

        private static int _abilityRenders;
        private static int _abilitySlotsVisited;
        private static int _stateUpdates;
        private static int _stateItemsVisited;
        private static int _summaryHealthCalls;
        private static int _summaryHealthVisited;

        private readonly ProfilerRecorder[] _recorders = new ProfilerRecorder[RECORDED_MARKERS.Length];
        private readonly double[] _totalsNs = new double[RECORDED_MARKERS.Length];
        private readonly StringBuilder _builder = new StringBuilder(512);
        private int _frames;
        private float _elapsed;

        public static void CountAbilityRender(int slots)
        {
            _abilityRenders++;
            _abilitySlotsVisited += slots;
        }

        public static void CountStateUpdate(int items)
        {
            _stateUpdates++;
            _stateItemsVisited += items;
        }

        public static void CountSummaryHealth(int ships)
        {
            _summaryHealthCalls++;
            _summaryHealthVisited += ships;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            GameObject probe = new GameObject(nameof(SelectionPerfProbe));
            probe.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(probe);
            probe.AddComponent<SelectionPerfProbe>();
        }

        private void OnEnable()
        {
            // Touch the custom markers so they are registered before the handle lookup.
            _ = ABILITY_RENDER_MARKER.Handle;
            _ = STATE_UPDATE_MARKER.Handle;
            _ = SUMMARY_HEALTH_MARKER.Handle;
            List<ProfilerRecorderHandle> handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            Dictionary<string, ProfilerRecorderDescription> descriptions =
                new Dictionary<string, ProfilerRecorderDescription>();
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                descriptions[description.Name] = description;
            }
            for (int i = 0; i < RECORDED_MARKERS.Length; i++)
                if (descriptions.TryGetValue(RECORDED_MARKERS[i], out ProfilerRecorderDescription description))
                    _recorders[i] = ProfilerRecorder.StartNew(description.Category, description.Name);
        }

        private void OnDisable()
        {
            for (int i = 0; i < _recorders.Length; i++)
                _recorders[i].Dispose();
        }

        private void Update()
        {
            for (int i = 0; i < _recorders.Length; i++)
                if (_recorders[i].Valid) _totalsNs[i] += _recorders[i].LastValue;
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < LOG_INTERVAL) return;

            float frames = _frames;
            _builder.Clear();
            _builder.Append(LOG_PREFIX)
                .Append(" fps=").Append((frames / _elapsed).ToString("F0"))
                .Append(" rings=").Append(SelectedRings)
                .Append(" | per frame: abilityRenders=").Append((_abilityRenders / frames).ToString("F1"))
                .Append(" abilitySlots=").Append((_abilitySlotsVisited / frames).ToString("F0"))
                .Append(" stateUpdates=").Append((_stateUpdates / frames).ToString("F1"))
                .Append(" stateItems=").Append((_stateItemsVisited / frames).ToString("F0"))
                .Append(" summaryHealthCalls=").Append((_summaryHealthCalls / frames).ToString("F1"))
                .Append(" summaryHealthShips=").Append((_summaryHealthVisited / frames).ToString("F0"))
                .Append(" | ms/frame:");
            for (int i = 0; i < RECORDED_MARKERS.Length; i++)
            {
                _builder.Append(' ').Append(RECORDED_MARKERS[i]).Append('=');
                if (_recorders[i].Valid) _builder.Append((_totalsNs[i] / frames * 1e-6).ToString("F3"));
                else _builder.Append("n/a");
                _totalsNs[i] = 0d;
            }
            Debug.Log(_builder.ToString());

            _frames = 0;
            _elapsed = 0f;
            _abilityRenders = 0;
            _abilitySlotsVisited = 0;
            _stateUpdates = 0;
            _stateItemsVisited = 0;
            _summaryHealthCalls = 0;
            _summaryHealthVisited = 0;
        }
    }
}
#endif
