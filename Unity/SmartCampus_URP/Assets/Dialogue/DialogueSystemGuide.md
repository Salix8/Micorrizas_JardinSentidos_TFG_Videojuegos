# Sistema de dialogos

## Integracion antes de minijuegos

El punto de integracion esta en `CoopMinigameCatalog.asset`.
Cada entrada del catalogo tiene dos campos nuevos:

- `storyDialogueActOrLocation`: reproduce todas las filas del CSV con ese `Act/Location`.
- `storyDialogueLineIds`: reproduce una secuencia concreta de `String ID`. Si hay IDs aqui, tienen prioridad sobre `storyDialogueActOrLocation`.

Flujo actual:

1. El jugador pulsa un minijuego en el launcher.
2. `CoopMinigameLauncherUIController` comprueba si la entrada tiene dialogo.
3. Si `Story Dialogues Enabled` esta activo, reproduce el dialogo con `DialogueUIController`.
4. Al cerrar o terminar el dialogo, se lanza el minijuego.
5. Si los dialogos estan desactivados, el minijuego se abre directamente.

## Checkbox para testear sin historia

El checkbox global esta en:

`Assets/Resources/DialogueSettingsConfig.asset`

Campo:

`Story Dialogues Enabled`

Para testear rapido, desmarcalo. No hace falta tocar cada escena ni borrar referencias.

## Escena requerida

En la escena donde este el launcher debe existir un `DialogueUIController` con:

- `Dialogue Database`: `Assets/Dialogue/DialogueDatabaseConfig.asset`
- `Dialogue View`: componente que pinta textos y botones.
- `Sound Player`: componente con `AudioSource`.

`DialogueDatabaseConfig.asset` ya referencia el CSV `Narrativa.xlsx - Localizacion_Deeproot.csv`.

## Sonido tipo Animal Crossing

El perfil esta en:

`Assets/Dialogue/DialogueAudioProfileConfig.asset`

Opciones principales:

- `Default Blip Clip`: un clip unico repetido.
- `Default Letter Clips`: banco de clips por letra/fonema.
- `Use Letter Based Clip Selection`: elige clip segun la letra visible.
- `Use Letter Based Pitch`: cambia pitch segun la letra.
- `Characters Per Blip`: cada cuantas letras suena.
- `Character Voices`: overrides por personaje, por ejemplo `Deeproot`.

Si solo tienes un clip, asignalo a `Default Blip Clip`.
Si tienes clips estilo Animal Crossing por letra o fonema, asigna el array en `Default Letter Clips` o en el override del personaje.
