# Design and development of a geolocated mobile video game to promote knowledge and relationship with the natural environment

**Serious game:** Micorrizas
**Author:** Saúl Pacheco Trilles
**Supervisor:** Águeda Gómez Cambronero
**Degree:** Bachelor's Thesis in Video Game Design and Development

This repository contains the Unity project that gives rise to the serious game **Micorrizas**. The project is a geolocated mobile cooperative video game designed to support learning, observation and interaction with the natural environment through a shared outdoor experience.

Micorrizas is built around the Jardín de los Sentidos as a physical play space. Instead of replacing the visit with a fully virtual experience, the game uses mobile devices to guide the group, synchronize progress, distribute partial information and encourage players to observe, discuss and interpret the environment together.

## Project Overview

The main goal of the project is to design and develop a serious game that promotes knowledge of the natural environment through cooperative play. The experience combines:

- mobile geolocation;
- cooperative multiplayer sessions;
- a real-world map-based exploration layer;
- educational quests connected to plants and sensory perception;
- responsive UI for outdoor mobile use;
- reusable systems that allow several quests to share a common flow.

The repository is not only a playable prototype. It also reflects an architecture intended to be understandable, modular, extensible and suitable for academic defense. For that reason, the implementation separates game logic, presentation, synchronization, configuration and external content as much as possible.

## General Status

- Engine: Unity 6000.3.7f1.
- Render pipeline: Universal Render Pipeline.
- Target platform: mobile devices.
- Main context of use: outdoor cooperative play.
- Multiplayer: Unity Netcode for GameObjects, Unity Transport and Unity multiplayer services/Relay.
- Input: Unity Input System.
- Content: scenes, prefabs, ScriptableObjects, CSV files and images in `StreamingAssets` and `CoopMinigames`.
- Validation: Edit Mode tests and QA utilities under `Assets/Test`.

## Repository Structure

```text
.
|-- Documentos/
|-- ExternAssets/
|-- Unity/
|   `-- SmartCampus_URP/
|       |-- Assets/
|       |   |-- Art/
|       |   |-- CoopMinigames/
|       |   |-- Dialogue/
|       |   |-- Editor/
|       |   |-- Prefabs/
|       |   |-- Resources/
|       |   |-- Scenes/
|       |   |-- ScriptableObjects/
|       |   |-- Scripts/
|       |   |-- StreamingAssets/
|       |   `-- Test/
|       |-- Packages/
|       `-- ProjectSettings/
`-- README.md
```

## Game Concept

Micorrizas is designed as a cooperative activity for small groups. One player creates a session, the rest of the players join through a code, and the group progresses through a shared map and a set of cooperative quests.

The design avoids treating the mobile phone as an isolated individual screen. Instead, each device can hold partial information, local interaction or player-specific state. The group must communicate face to face to solve the challenges. This design decision supports the educational goal of the thesis: promoting a stronger relationship with the natural environment through shared observation and discussion.

## Main Scene Flow

The scenes included in the build are configured in `Unity/SmartCampus_URP/ProjectSettings/EditorBuildSettings.asset`:

- `Lobby.unity`: entry point for creating or joining a cooperative session.
- `UJI.unity`: main world map and geolocated exploration scene.
- `GardenImageVotingMinigame.unity`: visual plant identification missions.
- `AudioWordConsensusMinigame.unity`: cooperative audio/word consensus missions.
- `CollaborativePlantGuessMinigame.unity`: collaborative plant deduction missions.
- `DistributedPairsMinigame.unity`: distributed pair-matching missions.
- `GardenSmellTaxonomyMinigame.unity`: plant taxonomy/classification missions.
- `CoopFinalResults.unity`: final cooperative results scene.

### Multiplayer Session Flow

The cooperative flow is supported by:

- `CoopSessionCoordinator`: general session coordination.
- `CoopSessionRules`: session rules, including player limits.
- `RelayConnectionService` and `RelayConnectionProtocol`: Relay-based session creation and joining.
- `CoopPlayerProfileSync`: player profile and slot synchronization.
- `CoopGpsStateSync`: synchronized publication of each player's GPS state.
- `CoopSessionProgressSync`: global quests and results progress synchronization.

The architectural intention is to keep local device state separate from synchronized game state. The host and synchronization components hold authoritative state when needed, while UI components present that state and send user actions.

### Geolocation and World Map

The geolocation layer connects the real physical space with the digital game flow. The main related components are:

- `DeviceGpsService`: defensive access to the device GPS service.
- `CoopGpsMarkerController`: visual representation of players on the map.
- `ArcGISMapCoordinateProjector`: conversion between geographic coordinates and scene representation.
- `ArcGISTopDownCameraController`: top-down camera control over the map.
- `GpsDebugPanelUIController`: GPS diagnostics during testing.

This layer must handle mobile-specific issues such as permissions, initialization delays, missing GPS availability and different device behavior.

### Reusable Cooperative Quests Framework

The cooperative quests share a common base under `Assets/Scripts/CoopMinigames/Core`:

- `CooperativeMinigameBase`: base class for the common quests lifecycle.
- `CooperativeMinigameConfigBase`: shared Inspector-editable quests configuration.
- `CooperativeMinigameStage`: stages of the cooperative quests flow.
- `MinigameUIControllerBase`: base class for quests UI controllers.
- `MinigameResultData` and `MinigameResultView`: result model and presentation.
- `CoopSessionFlowService` and `CoopSessionProgressService`: pure services for flow and progress rules.
- `ResponsiveGridLayoutController`, `ResponsivePanelLayoutController` and `SafeAreaFitter`: responsive layout support for mobile screens.

This structure uses inheritance only where there is a clear shared lifecycle. Specific gameplay rules are kept in each quests folder, usually through session classes, services, network models and UI views.

## Quests

The quests are organized by folder:

- `01-GardenImageVoting`: visual voting with plant cards and images.
- `02-AudioWordConsensus`: cooperative consensus based on distributed audio, words or clues.
- `03-CollaborativePlantGuess`: progressive plant deduction with autocomplete, comparison and scoring services.
- `04-DistributedPairs`: distributed cards and pair matching across player devices.
- `05-GardenSmellTaxonomy`: plant classification by categories or uses.

Each quests usually includes:

- a `...MinigameSession` class for gameplay flow;
- a `...MinigameUIController` for presentation;
- a `...MinigameConfig` for editable parameters;
- `...NetworkModels` when synchronized state is required;
- pure `...Service` classes for scoring, assignment, catalog or parsing logic;
- `...View` classes for reusable UI elements.

## Editable Configuration

The project avoids hardcoding gameplay values when they can be exposed through the Inspector or ScriptableObjects. Important configurable elements include:

- `CoopMinigameCatalogConfig`.
- `CoopMinigameThemeConfig`.
- `MinigameTutorialContentConfig`.
- `MinigameFailureFeedbackConfig`.
- `GardenImageVotingMinigameConfig`.
- `AudioWordConsensusMinigameConfig`.
- `CollaborativePlantGuessMinigameConfig`.
- `DistributedPairsMinigameConfig`.
- `GardenSmellTaxonomyMinigameConfig`.
- `DialogueFlowConfig` and `DialogueSystemConfig`.
- `PlayerMarkerAppearanceCatalogConfig`.

Configurable assets are mainly placed in `Assets/ScriptableObjects`, prefabs and scene-assigned components.

## Tests and QA

The project includes Edit Mode tests under `Unity/SmartCampus_URP/Assets/Test/Editor`.

The tests cover areas such as:

- quests gameplay rules;
- scoring and assignment services;
- CSV parsing;
- quests configuration validation;
- responsive layout controllers;
- cooperative session flow;
- dialogue systems;
- scene and manager QA checks.

To run the tests from Unity:

1. Open `Unity/SmartCampus_URP` with Unity 6000.3.7f1.
2. Go to `Window > General > Test Runner`.
3. Select `EditMode`.
4. Run the full suite or a specific group under `Assets/Test/Editor`.

Previous validation artifacts are also stored in the repository root, including `editmode-tests.log`, `unity-editmode-tests.xml` and responsive layout reports.

## Getting Started

1. Install Unity Hub and Unity 6000.3.7f1.
2. Open the folder `Unity/SmartCampus_URP` from Unity Hub.
3. Wait for Unity to restore the packages declared in `Packages/manifest.json`.
4. Open `Assets/Scenes/Lobby.unity`.
5. Check `File > Build Profiles` or `Build Settings` to confirm that the required scenes are included.
6. Run the project in the editor for basic checks, or create a mobile build to validate GPS and real multiplayer behavior.

## Main Dependencies

- `com.unity.netcode.gameobjects`.
- `com.unity.transport`.
- `com.unity.services.multiplayer`.
- `com.unity.multiplayer.playmode`.
- `com.unity.inputsystem`.
- `com.unity.render-pipelines.universal`.
- `com.unity.cinemachine`.
- `com.unity.ugui`.
- `com.unity.probuilder`.
- `com.coplaydev.unity-mcp`.

New dependencies should only be added when they solve a clear project need and their impact on maintainability is justified.

## Development Criteria

The project prioritizes:

- architectural clarity;
- separation between gameplay, UI, networking, persistence and configuration;
- reuse across quests;
- small components with clear responsibilities;
- Inspector or ScriptableObject-based configuration;
- responsive UI for different mobile screen sizes;
- pure services for testable logic;
- explicit ownership and synchronization rules in cooperative flows.

## Naming Conventions

- Interfaces: `I...`.
- Shared abstract base classes: `...Base`.
- Configuration data: `...Config`.
- ScriptableObjects: `...Definition`, `...Config` or `...Database`.
- UI components: `...View`, `...Panel`, `...Popup` or `...UIController`.
- Pure services: `...Service`.
- Networking and synchronization: `...Sync`, `...NetworkBehaviour`, `...RelayService`.
- Tests: system name followed by the expected behavior.

## Known Limitations

- Educational validation with the final target users still needs to be completed systematically.
- GPS behavior must be tested on real mobile devices and in the physical space of the Jardín de los Sentidos.
- Multiplayer behavior should be validated under real network conditions, including latency and connection issues.
- Quests balance and educational content may require adjustment after playtesting.
- Some editor utilities generate UI, prefabs or scene configuration, so generated changes should be reviewed before being consolidated.
