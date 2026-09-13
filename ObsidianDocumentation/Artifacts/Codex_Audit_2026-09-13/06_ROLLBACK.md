# Rollback for a future authorized repair

No live configuration changed during this audit, so there is no configuration repair to roll back. The new audit directory can be kept as historical evidence. Do not delete or restore unrelated files to undo it.

## Before applying any batch

1. Identify this audit folder and the approved finding/batch IDs. Re-read the exact current source and deployed files. Compare their SHA-256 values with `inventory.json`; a mismatch requires reconciling newer edits, not overwriting them.
2. Record current Git status in the game repository and chezmoi source. Preserve all unrelated modifications, including the 23 C#/asset changes already present at audit start.
3. Take **new** prechange snapshots for the exact files in the batch. A project branch does not protect `~/.codex`; global/source targets need their own paired copies. Store any secret-bearing copies outside this report and outside Git with restrictive permissions. Hashes in this report are not backups.
4. Inspect the proposed diff. No package install, permission broadening, automatic login, credential rotation, Obsidian change, push or merge is implied by an ordinary documentation patch.

## Restore by batch

| Batch | Restore scope |
|---|---|
| A — Documentation | Reverse the specific patch, or restore its exact paragraphs after checking concurrent edits |
| B — Sync | Restore both the changed chezmoi source/template and deployed target from the same prechange checkpoint; render again before any future apply |
| C — Vault launcher | Restore only approved server/mirror launcher fields and any newly authorized host mapping. Do not touch `.obsidian`, certificates, ports or credentials unless separately included |
| D — Skill/role policy | Revert only scoped skill enablement/description and approved role policy; reconcile source and installation |
| E — Advisory docs/credential | Restore ordinary documentation if appropriate. Do not re-enable a retired credential; credential remediation needs its own forward recovery procedure |
| F — Flags/rules | Restore the changed key/rule set and synchronized template, preserving newer user grants/edits |

After restoring, start a fresh affected task/session only when authorized and safe. Repeat the smallest relevant read-only configuration/tool check. If an integration repair needs a behavior test, obtain explicit automated-test authorization and follow the repository's Unity scene preflight when applicable.

Never use a blanket `git reset`, home-directory restore, forced settings sync, scene reload, or broad package reimport as rollback. No automatic “save all” or dialog-driven Editor recovery is permitted.

## Known audit-only artifacts

The persistent deliverable is this vault folder. Temporary sanitized inspection material was created at `/tmp/codex-audit-2026-09-13` (public source text, hashes, source/target key differences, and initial Git-status lines). It is not a live configuration source. No secret-bearing backup was created by this audit.
