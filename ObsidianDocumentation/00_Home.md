# Empire At War - Obsidian Documentation Vault

Welcome to the **Empire At War** project vault!

## 📌 Core Documentation & Standards

- [[AGENTS|AGENTS.md]] - Mandatory Shared AI Agent Instructions
- [[PROJECT_ORGANIZATION|PROJECT_ORGANIZATION.md]] - Authoritative Type-First Asset Structure & Placement Rules
- [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]] - UI Architecture & Refactoring Playbook
- [[SELECTION_SYSTEM|SELECTION_SYSTEM.md]] - Selection System Architecture & Usage
- [[GRAPHIFY_GUIDE|GRAPHIFY_GUIDE.md]] - Knowledge Graph & Graphify Guide
- [[README|README.md]] - Project Overview & Setup Guide
- [[TODOs|TODOs.md]] - Project Architecture & Refactoring Backlog

---

## 📋 Project TODOs

- [ ] **Fix UI Service**: Move UI creation, UI Service dependencies, and rendering orchestration out of gameplay services into UI prefabs / presenters (`<Feature>UiController` / `<Feature>Ui`), strictly adhering to [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]].

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

- **Plugin:** `obsidian-local-rest-api`
- **Port:** `https://127.0.0.1:27124/`
- **API Key:** `1c49d8b5597a92eb7b3001058807174c50fab29ebf60feaf509126440e02a9b2`
