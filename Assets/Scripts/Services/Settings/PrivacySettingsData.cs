using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>Controls gameplay analytics consent; crash diagnostics are collected independently.</summary>
    [Serializable]
    public sealed class PrivacySettingsData
    {
        [SerializeField] private bool shareUsageData;
        [SerializeField] private bool hasAnsweredConsent;

        public bool ShareUsageData => shareUsageData;
        public bool HasAnsweredConsent => hasAnsweredConsent;

        public void Answer(bool isShared)
        {
            shareUsageData = isShared;
            hasAnsweredConsent = true;
        }

        public bool Matches(PrivacySettingsData other)
        {
            return shareUsageData == other.shareUsageData && hasAnsweredConsent == other.hasAnsweredConsent;
        }
    }
}
