---
type: external-reference
record_state: user-provided
created: 2026-11-01
system:
  - ai
source: C:/Users/golin/.codex/attachments/7f7a27db-03d3-4f4d-bb4d-ce490862b384/Pasted
  text.txt
---
# EaW Global AI — Original User Attachment

- Original attachment preserved below, including its original links and formatting artifacts.
- Provenance: user-provided research; mod claims and proposed architecture are not automatically verified vanilla behavior.
- Checked local EaW XML/mod evidence and current implementation comparison → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].
- Implementation history and latest battle assessment → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review]].

---

# Star Wars: Empire at War — Global AI architecture

I researched the original Empire at War AI framework and modifications used by major mods, including Awakening of the Rebellion and Remake.

The most important finding: EaW uses a goal-driven AI system, combining priority evaluation, resource budgets, scripted plans, and finite-state machines. It is not simply one large decision tree.

Its core architecture is:

AI Player / Commander

Central decision-making controller

Perceptions

Analyze game state

Budgets

Allocate resources

Goals

Select objectives

Plans

Execute strategies

Task Forces + FreeStore

Assign and control available units

Unit AI

Movement, targeting, abilities, combat

This is a simplified representation of the documented framework, not an exact engine class diagram.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+2



## 1. How decisions work

The AI follows this general process:

1. Perception: Evaluate enemy forces, friendly units, resource availability, map control, threats and objectives.
2. Budget: Determine how much to invest in offensive, defensive, economic and other categories.
3. Goal: Score possible objectives and select eligible high-value goals.
4. Plan: Select a scripted procedure to accomplish each activated goal.
5. TaskForce: Assemble or build an appropriate group of units.
6. Execution: Move, attack, defend, retreat, and report success or failure.
7. Re-evaluation: Adjust goals as the situation changes.

Goals are prioritized independently within their budget categories. Plans may complete, fail or be reconsidered; failures can reduce the desirability of repeating an objective.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+2



## 2. Two levels of global AI

EaW separates strategic decisions from tactical battles.

Galactic AI — campaign commander

Manages planet conquest, economics, research, fleet production, defensive deployments and invasion routes.

Tactical AI — battle commander

Manages objectives inside space or ground battles, assigns task forces, chooses targets and handles reinforcements.

Both use the goal/plan framework, but tactical budgets primarily enable or disable goal categories rather than limiting spending like Galactic Conquest budgets.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+1



## 3. Tactical AI — how it controls ships

This is the most relevant part for a single-map space RTS.

A typical command flow looks like this:

Evaluate battlefield

Attack enemy

Offensive goal

Defend station

Defensive goal

Capture mine

Map control

Build units

Production goal

Score goals and assign task forces

Consider danger, importance and available strength

Execute orders

Attack-move, defend, capture, reinforce

The exact scoring depends on the installed AI perceptions and plans. Vanilla AI supports tactical threat classifications, task-force composition requirements and combat-power comparisons called Contrast.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+1



### How task forces work

EaW can define a task force using unit categories, specific unit types, minimum unit count and minimum total combat power.

For example, a hypothetical attack plan could request:

```
Goal: Destroy enemy capital ship

Required force:
  1 cruiser
  2 frigates
  2 corvettes

Minimum combat power: 1200

Execution:
  Assemble units
  Move toward target
  Attack
  Evaluate result
```

The game can gather existing units from its FreeStore and build additional units when the plan permits it. FreeStore is important: it handles units not currently committed to active AI plans.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+1



## 4. What the mods improve

[Updated Galactic Conquest Map and GUI image - Republic at War mod for Star Wars: Empire at War: Forces of Corruption - ModDB](https://images.openai.com/static-rsc-4/TAPuVhDQQni8zMPECABIwS6vCJhk5NR5WNNRt8WNYGWxRiOsa4BJ-PVNs7em38AlZPzWTGGR1qG84IP3fzdtVyxQc4F4pm4zTJkqLBuC3YDq3NFbM3-KDial3gIZHd6I8VETCz5b2r9pSIoSLV5yMrqUt3AbfQKxVS0QAyyboy0?purpose=inline)

Awakening of the Rebellion

One of the best documented examples. Adds Rampage multi-planet offensives, stranded-fleet recovery, more consistent raids, target-specific force valuation and smarter invasion behavior.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+1



[New holographic galactic conquest effect image - ModDB](https://images.openai.com/static-rsc-4/UziXJ61UwVXo7i1JQuOiF2WnunSCsbFx9MWSs3Y3XgEcTxD-nBV7qew9QaOAf4wBw7Xit97nT9fuc05yE2Rt3kR6un_u54AdmrRrjR1cRo8mQiTl9mb7rirfTLOdIAlQOmHjdtkb1inDrk2ANGmcKONE96K2XFDj66yJKgUMyhU?purpose=inline)

Empire at War: Remake

The 5.0 description emphasizes a rebuilt AI for skirmish and the galactic map. It uses curated fleet compositions and improves decision-making, while higher difficulties still provide economic and production advantages.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

[OFFICIAL]



[Eriadu Authority image - Empire at War Expanded: Thrawn's Revenge mod for Star Wars: Empire at War: Forces of Corruption - ModDB](https://images.openai.com/static-rsc-4/2Koe6Imjs6mleWgxhYuT7fE3RPX8rkSR5xEaOC6gFkJfV3DOU9HlnaGxKThEqx45mEGIZ51m3KZ3vPmTfe9E2JImd9KDasebsailcRqAZuCVP7N1XHCbUsPbGnbzQ7Vzp-j5m5NXngIYjfQHOjs_vjHc34xSJJul-qRTZXTNwg8?purpose=inline)

Thrawn's Revenge

Focuses on large multi-faction campaigns, different starting conditions and era-based progression. Difficulty adjusts economic and construction advantages; its complete internal AI decision logic is less publicly documented.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://steamcommunity.com\&sz=32)

Steam Community+1



Another useful source is the [Stargate: Empire at War development changelog](https://sgmg2022.stargate-eaw.de/mod_versions/?utm_source=chatgpt.com). It documents tactical AI improvements such as prioritizing resource mines, raising production desire when credits are available, defensive fleet concentration and separate space/land FreeStore scripts.

## 5. Architecture I would use in your Unity RTS

This is my proposed design, derived from EaW's documented principles—not a claim about the original engine's internal classes.

```
GlobalAIController
│
├── BattlefieldAnalyzer
│   ├── FriendlyForces
│   ├── EnemyForces
│   ├── ThreatMap
│   ├── EconomyState
│   └── ObjectivesState
│
├── StrategyEvaluator
│   ├── OffensiveEvaluator
│   ├── DefensiveEvaluator
│   ├── EconomyEvaluator
│   └── ReinforcementEvaluator
│
├── GoalManager
│   ├── ScoreGoals
│   ├── SelectGoals
│   ├── ResolveConflicts
│   └── TrackFailures
│
├── PlanManager
│   ├── AttackPlan
│   ├── DefendPlan
│   ├── CapturePlan
│   └── RetreatPlan
│
├── TaskForceManager
│   ├── AssembleForces
│   ├── AssignUnits
│   └── ReleaseUnits
│
└── ProductionManager
    ├── SelectUnits
    └── QueueProduction
```

For goal selection, use a scoring system. For example:

```
AttackGoalScore =
    TargetStrategicValue
  + EnemyVulnerability
  + AvailableForceAdvantage
  - ExpectedLosses
  - TravelCost
  - FrontlineRisk
```

These weights would be yours to balance, not vanilla EaW's exact formula.

For your space battles, this enables behavior such as prioritizing mining zones early, committing cruisers to a station assault, keeping corvettes for anti-fighter defense, and reallocating free ships when a defensive position is attacked.

Important design rule: the global commander should issue intent (attack that station, hold this mining zone), while individual ship controllers handle navigation, weapon selection, hardpoint targeting and local combat behavior.

## 6. Where to examine the original implementation

The most useful documented EaW files are:

```
Data/XML/AI/Players/
Data/XML/AI/Templates/
Data/XML/AI/Goals/
Data/XML/AI/GoalFunctions/
Data/XML/AI/PerceptualEquations/

Data/Scripts/AI/
Data/Scripts/AI/SpaceMode/
Data/Scripts/FreeStore/
Data/Scripts/Evaluators/
Data/Scripts/Library/PGTaskForce.lua
```

The Lua plan directories are particularly valuable for understanding how goals become orders.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://aet-sweaw-modding.readthedocs.io\&sz=32)

Unofficial Modding Documentation+1



Best technical reference: [Advanced Modding: Empire at War AI](https://steamcommunity.com/sharedfiles/filedetails/?id=1171759326\&utm_source=chatgpt.com) — covers budgets, goals, plans, perceptions, TaskForce and FreeStore.

For your clone, I'd start with the tactical global commander, GoalManager and TaskForceManager before building the full galactic strategic layer. That gives the enemy the ability to coordinate fleets and objectives without creating a giant unit-level AI controller.
