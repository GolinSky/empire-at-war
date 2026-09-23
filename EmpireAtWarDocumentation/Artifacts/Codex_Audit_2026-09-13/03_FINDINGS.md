# Findings

Evidence paths use aliases defined in [[01_INVENTORY]]. All findings are open: this audit did not apply repairs. Priority reflects impact, not file age or length. Confidence distinguishes observed facts from untested consequences.

## F01 — P1: Required vault MCP launcher cannot resolve on this Mac

**Confirmed, high confidence.** `REPO/.codex/config.toml:22–27` specifies a Windows Node executable, Windows mcpvault package and Windows vault path. The executable does not exist on this host. Both binaries retain this enabled declaration in their root/nested registry output; the current task exposes no vault tools. All three compatibility JSON files repeat Windows vault paths at lines 4–7.

**Impact:** mandated MCP retrieval of UI/organization notes is unavailable through this registration on the Mac. The vault's Markdown remains locally accessible, allowing this requested report to be saved without starting Obsidian.

**Smallest repair:** explicitly authorize a platform-aware `empire-vault` launcher correction; first locate the actual Mac installation of the same stdio server, then bind its executable, package and vault root. Preserve the working Windows branch and compatible client mirrors. Do not invent a Mac package path, install a substitute, or restore the retired REST bridge.

**Acceptance:** V04/V08/V14; initialize only the intended server and read a known note on each available host. **Rollback:** restore only the changed launcher declarations/source mapping. `AGENTS.md:20–24` specifically protects this configuration; a general repair request should name this Obsidian change explicitly.

## F02 — P1: Sync source would overwrite current working settings

**Confirmed drift, high confidence; overwrite not performed.** `SYNC/home/dot_codex/config.toml.tmpl:2–3` renders Sol/high while `HOME/.codex/config.toml:2–3` contains Astra/xhigh. Template lines 238–240 target app/browser runtime `26.831.21537`; installed lines 108–111 target `26.908.40834` and include current `sky` binding. The rendered template also drops `SKY_CUA_SERVICE_PATH`, restores old UI detail mode, and changes legacy Unity package `10.0.0` to `9.7.1`. `chezmoi cat` and parsed key comparison established these differences without applying them.

**Impact:** a broad sync can undo user model/UI preferences and newer desktop integration state. Global AGENTS source and installation do match; this is not universal sync failure.

**Smallest repair:** reconcile the Mac branch of the source template with deliberately preserved live values before any apply. Assign ownership for app-generated runtime fields and host-specific preferences so future updates do not repeatedly restore stale values. Preserve Windows independently. Include installed-only execution-rule differences in review rather than silently adding/removing them.

**Acceptance:** V07/V14; render-only comparison contains only reviewed intended changes, then authorized targeted application and readback. **Rollback:** paired source + deployed-file snapshots, never an entire home-directory restore. No model change or unverified runtime upgrade belongs in this batch.

## F03 — P1: A literal credential is present in tracked vault documentation

**Confirmed presence and Git tracking, high confidence.** `REPO/EmpireAtWarDocumentation/00_Home.md:43` contains a literal API credential. Its value is omitted from this report and proposed patches. Lines 41–42 describe the retired REST integration. `git ls-files` confirms the note is tracked.

**Impact:** reading or publishing the documentation can carry the credential with it. Current validity, remote repository visibility, historical distribution and exploitability were not assessed. This is not a claim that the current stdio server uses it or that an account was compromised.

**Smallest repair:** a separately authorized Obsidian credential/documentation cleanup should determine whether the credential is active, retire/rotate only if needed, and replace the literal in current documentation. Removing current text alone does not remove historical copies. Do not rewrite Git history or rotate anything automatically.

**Acceptance:** credential-free intended documentation plus verification of the intended integration under that separate authorization. **Rollback:** documentation-only reversal if needed; never restore a revoked credential as a functional rollback. `AGENTS.md:20–24` forbids changing these credentials/configuration during a general audit. Nothing was changed here.

## F04 — P2: Read-only role declarations are not proof of enforced isolation

**Confirmed received metadata; enforcement not probed.** Six role files declare `sandbox_mode="read-only"` at line 5. The spawned `code_explorer` received unrestricted filesystem/no-escalation runtime metadata, matching this parent session. No role contains MCP tool allow/deny lists. Parent Serena configuration advertises editing and memory-writing tools; parent hosted connectors also advertise write operations.

**Impact:** read-only behavior currently depends at least partly on instructions and parent permissions. No unauthorized write occurred. The user's intended full-access policy may be deliberate, so the permission mode itself is not automatically a defect.

**Smallest repair:** document the effective boundary first. If enforced isolation is wanted, select and validate supported parent/role permissions and role-specific MCP/plugin capabilities, retaining only what each role needs. A local filesystem sandbox does not govern writes performed by external servers. Do not change permissions in an unrelated documentation cleanup.

**Acceptance:** V09/V11 in a separately authorized disposable fixture; inspect child runtime metadata and callable tools, then verify restrictions without production writes. **Rollback:** only approved role/policy changes. [Official subagent inheritance guidance](https://learn.chatgpt.com/docs/agent-configuration/subagents) supports this distinction.

## F05 — P2: Personal Graphify skill trigger conflicts with project prohibition

**Confirmed metadata conflict, high confidence; no prohibited execution.** `HOME/.agents/skills/graphify/SKILL.md:3` advertises applicability to essentially any codebase/project-content question. It is visible in this task's catalog. `REPO/AGENTS.md:172` forbids Graphify. The server is correctly disabled in `.codex/config.toml:29–51`.

**Impact:** unnecessary conflicting routing instructions are presented on ordinary project tasks. Server disablement does not disable the skill metadata.

**Smallest repair:** scope skill disablement to this project using supported `skills.config` behavior with host-correct path resolution, or narrow the owned skill's applicability to honor repository opt-outs and update its chezmoi source. Prefer preserving use in other projects over deleting the global skill. The reference describes a skill directory while guide examples use `SKILL.md`; verify the chosen form against the installed client before applying it.

**Acceptance:** V05/V06/V14; it is absent/ineligible here and still available where allowed. No Graphify execution is needed to inspect its discoverability. **Rollback:** remove only the scoped override or restore the narrowly edited description.

## F06 — P2: Recovery path and compatibility routing are platform-specific

**Confirmed text/path issue; other-client behavior untested.** `REPO/AGENTS.md:37` instructs Serena recovery using `F:\Private\empire-at-war`, although live Serena is already correctly attached to the Mac project. `.agents/mcp_config.json:14–15` pins Unity to that Windows root; `.antigravity` and `.gemini` omit the pin, as does native Codex config.

**Impact:** Serena's exceptional recovery instruction is wrong on macOS; client mirrors express inconsistent project-routing intent. This is not a current Serena failure and does not prove wrong routing on Windows.

**Smallest repair:** replace the fixed recovery path with the verified current repository root; a documentation patch is staged. Separately decide whether mirrors should explicitly bind the current host's root or use validated working-directory routing. Preserve each client's supported loader and syntax.

**Acceptance:** V04/V08 on each relevant client/host; correct project identity under normal and recovery conditions. **Rollback:** reverse the exact instruction/mirror edits.

## F07 — P2: Advisory setup documentation contradicts live project decisions

**Confirmed documentation drift, high confidence.** `EmpireAtWarDocumentation/Rules/README.md:3,16–17,48` presents legacy Unity MCP/Graphify as active. `00_Home.md:17,29,41–43` routes toward old Graphify, a partial rules duplicate and retired REST settings. `.codex/agents/README.md:1,20` describes a Sol/high parent while configured parent defaults are Astra/xhigh.

**Impact:** following the guides can revive retired tooling or change the parent model unnecessarily. The current root AGENTS remains authoritative, and missing repeated rules in the nested note do not cancel inherited rules.

**Smallest repair:** update active-tool statements and designate the root policy as canonical; retain historical references with explicit status. Make the role README inherit the configured parent choice; that small patch is staged. Obsidian integration/credential statements require the explicit authorization described in F01/F03, even though they occur in Markdown.

**Acceptance:** V04; links resolve and no current guide directs agents to prohibited tooling. **Rollback:** revert only changed documentation paragraphs. Do not delete the historical Graphify guide or all duplicate notes automatically.

## F08 — P2 policy review: Broad persisted command allowances

**Confirmed configuration; user intent unresolved.** `HOME/.codex/rules/default.rules:2,3,5–7` allow broad prefixes for `codex mcp add`, MCP login, tool installation/upgrade and arbitrary `uvx --from` packages. All seven rules use `allow`; no narrower restrictions are present in this file. The template includes only four Mac rules and additional Windows-specific historical commands.

**Impact:** in sessions using command escalation, these prefixes can suppress a new prompt for a larger family of commands than one previously approved operation. In this task approval is already `never`; changing these rules alone would not provide the desired boundary. Their presence is not authorization for this audit to install or configure anything.

**Smallest repair:** review intended scope with the user's policy; narrow or remove only obsolete allowances and preserve required workflows. Reconcile source and deployed rules together. Do not automatically replace them with another approval mode.

**Acceptance:** V10 plus explicit intended approval scenarios on a supported checker. No checker was run in this audit. **Rollback:** restore the one changed rules file and its source template. See [execution-rule semantics](https://learn.chatgpt.com/docs/agent-configuration/rules).

## F09 — P2: Mac receiver instructions use an unsupported login command and print a secret

**Confirmed, high confidence.** `SYNC/MACOS_RECEIVER.md:162` says `codex auth login`; both inspected CLI helps expose top-level `login` and no `auth` command. Line 152 advises echoing the Context7 API key. The guide also lists legacy Unity server `9.7.1` at line 70.

**Smallest repair:** use the installed `codex login` command, check credential presence without printing its value, and distinguish historical global tooling from this repository's official Unity route. The first two corrections are staged as a documentation-only patch. No login or credential command was executed.

**Acceptance:** installed CLI help matches the guide; examples reveal only presence/missing state. **Rollback:** reverse the guide patch. Updating legacy Unity guidance requires checking other projects before declaring a global replacement.

## F10 — P3: Legacy feature flags merit version-specific cleanup only

`HOME/.codex/config.toml:157–159` and the source template include `rmcp_client=true`, `js_repl=false`, `multi_agent=true`. CLI `features list` labels `js_repl` removed and `multi_agent` stable. `rmcp_client` was not listed. Normal MCP/config commands succeeded without warnings.

`--strict-config mcp list` and `--strict-config features list` are both rejected as unsupported command combinations by both installed binaries. Those failures do **not** establish invalid user TOML. `rmcp_client` remains an unverified compatibility item, not a proven startup defect.

**Plan:** leave working settings until the exact client schema or a supported strict-validation route establishes semantics. Removing a proven no-op is optional. Keep the supported `max_concurrent_threads_per_session` value; there is no obsolete `max_threads` setting to migrate here.

## Recommendations and deliberate non-findings

- The global “if unclear, stop and ask” wording can be narrowed for routine reversible work if unnecessary stops are observed. It is not evidence of a present failure. Preserve meaningful ambiguity, destructive-action and scope boundaries.
- The global testing examples are overridden here by the explicit project test gate. Do not remove that gate based on Astra recommendations.
- Root architecture, DI, naming, explicit bindings, UI routing, Unity asset persistence and scene safety remain intact. No gameplay refactor is justified by this audit.
- Large Markdown, vendor skills, cached plugin copies, disabled legacy entries, the role filename convention, and a four-child concurrency cap are not defects by themselves.
- No quota/performance saving was measured. No fresh agent-routing benchmark, remote write-permission probe, or Windows check was performed.
