---
title: Codex configuration audit — 2026-09-13
date: 2026-09-13
completed: 2026-09-14
status: audit-complete-repairs-not-applied
tags: [codex, configuration, audit]
---

# Codex configuration audit

The Mac setup loads the project configuration and its eight custom agents. Unity and Serena respond for the correct project. The most important problems are an unusable Windows-only vault launcher on macOS, a settings-sync template that would restore older local settings, and a credential stored in tracked vault documentation. The Graphify skill also advertises a workflow prohibited by this repository. Role files describe read-only defaults, but the current full-access session does not establish an enforced read-only boundary for children.

**This is an audit and repair plan. No live settings were repaired.** Only this new report folder was written into the vault. No automated tests, asset edits, imports, package installations, permission changes, credential changes, or settings synchronization were performed.

## Read the results

- [[01_INVENTORY|Inventory, ownership, MCP and role matrices]]
- [[02_EFFECTIVE_LOAD_MAP|What loads and which layer wins]]
- [[03_FINDINGS|Prioritized findings and evidence]]
- [[04_MIGRATION_MAP|Minimal repair batches and preservation map]]
- [[05_VALIDATION|Checks performed and checks still outstanding]]
- [[06_ROLLBACK|Rollback and change-control procedure]]
- [[07_HANDOFF|Repair handoff and completion checklist]]
- `inventory.json`: paths, sizes, modification times and SHA-256 hashes of 27 inspected declarative/reference files. It contains no file bodies.
- `patches/`: three small, unapplied documentation proposals. These do not repair configuration or authorize changes.

## Environment and scope

| Item | Observed value |
|---|---|
| Machine | macOS 15.0, arm64; zsh |
| Project | `/Users/golinsky/Projects/empire-at-war` |
| Codex home | `/Users/golinsky/.codex`; `CODEX_HOME` unset in the inspected shell |
| Terminal executable | `/opt/homebrew/bin/codex`, `codex-cli 0.153.0` |
| Desktop app | `/Applications/ChatGPT.app`, version `26.908.40834`, build `8881` |
| App-bundled executable | `/Applications/ChatGPT.app/Contents/Resources/codex`, `0.154.0-alpha.6.2` |
| Configured parent defaults | `gpt-6-astra`, `xhigh`; no change proposed |
| Current task permissions | Runtime instructions report unrestricted filesystem, approval policy `never` |
| Project trust | Project explicitly trusted in global config |
| Profiles | No `~/.codex/*.config.toml` files or selected profile identified; historical usage not inferred |
| Unity | CLI `1.0.0-beta.6`; Editor `6000.4.7f1`; `com.unity.pipeline` `0.6.0-exp.1` |
| Other runtimes | Node `v25.9.0`; uvx `0.11.18`; Context Mode `1.0.169`; Serena `1.7.0` |
| Sync source | `/Users/golinsky/.local/share/chezmoi`, repository identified by its guides as `GolinSky/codex-dotfiles`; clean working tree at inspection |
| Baseline project work | 23 pre-existing modified C#/data-asset files; preserved |

Inspected global/project TOML, instruction Markdown, all eight agent definitions, personal/system skill inventory, three compatibility MCP JSON files, installed tool declarations, selected browser integration manifests, execution rules, sync templates/guides, package manifest and targeted vault navigation notes. One project `code_explorer` assisted with read-only inspection; no recursive delegation.

Excluded: authentication stores, complete logs/session histories, a recursive vault-content audit, `.obsidian` settings/credentials, gameplay review, generated dependency cleanup, other project audits, Windows live execution, and remote synchronization. Accessible system/managed-file locations were absent; this does not prove that no account/cloud/desktop policy exists.

## Supplied guidance read

The referenced [Analyze MD Skill Rules conversation](https://chatgpt.com/c/6aa673c5-b2ec-83ed-9c87-d2ea5c40d316) was retrieved through the task reader. All three linked Markdown documents were fetched in full from Drive after metadata verification:

1. [01 — Verified OpenAI guidance](https://drive.google.com/file/d/1XXjI7RrOwh_sAetNIcVElQhTnEej1kVp/view)
2. [02 — Audit and refactor plan](https://drive.google.com/file/d/1Q0ahD4pV6tveDBdEOIuDODqo7RZemnD1/view)
3. [03 — Execution prompts and checklist](https://drive.google.com/file/d/1up7KWiXTz_pS23FWBfsLEGRc9IWXxIqv/view)

Their audit-only intent informed this work. Their future test/repair examples do not override the active request to analyze first or the repository's prohibition on unsolicited automated tests.

## Official evidence and interpretation

Official pages were retrieved during the 13–14 September 2026 audit. Terminal behavior was checked on both installed binaries where stated. Current web documentation alone was not treated as a version-matched schema.

| Source | Applicable conclusion |
|---|---|
| [Config basics](https://learn.chatgpt.com/docs/config-file/config-basic) | Launch overrides, trusted project layers, profiles, user config, cloud defaults, system config and defaults are distinct layers. Managed constraints are separate. |
| [AGENTS.md discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md) | Override/ordinary/fallback selection and the combined instruction-byte budget govern loading; descendant files are not universally loaded. |
| [Configuration reference](https://learn.chatgpt.com/docs/config-file/config-reference) | Current agent-limit and skill/tool enablement fields exist. A legacy alias is not automatically invalid. |
| [Subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents) | Standalone custom role TOMLs are supported. Parent live permissions and omitted settings can affect children. Inspect received capabilities. |
| [Build skills](https://learn.chatgpt.com/docs/build-skills) | Skills expose metadata first and full instructions on use. Per-skill disablement is supported; bundled assets should not be deleted merely for being older or long. |
| [MCP](https://learn.chatgpt.com/docs/extend/mcp?surface=cli) | Configuration, connectivity, tool availability and approval policy are separate; plugin MCP policy has its own supported configuration path. |
| [Execution rules](https://learn.chatgpt.com/docs/agent-configuration/rules) | Rule files govern command approval outside a sandbox. They do not grant user authorization or prove remote-tool isolation. |

The previously unavailable [Rethinking skills and prompts for GPT-6 Astra article](https://learn.chatgpt.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra) was successfully retrieved in this audit; its page gives 11 September 2026 as publication date. It recommends narrow skill descriptions, selective reference loading, revisiting obsolete instruction scaffolding, and clear completion boundaries. These are recommendations. Its discussion of testing and autonomy does not authorize removing this project's test gate, Unity persistence rules, or Obsidian protection. The setup uses Luna, Terra and Sol children too, which further argues against optimizing all shared instructions exclusively for Astra.

No mandatory 100-line cap, compulsory model migration, blanket Markdown rewrite, or guaranteed quota saving is established by this audit.
