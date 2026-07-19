# Graphify Guide for AI Agents and Developers

This guide explains how to install, configure, and use **Graphify** in this project to navigate its structure and assist AI coding agents (such as Claude Code, Cursor, Aider, and Antigravity) with token-efficient codebase querying.

---

## 1. What is Graphify?
**Graphify** is a local AST and semantic parser that transforms the project directory (C# source files, scene configurations, and playbooks) into a queryable semantic knowledge graph. It outputs its files to `graphify-out/`, including:
* **[GRAPH_REPORT.md](file:///f:/Private/empire-at-war/graphify-out/GRAPH_REPORT.md)**: A high-level plain-text overview of the code's modules, communities, and dependency flows.
* **`graph.json`**: The full JSON representation of the graph (approx. 112 MB) used for querying.
* **`graphify-out/memory/`**: Cache files and markdown summaries of queries ran against the graph.

To prevent repository bloat, the large `graph.json` and its cache directories are ignored by Git. Only the `.md` report files and query history are tracked and synced across machines, as configured in [.gitignore](file:///f:/Private/empire-at-war/.gitignore).

---

## 2. Installation & Setup

To use Graphify on your development machine (Windows or macOS), install it using python's package manager. The package name on PyPI is `graphifyy` (two **y**'s), while the CLI command is `graphify` (one **y**).

### Install via `uv` (Recommended)
```bash
uv tool install graphifyy
```

### Install via `pipx`
```bash
pipx install graphifyy
```

### Install via `pip`
```bash
pip install graphifyy
```

---

## 3. Integrating with AI Agents

AI coding agents use Graphify to query relationships in the codebase without reading hundreds of raw files, saving time and token usage.

Run the setup command for the specific agent you are using from the root of this project:

| Agent / Editor | CLI Integration Command | What it configures |
| --- | --- | --- |
| **Antigravity / Gemini** | `graphify install --platform antigravity` | Configures `.agent/rules` and workspace workflows |
| **Cursor** | `graphify install --platform cursor` | Creates `.cursor/rules/graphify.mdc` |
| **Claude Code** | `graphify install --platform claude` | Updates `CLAUDE.md` and hooks into pre-tool execution |
| **Codex / Aider** | `graphify install --platform codex` | Appends context capabilities to [AGENTS.md](file:///f:/Private/empire-at-war/AGENTS.md) |

---

## 4. Rebuilding & Updating the Graph

When you clone the project on a new machine or pull new updates, you can rebuild or update the graph locally.

### Fast Offline Rebuild (No LLM Required)
If you pull new code or make changes, update the local `graph.json` without any token costs or API keys:
```bash
graphify update .
```
*(If you delete files or do a massive refactor, add `--force` to overwrite stale nodes)*

### Full Semantic Re-Extraction
If you want to perform a deep semantic extraction using an LLM (requires an API key like `GEMINI_API_KEY` or `OPENAI_API_KEY` in your environment):
```bash
graphify extract .
```

---

## 5. CLI Query Tools
You can query the graph locally from your terminal using these subcommands:

### Ask a question about the structure
Queries the graph via Breadth-First Search (BFS) to retrieve relevant files and context:
```bash
graphify query "How does selecting player ships open shipui?"
```

### Find the dependency path between two components
Finds the shortest path of dependencies between two nodes in the graph:
```bash
graphify path "ShipAIBrain" "ShipController"
```

### Explain a specific component
Generates a plain-language explanation of a code module and its immediate neighbors:
```bash
graphify explain "EconomyController"
```

---

## 6. How it helps in Refactoring
As outlined in the [UI_REFACTORING_PLAYBOOK.md](file:///f:/Private/empire-at-war/UI_REFACTORING_PLAYBOOK.md), when starting a refactor:
1. Reference the [GRAPH_REPORT.md](file:///f:/Private/empire-at-war/graphify-out/GRAPH_REPORT.md) to inspect the current feature's community.
2. Run `graphify path` or `graphify query` to locate exactly which C# models and views will be affected by changes.
3. Verify that your refactor did not break dependency rules (e.g. View should not depend directly on Model in MVP structure).
