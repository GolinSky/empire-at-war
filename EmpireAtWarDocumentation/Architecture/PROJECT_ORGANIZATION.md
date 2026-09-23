# Project Organization Guide - MirrorMultiplayerTemplate

This document outlines the asset organization rules for the **MirrorMultiplayerTemplate** Unity project. The project follows a **type-first** structure, where the root folder defines the asset type, and subfolders define the domain or category.

## Structure Overview

```mermaid
graph TD
    A["Assets/"] --> Art["Art/ - Visual assets"]
    A --> Plugins["Plugins/ - Core external packages"]
    A --> Prefabs["Prefabs/ - Reusable objects"]
    A --> Scripts["Scripts/ - C# source code"]
    A --> Settings["Settings/ - Configuration assets"]
    A --> Scenes["Scenes/ - Game levels"]
    A --> Sandbox["Sandbox/ - Technical tests"]
    A --> ThirdParty["ThirdParty/ - External tools & documentation"]
    A --> Addressables["AddressableAssetsData/ - Addressables config"]
    A --> Resources["Resources/ - Minimal bootstrap"]

    Art --> Art1["Models/ - Meshes & character models"]
    Art --> Art2["Animation/ - Controllers & avatar masks"]
    Art --> Art3["Materials/ - Visual surface definitions"]
    
    Prefabs --> P1["Models/ - Characters, items, and skins"]
    Prefabs --> P2["Ui/ - Canvas and menu elements"]
    Prefabs --> P3["View/ - Network and service orchestration"]

    Scripts --> S1["Components/ - Reusable logic operations"]
    Scripts --> S2["Entities/ - State models and data holders"]
    Scripts --> S3["Services/ - Global systems and VContainer scopes"]
    Scripts --> S4["Editor/ - Editor tooling"]
    Scripts --> S5["Tests/ - Automated unit & integration tests"]

    Settings --> Set1["Input/ - Input system actions"]
    Settings --> Set2["Player/ - Movement and player settings"]
    Settings --> Set3["Build Profiles/ - Project build configs"]
    Settings --> Set4["Data/ - Game databases and settings data"]
```

## Root Folder Definitions

### 1. Art (`Assets/Art`)
Contains all visual assets.
- **Models/**: 3D meshes, FBX files, and their imported materials.
- **Animation/**: Animator Controllers, Animation Clips, and Avatar Masks.
- **Textures/**: Image assets and sprites.
- **Materials/**: Shared material definitions.

### 2. Prefabs (`Assets/Prefabs`)
Contains reusable GameObject configurations.
- **Models/**: Prefabs representing physical entities (Player, Equipment, Items, Skins).
- **Ui/**: Menu screens, HUD elements, and UI widgets.
- **View/**: Non-physical orchestration prefabs (NetworkManager, Scopes, Services).

### 3. Scripts (`Assets/Scripts`)
Contains all project-owned C# source code.
- **Components/**: Logic components that drive behavior (e.g., Movement, Interaction).
- **Entities/**: Data-focused models and shared entity logic.
- **Services/**: Global systems, manager logic, and dependency injection (VContainer).
- **Editor/**: Editor-only scripts and custom inspectors.
- **Tests/**: Automated test suites (EditMode and PlayMode).

### 4. Settings (`Assets/Settings`)
Contains configuration and scriptable object data.
- **Data/**: Game settings and databases (e.g., ScriptableObjects inheriting from the `Data` class, like `HealthData`, `InventoryData`, and the global `AssetMappingData`).
- **Input System Actions**: The `.inputactions` and `.inputsettings` assets.
- **Player Data**: ScriptableObjects like `MovementData`.
- **Render Pipelines**: HDRP/URP profiles and quality settings.

### 5. Plugins (`Assets/Plugins`)
Reserved for major, project-wide external packages.
- **Mirror**: Networking library.
- **TextMesh Pro**: Text rendering.
- **DOTween**: Animation engine.

### 6. Sandbox (`Assets/Sandbox`)
A boundary for temporary development.
- **Scenes/**: Blocking, technical testing, and prototyping levels.
- **Prefabs/Debug/**: Debug-only objects and technical integration tests.

## Placement Rules

1. **Type-First**: Always place assets in the root folder that matches their type (e.g., a weapon model goes in `Art/Models`, not `Prefabs`).
2. **Graphics vs Art**: The folder for visual assets must always be named `Art`.
3. **Addressables**: **Do not modify the `AddressableAssetsData` folder structure.** Assets referenced by Addressables can be moved through the Unity Editor, but the data folder itself must remain intact.
4. **Resources**: Keep `Assets/Resources` minimal. Only use it for bootstrapping assets (e.g., initial VContainer configuration).
5. **Third-Party**: External assets from the Asset Store that are not core plugins belong in `Assets/ThirdParty`.
6. **Scripts**: Maintain the `Components/Entities/Services` separation to ensure a decoupled architecture.
