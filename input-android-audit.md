# Android Input Audit

Fecha: 2026-05-13

## Resumen

El proyecto usa `com.unity.inputsystem` `1.18.0` y actualmente tiene `Active Input Handling = Both` en `Unity/SmartCampus_URP/ProjectSettings/ProjectSettings.asset`.

Tras la auditoria y el refactor aplicado:
- La UI de todas las escenas auditadas usa `InputSystemUIInputModule`.
- La camara de `UJI` ya usa solo `UnityEngine.InputSystem`.
- La unica dependencia funcional relevante del input legacy que queda localizada es `DeviceGpsService`, a traves de `Input.location`.

Esto deja la mezcla reducida a una excepcion tecnica clara: GPS/sensores del dispositivo en Android.

## Inventario por sistema

### Legacy Input API

| Elemento | Ruta | Uso | Riesgo | Dependencia Android |
| --- | --- | --- | --- | --- |
| `DeviceGpsService` | `Unity/SmartCampus_URP/Assets/Scripts/DeviceGpsService.cs` | `Input.location`, `LocationServiceStatus` | Alta: bloquea pasar a `New` sin validar GPS | Si |

### New Input System API

| Elemento | Ruta | Uso | Riesgo | Dependencia Android |
| --- | --- | --- | --- | --- |
| `ArcGISTopDownCameraController` | `Unity/SmartCampus_URP/Assets/Scripts/ArcGISTopDownCameraController.cs` | `EnhancedTouch`, `Mouse.current` | Media: input tactil de camara | Si |
| `Lobby` | `Unity/SmartCampus_URP/Assets/Scenes/Lobby.unity` | `InputSystemUIInputModule` | Baja | Si |
| `UJI` | `Unity/SmartCampus_URP/Assets/Scenes/UJI.unity` | `InputSystemUIInputModule` | Baja | Si |
| `AudioWordConsensusMinigame` | `Unity/SmartCampus_URP/Assets/Scenes/AudioWordConsensusMinigame.unity` | `InputSystemUIInputModule` | Baja | Si |
| `CollaborativePlantGuessMinigame` | `Unity/SmartCampus_URP/Assets/Scenes/CollaborativePlantGuessMinigame.unity` | `InputSystemUIInputModule` | Baja | Si |
| `DistributedPairsMinigame` | `Unity/SmartCampus_URP/Assets/Scenes/DistributedPairsMinigame.unity` | `InputSystemUIInputModule` | Baja | Si |
| `GardenImageVotingMinigame` | `Unity/SmartCampus_URP/Assets/Scenes/GardenImageVotingMinigame.unity` | `InputSystemUIInputModule` | Baja | Si |
| `GardenSmellTaxonomyMinigame` | `Unity/SmartCampus_URP/Assets/Scenes/GardenSmellTaxonomyMinigame.unity` | `InputSystemUIInputModule` | Baja | Si |

### Sistemas hibridos

| Elemento | Estado actual | Observacion |
| --- | --- | --- |
| Proyecto completo | Hibrido por `PlayerSettings` | `Both` sigue activo porque el GPS aun depende de `Input.location` |
| `ArcGISTopDownCameraController` | Resuelto | Antes mezclaba legacy y new; ahora solo usa new input |

## Dependencias legacy obligatorias

### Imprescindibles hoy

- `Input.location`
- `LocationServiceStatus`
- permiso Android para ubicacion en `DeviceGpsService`

### Migrables o ya migradas

- Input tactil y raton de la camara
- Input UI de `EventSystem` y canvases
- Navegacion de menus y minijuegos

## Evaluacion de configuraciones objetivo

Escala: 1 muy mala, 5 muy buena.

| Opcion | GPS Android | UI actual | Complejidad | Riesgo movil | Mantenimiento | Conclusión |
| --- | --- | --- | --- | --- | --- | --- |
| `Both` | 4 | 5 | 5 | 2 | 2 | Solucion temporal controlada mientras se valida GPS |
| `Input System Package (New)` | 2 | 5 | 3 | 3 | 5 | Objetivo final deseable, no listo aun |
| `Input Manager (Old)` | 3 | 1 | 1 | 2 | 1 | Descartado |

## Matriz de experimentos

| ID | Configuracion | Escena | Funcionalidad | Resultado esperado | Estado |
| --- | --- | --- | --- | --- | --- |
| A1 | `Both` | `Lobby` | Botones y TMP input field | UI responde | Pendiente build Android |
| A2 | `Both` | `UJI` | Paneo de camara | Arrastre tactil responde | Pendiente build Android |
| A3 | `Both` | `UJI` | GPS local | `Initializing -> Running` | Pendiente build Android |
| B1 | `New` | `Lobby` | Botones y TMP input field | UI responde | Pendiente rama de prueba |
| B2 | `New` | `UJI` | Paneo de camara | Arrastre tactil responde | Pendiente rama de prueba |
| B3 | `New` | `UJI` | GPS local | Posible bloqueo en Android | Pendiente rama de prueba |
| C1 | `Both` | `UJI` | Camara aislada en new input | Sin uso de `Input.*` en camara | Hecho en codigo |
| D1 | `Both` | build GPS minimizada | GPS sin paneo/camara | Aislar si falla `Input.location` | Pendiente build Android |

## Recomendacion actual

### Corto plazo

- Mantener `Active Input Handling = Both` para no romper `DeviceGpsService`.
- Considerar `DeviceGpsService` como unica excepcion legacy permitida.
- No reintroducir `Input.*` en scripts de UI, camara o gameplay.

### Medio plazo

- Ejecutar una build de prueba con `Input System Package (New)` en una rama aislada.
- Confirmar si el bloqueo del GPS en Android reproduce el issue oficial de Unity.
- Si se confirma, documentar `Both` como workaround temporal del TFG.

### Largo plazo

- Reintentar la unificacion completa cuando Unity permita una ruta estable de geolocalizacion sin depender de `Input.location`, o cuando el proyecto adopte otro wrapper validado.

## Fuentes externas contrastadas

- Unity Input System migration guide:
  <https://docs.unity.cn/Packages/com.unity.inputsystem%401.13/manual/Migration.html>
- Unity UI support:
  <https://docs.unity.cn/Packages/com.unity.inputsystem%401.6/manual/UISupport.html>
- Unity Input Modules manual:
  <https://docs.unity.cn/2023.3/Documentation/Manual/InputModules.html>
- Unity issue: `LocationService` stuck on Android with new Input System:
  <https://issuetracker.unity3d.com/issues/android-locationservice-stuck-initializing-with-new-inputsystem>
- Unity issue: location without high accuracy mode:
  <https://issuetracker.unity3d.com/issues/android-locationservice-doesnt-work-without-high-accuracy-mode>
- Unity issue: touch broken with `Both`:
  <https://issuetracker.unity3d.com/issues/touch-screen-support-is-broken-when-active-input-handling-is-set-to-both>
