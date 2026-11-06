using IngameDebugConsole;
using UnityEngine;

namespace EmpireAtWar.Services.Diagnostics
{
    public sealed class DebugConsoleService : MonoBehaviour
    {
        [SerializeField] private DebugLogManager consolePrefab;

        private void Awake()
        {
            if (DebugLogManager.Instance == null)
                Instantiate(consolePrefab);
        }
    }
}
