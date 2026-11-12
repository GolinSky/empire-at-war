---
category: Bugs
status: todo
created: 2026-11-12
---
# Carrier and Squadron Rebalance — Audit

## Goal

- Explain fresh-match Republic/Rebellion air presence reported 2026-11-12; parent → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]].

## Evidence

- Original balance commit `bf632b832` → 2026-11-11 13:36:44 +0200. The two battle logs below postdate it.
- Telemetry directory → `C:/Users/golin/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/`.
- `Battle_20261112_132136.csv` → 533 unscaled seconds; Republic regular squadrons killed 57 CIS squadrons; CIS regular squadrons killed 14 Republic squadrons. All weapon sources: CIS lost 101, Republic 76; ships/stations credited 44 CIS and 58 Republic kills, Jango credited 4 Republic kills.
- `Battle_20261111_204932.csv` → 831 unscaled seconds; Rebel regular squadrons killed 71 Empire squadrons; Empire regular squadrons killed 32 Rebel squadrons. All sources: Empire lost 143, Rebels 117; ships/stations credited 69 Empire and 85 Rebel kills, Han Solo credited 3 Empire kills.
- Counts are whole-squadron final blows: `WeaponComponent.CommitImpact` records `attackData.TargetHealth.IsDestroyed`; `SquadronHealthComponent.IsDestroyed` reflects the model, which dies at `AliveCount == 0`. Not individual craft kills; not normalized duel results.
- Logs contain all three regular CIS roles and Empire fighter/bomber/interceptor firing. They establish launch/combat participation and lopsided squadron exchanges, not initial spawn totals, peak live counts, or causal shares.
- Latest `Battle_20261112_145112.csv` includes a Republic-owned Malevolence credited with 57 CIS squadron kills; that match is not clean evidence for Republic squadron superiority.

## Implementation

- Read-only serialized comparison: 29 original carrier cap/stock/timer sets match; 17 squadron count/role/hull/shield/recovery/speed sets match. No new weapon DPS rebake, tests or gameplay run performed.
- Current deployable one-of-each roster totals (active/stock): CIS 66/264, Empire 40/120, Republic 38/114, Rebellion 38/73.
- Outside original table: Rebel MC80 Justice +3 active, MC80 Freedom +1, Mediator +1, Remonda +2; Imperial Interdictor +1. Matching view prefabs contain bound hangars and launch points. These totals are roster sums, not equal-cost fleets.

### Survival

| Full squadron | Starting hull + shield HP | Intact squad shield recovery HP/s |
|---|---:|---:|
| Vulture | 196 | 0 |
| TIE Fighter | 294 | 0 |
| ARC-170 | 550 | 15 |
| X-Wing | 725 | 20 |
| Belbullab-22 | 172 | 2 |
| TIE Interceptor | 256 | 0 |
| V-Wing | 420 | 10 |
| A-Wing | 750 | 18 |

- Raw HP totals understate shield advantage: fighter lasers do 0.5× raw damage to shields versus 1.5× to fighter hull; interceptor lasers also do 0.5× to shields. See `DamageMatrixData.asset:340–393`, `SquadronHealthModel.ApplyDamage`.
- Recovery continues during combat; `SquadronHealthComponent.Tick` gates shield recovery on its timer/suppression, not a time-since-hit cooldown. V-Wings and Republic Y-Wings also repair surviving hull.
- These are source-derived durability advantages, not simulated time-to-kill. Damage, accuracy, maneuvering, research, abilities and supporting ships affect actual exchanges.

### Launch throughput

- One automatic squadron per carrier per interval: CIS 6 s, Empire 10 s, Republic/Rebellion 8 s; initial delay 4 s.
- Lossless example: Lucrehulk reaches 18 active squads at 106 s; Liberator reaches 3 at 20 s. Larger maximum capacity does not give a proportional opening launch rate.
- `HangarModel.SquadronLost:173–176` resets the **shared** cooldown to at least the launch interval on every squadron death. Deaths more frequent than that interval can continually delay every bay, even with unused stock and active capacity.
- `FindLaunchBay:136–148` retains each launch in a 60 s recent-launch list; squadron death does not remove it. Early deaths can leave bays empty until recent launches age out.
- `EnemyProductionStrategy.CountHangarSquadrons:587–599` counts reserved carrier count × theoretical hangar capacity, not actual surviving wings or remaining stock. Normal direct squadron purchase room is one per two ships, including those theoretical hangar counts; critical-station/no-ship fallbacks can bypass it.

### Quantity coverage

- Stations were explicitly excluded by the plan: every faction gets 2 active / 6 stock, with 1 s initial delay and 3 s interval. Rebel stations launch A-Wings; Republic ARC-170; Empire TIE Fighter; CIS Vulture. Equal quantity inherits the new unequal quality.
- Ordinary purchased squads retain capacity 1 and limit 10. ARC-170 costs 375 / 8 s build versus Vulture 350 / 8 s and TIE Fighter 300 / 10 s. Rebel bomber costs 500 / 15 s versus TIE Bomber 550 / 17 s.
- Carrier-only reductions therefore do not guarantee smaller faction-wide live air forces.

## Decision

- Reopen parent balance plan; keep original implementation and historic verification evidence.
- Why → existing battle telemetry supports stronger Republic/Rebel squadrons winning the air exchange; launch pacing and recovery constrain weaker factions despite larger reserve/cap figures.
- Avoid → blindly increasing stock or assuming a faction-specific spawn failure. No faction gate was found in the shared automatic launch path.
- Remaining → approved target retuning and timeline measurements of launched/live/lost/replaced squads in equal-cost matchups. No gameplay values changed during this diagnostic task.

## Follow-up implementation — 2026-11-12

- The findings above describe the pre-fix audit. Implemented: six CIS carrier intervals 1 s; `SquadronLost` only decrements active count; recent-launch window 5 s; stations active/stock CIS 5/20, Empire 4/12, Republic 3/9, Rebellion 2/4.
- Global `SquadronLaunchSlots` remains limited to 10 launches/s across the battle without a fair queue. Ten or more continuously launching CIS carriers plus other producers can saturate it; nominal 1 s per carrier is not guaranteed under contention.
- New station complements have raw starting hull+shield pools CIS 980, Empire 1,176, Republic 1,650, Rebellion 1,500. Recovery and shield resistance still favor Republic/Rebellion; quantity ordering alone does not guarantee air superiority.
- Non-CIS automatic launches remain interval-limited (Empire 10 s, Republic/Rebellion 8 s), even though the recent-launch window is 5 s.
- Existing station saves restore their old remaining reserve and cooldown. Fresh matches use new stock. No station stock migration added; this preserves spent-stock saves rather than inventing replenishment.
- Existing AI purchase room counts reserved carriers × theoretical hangar capacity, including ships with destroyed bays or exhausted stock. Further work must use appropriate live/pending air strength to avoid suppressing useful replacements.
- Tests authored/updated, not executed. No quality, direct-purchase, global-throttle or save migration changes in this follow-up.

## Empire versus CIS — 2026-11-12 15:54

- Sources: `Battle_20261112_155400.csv`, `Loading_20261112_155240_ai.csv`, `Loading_20261112_155240_ai_orders.csv` in the telemetry directory above. Combat duration 1,170 unscaled seconds (19:30). Price update in this task postdates the battle.
- Regular-squadron final blows against regular squads: Empire 319, CIS 199. Damage to regular squads: Empire 57,782, CIS 53,255 shield+hull HP. All-weapon regular squad losses: Empire 398, CIS 536.
- Leading squadron killers: TIE Interceptor 141, TIE Fighter 137; Vulture 116, Belbullab-22 59, Droid Bomber 24. Boba adds 24 CIS squad kills outside the regular exchange.
- Ships/stations killed 193 CIS squadrons and 199 Imperial squadrons. Leading Imperial AA: ISD I 61, Acclamator Assault 32, Gladiator 23. Leading CIS AA: station 38, quad-laser satellites 32, Dispatcher 24.
- Regular squad damage to ships/structures: Empire 61,500 HP / 6 final blows, CIS 37,724 HP / 2 final blows. Damage totals sum rounded CSV shield/hull losses; these are mixed-fleet outcomes, not controlled duels.
- CIS AI purchased two Vultures at old price 350; logged times 53.0 and 128.2. Carrier purchases: Bulwark 10, Providence 7, Wavecrest 5, Invincible 4, Captor 1, Lucrehulk 0. AI companion covers CIS only.
- Final AI state: no combat ships or living production station. Late samples select Lucrehulk with population 114/120, capacity cost 25, funds 460–654 versus price 15,000; possible production-selection issue, not enough adjacent samples to establish a prolonged stall.
- Conclusion: CIS air support participated heavily; Empire achieved more squadron kills. Price changes alone may have limited effect because CIS bought only two regular squads. Logs lack launch requests, wait time and live/peak counts; no global launch bottleneck is established.
