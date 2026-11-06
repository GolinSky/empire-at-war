---
category: Refactoring
status: done
created: 2026-11-06
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Empty Interface Members (Simulation launcher, save mode selector, analytics)

## Implementation
- [x] Split `Pause` and `Resume` into `ISimulationSetupPause`. Simulation mode registers the same battle launcher singleton for both contracts. The main menu supplies zero pause handlers.
- [x] Keep `Current` on `ISimulationLauncher` for draft loading and the in-battle hide decision. Cancel resumes only a battle paused by setup. Launch clears the resume flag before restart.
- [x] Replace `ISaveBrowserPresenter.SelectMode` with `SaveBrowserUi.ModeSelected`. The load controller subscribes once, selects the initial tab on open, and unsubscribes on disposal. Save hides both tabs. The view ignores deselection and the current tab.
- [x] Update the existing load browser lifecycle test to switch both toggles. No affected interface fakes were found.
- [x] Retain `DisabledAnalyticsService` for `UNITY_EDITOR && !ANALYTICS_EDITOR`. Four consumers share `IAnalyticsService` regardless of build configuration.

## Verification
- Unity reported scripts up to date with zero compilation errors. Live Editor reflection confirmed the battle pause contract and save mode event.
- Source search found no empty launcher or mode-selection methods in the affected routes. Tests were not run under the checkout override.
- User checks → [[TODOs/User_Acceptance_Queue]]. Shared view event ownership → [[Rules/UI_CODE_BUILD_GUIDE]].
