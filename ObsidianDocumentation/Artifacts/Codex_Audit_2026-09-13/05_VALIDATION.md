# Validation record

Only static inspection, installed help/version/config listings, and harmless read operations were performed. **No automated test suite or behavior fixture was run.** The source plan's V01–V14 identifiers are retained for continuity, but partial evidence is not reported as a full pass.

| ID | Status | Evidence obtained / remaining work |
|---|---|---|
| V01 — Root/nested/outside loading | Partial | Both binaries' merged MCP registries checked in all three locations. Current root instructions/roles visible. Fresh model instruction-chain launches not performed |
| V02 — Overrides and byte limits | Static only | No active override/fallback discovered; 17,848 root-chain bytes, 23,002 under vault Rules before wrappers. Boundary fixtures not run |
| V03 — Profiles, overrides, trust | Partial | Explicit trusted project; no profile files found; current task override reports full access/never. No alternate trust/profile fixture or cloud-policy enumeration |
| V04 — Links/resources/platforms | Partial; defects found | Mandatory vault launcher unavailable on Mac; stale recovery path found; report's own links/files checked. All vendor resources and Windows paths not exercised |
| V05 — Skill routing | Not tested | Metadata inventory and Graphify conflict established; no positive/negative invocation fixtures |
| V06 — Duplicate/disabled skills | Static only | Personal Graphify still advertised while server disabled. Installed review-agent not advertised. No duplicate-name fixture |
| V07 — Config syntax/application | Partial | Ten TOML files and three compatibility JSON files parse; both clients' MCP lists and CLI feature listing succeed. Unsupported strict-command combinations are recorded below; complete matching-schema validation not claimed |
| V08 — MCP | Mixed | Unity, Serena, Context7, Context Mode and desktop/Drive reads succeed. Vault unavailable. DeepWiki/browser/other connectors not health-tested |
| V09 — Permissions | Not tested | Current task and child runtime metadata inspected. No capability restriction was validated through a write attempt |
| V10 — Execution policy | Static only | Seven deployed allow rules inspected; no rule matcher/approval fixture run |
| V11 — Roles | Partial | All eight files valid and exposed; one assigned read-only child used; received full-access metadata recorded. No enforcement or ownership-collision fixture |
| V12 — Hooks/degraded behavior | Static only | Five relevant bundled manifests inspected; lifecycle callbacks/notify identified. No offline, reentrancy, or callback count experiment |
| V13 — Unity | Read-only check passed | Correct project and Editor `6000.4.7f1`, state ready. `MainMenuScene`, `Assets/Scenes/MainMenuScene.unity`, loaded/active, `isDirty=false`. No scene mutation or test command |
| V14 — Sync/rollback | Partial; drift found | `chezmoi cat` rendered the Mac target without applying; parsed differences recorded. Global AGENTS matches source. No apply, Windows run, or rollback rehearsal |

## Installed command results

- `/opt/homebrew/bin/codex --version`: `0.153.0`.
- App-bundled Codex `--version`: `0.154.0-alpha.6.2`.
- Each binary's `mcp list --json`: exit 0 at project root, `Assets/Scripts`, and `/Users/golinsky`. Project Unity/Serena/vault present only inside project; Graphify and legacy Unity disabled inside project.
- `codex features list`: exit 0; `js_repl` reported removed, `multi_agent` stable/true. `rmcp_client` absent from that output; semantics remain unresolved.
- Both `--strict-config mcp list` and `--strict-config features list`: rejected with “not supported” for those subcommands. These are diagnostic-command limitations, not failed TOML parsing. No unsafe alternate invocation was used just to obtain a strict result.
- Unity status: one existing Editor, exact project path, state ready. The scene command's schema was inspected before its read-only invocation.
- Serena `get_current_config`: version `1.7.0`; active project `empire-at-war`; language server ready; `codex` context; editing/interactive modes. No activation, onboarding, memory write or symbol edit performed.
- Context7 resolved Codex documentation and returned relevant current guidance; Context Mode stats responded.
- Three Drive Markdown documents: metadata read, complete text fetched. The app's task reader returned the linked conversation.

## Report integrity and limits

The source directory was clean during inspection. The project already contained 23 modified C#/asset files. Before final delivery, hash comparison against the initial 15-file settings baseline and a broader 27-file inventory checks that audited source/configuration files remain unchanged. The original Git-status lines are compared separately so unrelated work is not mistaken for report edits.

Report files are scanned locally against the credential encountered in the existing vault note; its value must not occur in any output. No auth stores, raw backups, full runtime environment or secret-bearing source files are included. New report Markdown and proposed patches are ordinary vault artifacts, not auto-loaded instruction/skill files.

Windows remains **not inspected live**. Other-client mirror behavior, complete vendor skill/script safety, all connector grants, cloud-managed configuration, full feature-key schema validation, and fresh-session behavior remain explicitly unverified. These limits prevent claiming that configuration is repaired or secure solely from this audit.

No before/after cost, token, latency or quality benchmark was run. Byte totals are not token counts, and no quota saving is claimed.

## Final artifact readback — 14 September 2026

All 27 inventoried source/config/reference files and the initial 15 settings baselines retained identical SHA-256 values. All 23 pre-existing Git-status entries remained present. Other work changed additional gameplay/UI files and Obsidian workspace state concurrently, and added other files during the audit. Those changes were not inspected, altered or attributed to this audit. This audit's own writes are limited to this new report folder. All report wikilinks resolve, and the encountered credential occurs zero times in the delivered artifacts. The three proposed patches remain unapplied.
