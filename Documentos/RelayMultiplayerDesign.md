# Relay Co-op Design

## Revised recommendation

Because this is a 3 to 6 player co-op game with a real waiting room, a shared main map, and synchronized mini-games, the project should use a dedicated **lobby scene** before the map. This is a change from the earlier lighter recommendation.

The best flow now is:

1. `Lobby` scene
2. Host creates a Relay allocation and shares the join code
3. Players join the lobby scene
4. Host can only leave the lobby when 3 to 6 players are connected
5. NGO scene management transitions everyone to the shared map scene
6. Server-authoritative logic moves everyone into one of the five mini-game scenes at the same time
7. After each mini-game, everyone returns to the shared map

This fits the co-op structure much better than treating the lobby as just a popup. The lobby is now a real game phase, not only a connection screen.

## Lobby scene versus Unity Lobby service

These are different things:

- **Lobby scene**: a Unity scene in your game where players wait, see team size, and start the co-op run
- **Unity Lobby service**: a UGS backend service for discoverable/public lobbies and richer pre-game metadata

For this project, Relay join codes are still enough unless you later need public room browsing, host migration metadata, or richer ready-state systems.

## Co-op-specific scene architecture

- `Lobby`: connection, player count, readiness gate, session start
- `UJI`: shared overworld map
- `MiniGame01` to `MiniGame05`: synchronized cooperative tasks

All players load the same scene names at the same time, but each player can see different information inside those scenes. That difference should come from player slot or role assignment, not from loading different scenes per player.

## How player-specific information should work

The cleanest approach is:

1. Assign each connected player a stable slot inside the co-op session
2. Use that slot to decide which clue, prompt, or private information set is shown locally
3. Keep the simulation and scene transitions shared for all players

That means the same mini-game scene can contain several information variants, while each client only reveals the variant that belongs to its slot. This matches your requirement that players cannot complete the puzzle alone.

## Current implementation in the project

- Unity packages for `com.unity.services.multiplayer`, `com.unity.netcode.gameobjects`, and `com.unity.transport`
- `RelayConnectionService`
  - Relay join-code hosting/joining
  - anonymous UGS sign-in
  - lobby size gate for 3 to 6 players
  - synchronized transition from lobby to main map
- `CoopSessionCoordinator`
  - authoritative co-op phase tracking
  - synchronized transitions between lobby, map, and mini-games
  - stable player slot ordering
- `CoopInformationPresenter`
  - local activation of per-player information variants in a shared scene
- `MultiplayerMenuController`
  - lobby UI controller updated for the 3 to 6 player rule
- `RelayConnectionProtocol`
  - transport selection for `udp`, `dtls`, and `wss`

## Scene and object setup in Unity

1. Create a `Lobby` scene and make it the first enabled scene in Build Settings.
2. Keep `UJI` as the shared main map scene.
3. Create five mini-game scenes and add all of them to Build Settings.
4. In the `Lobby` scene, add a persistent networking root with:
   - `NetworkManager`
   - `UnityTransport`
   - `RelayConnectionService`
5. Enable NGO scene management on the `NetworkManager`.
6. Add a `NetworkObject` with `CoopSessionCoordinator`.
7. Configure the coordinator with:
   - `Lobby` as the lobby scene
   - `UJI` as the main map
   - the five mini-game scene names
8. Add a Canvas in the lobby and wire `MultiplayerMenuController`.
9. In shared scenes, place `CoopInformationPresenter` wherever a clue or information block needs per-player variation.

## Design consequence of the 3 to 6 player rule

- The host can open the session for up to 6 total players.
- The co-op run should not begin until at least 3 players are present.
- If a player disconnects and the team drops below 3, the safest default is to block the next phase transition until the team size is valid again.

## UGS setup still required

1. Link the project to Unity Dashboard.
2. Enable Relay for the project.
3. Ensure anonymous authentication is available.
4. If you later target WebGL, use `wss` for the transport protocol.
