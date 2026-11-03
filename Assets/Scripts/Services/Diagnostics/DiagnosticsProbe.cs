#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace EmpireAtWar.Services.Diagnostics
{
    /// <summary>Explicit command-line probes for a disposable development player only.</summary>
    public static class DiagnosticsProbe
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Run()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "--eaw-diagnostics-exception") >= 0)
                throw new InvalidOperationException("Empire at War Diagnostics verification exception.");
            if (Array.IndexOf(arguments, "--eaw-diagnostics-error") >= 0)
                Debug.LogError("Empire at War Diagnostics verification error.");
            if (Array.IndexOf(arguments, "--eaw-diagnostics-native-crash") >= 0)
                UnityEngine.Diagnostics.Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
        }
    }
}
#endif
