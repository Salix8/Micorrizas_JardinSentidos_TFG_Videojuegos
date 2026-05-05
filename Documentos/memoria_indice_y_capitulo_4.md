# Propuesta de índice y borrador del capítulo 4

## Índice recomendado

La estructura que mejor encaja con la rúbrica y con las memorias modelo no es mantener un capítulo 4 intermedio indefinido, sino cerrar bien el análisis y convertir el capítulo 4 en el verdadero bloque de desarrollo y resultados.

### 1. Introduction
- 1.1 Work Motivation
- 1.2 Objectives
- 1.3 Environment and Initial State

### 2. Planning and Resources Evaluation
- 2.1 Planning
- 2.2 Cost and Resource Evaluation
- 2.3 Planned vs. Real Dedication

### 3. System Analysis and Design
- 3.1 Requirement Analysis
- 3.1.1 Functional Requirements
- 3.1.2 Non-Functional Requirements
- 3.2 Game Design
- 3.2.1 Core Gameplay Loop
- 3.2.2 Cooperative Design
- 3.2.3 Minigame Structure
- 3.3 System Architecture
- 3.3.1 Multiplayer Session Flow
- 3.3.2 Geolocation and World Map
- 3.3.3 Reusable Minigame Framework
- 3.4 Interface Design

### 4. Work Development and Results
4.1 Work Development
4.1.1 Project Pivot and Scope Redefinition
4.1.2 Multiplayer Session Implementation
4.1.3 Geolocation Implementation
4.1.4 Development of the Five Minigames
4.1.5 Interface Adaptation for Outdoor Mobile Use
4.1.6 Main Technical Problems and Solutions
4.2 Results
4.2.1 Objectives Achieved
4.2.2 Final Prototype Capabilities
4.2.3 Limitations and Pending Validation
4.2.4 Comparison Between Planned and Real Work

### 5. Conclusions and Future Work
- 5.1 Conclusions
- 5.2 Future Work

### Bibliography

### Appendix A. Source Code

## Por qué esta estructura es mejor para Micorrizas

- Se alinea con dos de las tres memorias de ejemplo, que usan la secuencia clásica de introducción, planificación, análisis y diseño, desarrollo y resultados, y conclusiones.
- Evita un capítulo 4 vacío o poco defendible.
- Permite usar el GDD como material de apoyo dentro del análisis, pero sin convertir la memoria en un segundo GDD.
- Se ajusta mejor a la rúbrica, que distingue claramente entre análisis, desarrollo y resultados.

## Qué corregir de la versión actual antes de seguir

- El índice actual está roto: existe un `4.Titulo??`, el salto posterior va al capítulo 5 y luego al 7.
- El apartado `3.2 Interface Design` no puede quedarse como una duda. Sí es relevante en tu TFG porque el proyecto depende de uso móvil, lectura en exterior, cooperación por múltiples dispositivos y layouts adaptativos.
- El capítulo 4 actual no debe ser un “GDD resumido” si luego ya tienes un capítulo 3 de análisis y diseño.
- Los capítulos 5 en adelante deben reescribirse desde hechos verificables del proyecto, no desde texto provisional.

## Borrador propuesto del capítulo 4

Este texto está planteado en castellano para revisión de contenido. Más adelante puede traducirse y pulirse en inglés.

---

# 4. Work Development and Results

## 4.1 Work Development

### 4.1.1 Project Pivot and Scope Redefinition

Uno de los aspectos más importantes del desarrollo de Micorrizas fue la redefinición del proyecto durante sus primeras etapas. La idea inicial no estaba planteada como una aplicación móvil cooperativa, sino como una experiencia más cercana a la realidad mixta, en la que la información y ciertos elementos lúdicos se superponían directamente sobre el entorno físico del Jardín de los Sentidos. En esa primera versión conceptual, parte del planteamiento narrativo se apoyaba en la contaminación de la red de micorrizas por la presencia humana, y algunos minijuegos giraban alrededor de evitar zonas problemáticas o manipular nodos superpuestos en el espacio.

Sin embargo, tras las reuniones con el profesorado implicado en la actividad didáctica original, se comprobó que ese enfoque no respondía bien a la necesidad real del proyecto. El objetivo no era crear una experiencia puntual o excepcional, sino una herramienta complementaria a la docencia que pudiera ser utilizada por grupos completos de estudiantes de forma accesible. Esto hacía inviable depender de un hardware específico como gafas de realidad mixta o de una puesta en escena demasiado condicionada por la tecnología.

Como consecuencia, el proyecto pivotó hacia una aplicación móvil geolocalizada. Este cambio de plataforma obligó a rediseñar tanto la interacción como la narrativa. También supuso replantear los minijuegos, ya que la lógica de superposición espacial dejó de tener sentido en el nuevo formato. A partir de este punto, el diseño pasó a centrarse en la cooperación entre dispositivos, en la exploración física del jardín y en la reconstrucción compartida del conocimiento natural.

### 4.1.2 Multiplayer Session Implementation

El sistema multijugador fue uno de los pilares técnicos del proyecto, ya que Micorrizas no está pensado como una experiencia individual, sino como una actividad cooperativa para grupos de entre dos y seis jugadores. Esta decisión de diseño afectó tanto a la arquitectura técnica como a la estructura de juego.

La implementación actual organiza la sesión cooperativa en torno a un flujo claro de creación de sala, unión mediante código, espera en lobby, transición al mapa principal y acceso sincronizado a cada minijuego. Para ello se utiliza un sistema basado en Unity Relay y Netcode for GameObjects. El host crea la sesión, obtiene un código de unión y el resto de jugadores accede a la partida introduciendo dicho código. Una vez formada la sala, el sistema valida que se cumpla el número mínimo de jugadores antes de permitir el inicio de la experiencia.

La coordinación general de la partida recae sobre un controlador de sesión que sincroniza el estado compartido entre todos los dispositivos. Este componente gestiona la fase actual del juego, el minijuego activo y la asignación de slots de jugador. Gracias a esta estructura, el grupo puede pasar del lobby al mapa y del mapa a cada minijuego manteniendo una experiencia coherente para todos los participantes.

Desde el punto de vista de diseño, este multijugador no se limita a conectar varios clientes. Su función es reforzar la cooperación como mecánica principal. El sistema está construido de manera que la progresión y los resultados sean compartidos, evitando una lógica competitiva entre miembros del mismo grupo. Esta decisión se corresponde con el objetivo didáctico del proyecto, donde interesa priorizar la interpretación conjunta del entorno sobre el rendimiento individual.

### 4.1.3 Geolocation Implementation

La geolocalización ha sido uno de los hitos técnicos más relevantes del TFG. A diferencia de otros sistemas que podrían haberse resuelto de forma más convencional en un entorno completamente virtual, en Micorrizas la posición física de los jugadores forma parte de la lógica de interacción. El videojuego no representa simplemente un jardín ficticio, sino que utiliza el Jardín de los Sentidos real como espacio de juego.

El sistema de geolocalización obtiene la posición del dispositivo móvil, controla el estado del servicio de localización y gestiona aspectos problemáticos del entorno móvil, como la concesión de permisos, la activación manual del GPS por parte del usuario o los tiempos de espera durante la inicialización. La implementación actual no asume que la localización funcionará a la primera, sino que contempla reintentos, mensajes de diagnóstico y estados intermedios, algo especialmente importante en un proyecto pensado para exteriores y para pruebas en distintos dispositivos.

Una vez obtenida la lectura local del GPS, esta se sincroniza con la sesión cooperativa mediante un sistema de publicación del estado de cada jugador. Esto permite compartir la posición de todos los participantes y representarla sobre el mapa, lo cual refuerza la percepción de grupo dentro del entorno real. Para ello también se implementó un sistema de marcadores que diferencia visualmente entre el dispositivo local y los remotos.

La geolocalización, por tanto, no solo cumple una función técnica de posicionamiento, sino que conecta el espacio físico, la interfaz digital y la estructura cooperativa del juego.

### 4.1.4 Development of the Five Minigames

Otro de los núcleos del desarrollo fue la implementación de cinco minijuegos diferenciados, cada uno asociado a un jardín sensorial y a un tipo de aprendizaje distinto. El principal reto aquí no era únicamente crear cinco experiencias diferentes, sino hacerlo manteniendo coherencia estructural y reutilización técnica.

Para resolverlo, se desarrolló una base común para minijuegos cooperativos. Esta base compartida gestiona el flujo de tutorial, espera de jugadores, inicio de partida, desarrollo y pantalla de resultados. A partir de ella, cada minijuego implementa únicamente su lógica específica, sus modelos de datos y su sistema de evaluación.

El minijuego del jardín de la vista se centra en la identificación visual de especies mediante cartas e imágenes, y plantea decisiones rápidas sobre si una planta pertenece o no al entorno observado. El del oído distribuye el audio y las opciones entre distintos dispositivos, obligando a la comunicación para clasificar correctamente sonidos ambientales o de fauna. El del tacto adopta una lógica de deducción progresiva basada en atributos de plantas, en la que el historial compartido de intentos se convierte en una parte importante de la cooperación. El del gusto utiliza una dinámica de parejas distribuidas entre dispositivos, haciendo imposible resolver el reto desde un solo móvil. Por último, el del olfato plantea una clasificación taxonómica de plantas según su función o uso, también con información repartida entre jugadores.

Aunque sus mecánicas son distintas, los cinco minijuegos comparten varios principios de diseño: información fragmentada, dependencia del diálogo entre jugadores, evaluación colectiva y relación directa con los objetivos didácticos del jardín correspondiente. Esto refuerza la identidad unitaria del proyecto y evita que la experiencia se convierta en una colección inconexa de actividades.

### 4.1.5 Interface Adaptation for Outdoor Mobile Use

Aunque la interfaz no sea el elemento más vistoso del TFG, su adaptación a las condiciones reales de uso ha sido importante en el desarrollo. Micorrizas no se juega sentado frente a un ordenador, sino caminando en exterior, con varios dispositivos simultáneos y en un contexto en el que los jugadores deben mirar tanto a la pantalla como al entorno físico.

Por ello, el diseño de UI se orientó a la legibilidad, la simplicidad y la capacidad de adaptación a diferentes móviles. El proyecto incluye controladores de layout responsive, ajuste a safe area y vistas reutilizables para tutoriales, resultados y paneles de minijuegos. Esto permite que la experiencia sea más robusta frente a cambios de resolución, proporción de pantalla y condiciones de uso reales.

También se tomaron decisiones de diseño que reducen fricción. Por ejemplo, se evita incorporar sistemas innecesarios como un chat interno, ya que la cooperación debe producirse cara a cara. Del mismo modo, la interfaz del mapa no pretende sustituir el entorno físico, sino funcionar como apoyo de orientación y progreso.

### 4.1.6 Main Technical Problems and Solutions

Durante el desarrollo aparecieron varios problemas técnicos y de diseño que condicionaron el resultado final. El primero fue la ya mencionada redefinición del alcance del proyecto, que obligó a abandonar una idea inicial más ambiciosa tecnológicamente pero poco viable en el contexto real de uso.

Otro problema relevante fue la sincronización del flujo cooperativo. No bastaba con que varios jugadores compartieran partida; era necesario asegurar que todos atravesaran las mismas fases de forma coordinada y que el sistema no se rompiera si un usuario iba más rápido que el resto. La solución fue centralizar el estado del minijuego y de la sesión en estructuras de red que marcan claramente el tutorial, la fase de juego y la publicación del resultado.

La geolocalización también introdujo dificultades específicas: retrasos en la inicialización del GPS, falta de permisos, imprecisión del posicionamiento o diferencias entre dispositivos. Esto llevó a implementar una lógica más defensiva en lugar de depender de un comportamiento ideal del hardware.

Por último, un reto importante fue conseguir que cinco minijuegos distintos no generaran cinco sistemas aislados. La solución adoptada fue diseñar una base cooperativa común sobre la que cada uno pudiera especializarse. Esta decisión mejoró la mantenibilidad del proyecto y hace que el sistema sea más defendible desde el punto de vista arquitectónico.

## 4.2 Results

### 4.2.1 Objectives Achieved

En su estado actual, Micorrizas ya permite afirmar que los objetivos principales del proyecto han sido abordados de forma coherente. Se ha desarrollado un prototipo móvil con un planteamiento didáctico claro, ligado al Jardín de los Sentidos y orientado a promover observación, cooperación e interpretación compartida del entorno.

También se ha implementado una base multijugador funcional que permite crear sesiones cooperativas, unirse mediante código, controlar el número de jugadores y sincronizar la progresión del grupo. Además, el sistema integra geolocalización y representación del grupo sobre el mapa, lo que cumple con uno de los objetivos técnicos más característicos del TFG.

Por último, el proyecto cuenta con cinco minijuegos diferenciados que cubren distintos ámbitos perceptivos y didácticos, y que comparten una estructura cooperativa común.

### 4.2.2 Final Prototype Capabilities

El prototipo actual permite:

- crear y unirse a sesiones cooperativas de entre dos y seis jugadores;
- desplazarse por el mapa principal del jardín con representación geolocalizada;
- activar y jugar cinco minijuegos diferentes;
- sincronizar estado, progreso y resultados entre dispositivos;
- distribuir información parcial entre jugadores para forzar cooperación;
- mostrar resultados compartidos al finalizar cada reto.

Estas capacidades muestran que el proyecto ha superado la fase de idea conceptual y se ha materializado en un sistema jugable con identidad propia.

### 4.2.3 Limitations and Pending Validation

Aun así, el proyecto presenta limitaciones que deben reconocerse de forma explícita. La primera es que todavía necesita una validación más sistemática con usuarios del perfil objetivo, especialmente desde el punto de vista didáctico. El hecho de que el sistema funcione técnicamente no implica por sí mismo que la experiencia ya esté optimizada como herramienta educativa.

También quedan aspectos mejorables relacionados con el ajuste fino de contenido, el balance entre minijuegos, el comportamiento de la geolocalización en contextos reales y la robustez del sistema en un mayor abanico de dispositivos móviles.

Estas limitaciones no invalidan el resultado alcanzado, pero sí delimitan honestamente el estado del proyecto y ayudan a justificar futuras líneas de mejora.

### 4.2.4 Comparison Between Planned and Real Work

Este subapartado debe utilizarse para cerrar el capítulo con una tabla comparativa entre la planificación inicial y el tiempo realmente invertido en cada bloque de trabajo. En tu caso es especialmente importante porque el proyecto ha sufrido una reformulación de alcance, y esa desviación está plenamente justificada.

Aquí conviene explicar, al menos, tres desviaciones:

- el tiempo adicional dedicado al rediseño del proyecto tras abandonar la realidad mixta;
- el coste técnico no previsto inicialmente de la sincronización multijugador y la geolocalización;
- el esfuerzo derivado de convertir varios minijuegos distintos en un framework común reutilizable.

---

## Recomendación final para el capítulo 4

No conviertas este capítulo en una repetición del GDD. El GDD te sirve como inspiración para recordar decisiones de diseño, pero el capítulo 4 de la memoria debe contar qué se desarrolló realmente, qué problemas aparecieron, qué cambió respecto a la idea inicial y qué resultado existe hoy.
