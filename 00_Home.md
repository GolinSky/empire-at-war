# Empire At War - Obsidian Vault Dashboard

Welcome to the **Empire At War** project vault!

## 📌 Core Documentation & Standards

- [[AGENTS|AGENTS.md]] - Mandatory Shared AI Agent Instructions
- [[PROJECT_ORGANIZATION|PROJECT_ORGANIZATION.md]] - Authoritative Type-First Asset Structure & Placement Rules
- [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]] - UI Architecture & Refactoring Playbook
- [[SELECTION_SYSTEM|SELECTION_SYSTEM.md]] - Selection System Architecture & Usage
- [[GRAPHIFY_GUIDE|GRAPHIFY_GUIDE.md]] - Knowledge Graph & Graphify Guide
- [[README|README.md]] - Project Overview & Setup Guide

---

## 🏗️ Architecture & Codebase Map

### Architecture Pattern
This project follows **Model-View-Presenter (MVP)** decoupled from Unity APIs:
- **Model:** Pure C# classes (no `UnityEngine` references). Own state & business logic.
- **View:** `MonoBehaviour` implementations responsible only for rendering state & input.
- **Presenter:** Coordinates Model & View via C# events (`System.Action`).

### Folder Layout Summary
- `Assets/Art/`: Visual source assets (Models, Animations, Textures, Materials)
- `Assets/Prefabs/`: Reusable GameObjects (Models, UI, View)
- `Assets/Scripts/`: C# code (`Components`, `Entities`, `Services`, `Editor`, `Tests`)
- `Assets/Settings/`: ScriptableObjects & configuration data

---

## 🔌 Obsidian MCP Integration

This vault is connected to AI Coding Assistants (**Antigravity** and **Codex**) via the Obsidian Local REST API MCP Server (`@oleksandrkucherenko/mcp-obsidian`).

- **Plugin:** `obsidian-local-rest-api`
- **Port:** `https://127.0.0.1:27124/`
- **MCP Server Name:** `obsidian`
