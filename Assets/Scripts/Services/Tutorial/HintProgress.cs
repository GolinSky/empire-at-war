using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Ids of hints the player completed or dismissed; they are never shown again.</summary>
    [Serializable]
    public sealed class HintProgress
    {
        [SerializeField] private List<string> done = new List<string>();

        public bool IsDone(string id) => done.Contains(id);

        /// <returns>False when the id was already recorded.</returns>
        public bool MarkDone(string id)
        {
            if (done.Contains(id)) return false;
            done.Add(id);
            return true;
        }

        public void Clear() => done.Clear();

        public string ToJson() => JsonUtility.ToJson(this, true);

        public static HintProgress FromJson(string json)
        {
            HintProgress progress = new HintProgress();
            JsonUtility.FromJsonOverwrite(json, progress);
            return progress;
        }
    }
}
