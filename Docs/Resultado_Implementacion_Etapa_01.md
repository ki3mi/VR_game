# Resultado de implementación: etapa 1

## Estado

**Mapa y entorno explorable implementados y verificados con XR Interaction Simulator.**

La escena activa de Unity es [DataCenter_VR.unity](C:/Games/grupo03/Assets/Scenes/DataCenter_VR.unity), guardada en modo edición. Se conservaron las escenas de la instalación.

- [Guía para ejecutar y mantener esta versión](C:/Games/grupo03/Docs/Guia_Ejecucion_Etapa_01.md).
- [Plan de la etapa 1 con tareas completadas](C:/Games/grupo03/Docs/Etapa_01_Mapa_y_Entorno_Explorable.md).

## Contenido entregado

- Sala cerrada y pasillo con **cuatro racks**, servidores, LEDs animados y señalización.
- Terminal SWIFT, switch, impresora apagada y panel de controles con sus posiciones e identificadores estables.
- Portal funcional y **seis estaciones de teletransporte**: entrada, racks, terminal, switch, impresora y controles.
- **Cinco objetos de inspección**, con respuesta al apuntar, fichas legibles y cierre mediante teclado o ratón.
- Cámara con seguimiento del HMD creado por XR Interaction Simulator, retícula central y selección con Espacio o clic izquierdo.
- Movimiento libre mediante WASD y E/Q en XR Interaction Simulator, añadido por petición posterior del equipo; se conserva el teletransporte.
- Ambiente sonoro original de servidores, materiales y prefabs reutilizables.

## Actualización: movimiento libre en el simulador

Se habilitó el desplazamiento nativo de XR Interaction Simulator por petición del equipo. La escena conserva **un único simulador**, el portal, los seis destinos y las cinco inspecciones.

- **W/A/S/D:** avanzar, retroceder y desplazarse lateralmente.
- **E/Q:** subir y bajar.
- **Clic derecho + ratón:** mirar; **H** selecciona la cabeza si se ha cambiado el dispositivo simulado.
- **Espacio o clic izquierdo:** seleccionar; **Esc:** cerrar fichas.

Las velocidades XYZ se restauraron a `0.2`, con multiplicador corporal `5`. La interfaz y el constructor de escena incorporan esta configuración, y la guía y los requisitos de pruebas reflejan la actualización.

La ejecución **`Run_20261105_045854`** superó las siete comprobaciones: se midieron desplazamientos de aproximadamente **0,25 m con cada tecla W, S, A, D, E y Q** durante pulsaciones de prueba, giro de cámara por ratón, 16 teletransportes, seis estaciones y cinco inspecciones, con cero errores de ejecución.

- [Informe de la actualización](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/Resultados_Etapa_01.md).
- [Mediciones de movimiento en JSON](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/resultados.json).
- [Vista con los controles actualizados](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/04_interior.png).

## Validación inicial observada

Ejecución validada: **`Run_20261105_044916`**, iniciada el 4 de noviembre de 2026 a las 23:49 en la zona horaria de Bogotá; los nombres de carpetas y el registro técnico usan UTC.

| Comprobación | Resultado observado |
| --- | --- |
| E1-P01: inicio y orientación | XRSimulatedHMD presente, giro real mediante entrada del simulador y cámara a 1,65 m. |
| E1-P02: portal y destinos | Portal utilizado, seis destinos alcanzados y regreso a la entrada. |
| E1-P03: navegación | Tecla W sin desplazamiento continuo; recorrido mediante teletransporte. |
| E1-P04: inventario y ambiente | Cuatro racks y objetos obligatorios presentes; audio reproduciéndose; cuatro componentes de LEDs animados, diez LEDs por rack. |
| E1-P05: interacción | Cinco objetos seleccionados por mirada y Espacio, con indicación visual al apuntar. |
| E1-P06: fichas | Cinco fichas abiertas y cerradas; cero desbordamientos de texto TMP; capturas revisadas visualmente. |
| E1-P07: errores | Recorrido finalizado con cero errores de ejecución; consola del Editor revisada después de salir de Play Mode, sin errores ni advertencias. |

**Balance: 7 de 7 comprobaciones satisfactorias, 16 teletransportes durante la prueba, 6 estaciones visitadas y 5 objetos inspeccionados.**

La prueba utiliza eventos de teclado y ratón en el Input System. El simulador produce el seguimiento de cabeza; la selección atraviesa el raycast real de la cámara. La herramienta orienta horizontalmente el origen XR para repetir el recorrido y ajusta la inclinación mediante entradas de ratón del simulador.

## Evidencias

- [Informe de pruebas funcionales](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/Resultados_Etapa_01.md).
- [Resultados y tiempos de captura en JSON](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/resultados.json).
- [Vista interior con interfaz](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/04_interior.png).
- [Inspección de la terminal](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Terminal_SWIFT.png).
- [Inspección del panel](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Panel_Controles.png).
- [Video del recorrido](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/video_recorrido.avi): AVI MJPEG, 35 segundos, 960 × 799, 4 fps, generado a partir de 139 capturas reales con sus tiempos. El archivo no incluye pista de audio; su reproducción dentro de Unity se comprobó por separado.

Las ejecuciones anteriores se conservan como historial de depuración. La carpeta identificada arriba corresponde a la validación inicial, realizada antes de habilitar el movimiento libre. El informe de esa ejecución incluye la revisión visual de las cinco fichas y su estado final es `passed`.

## Ajustes de compatibilidad realizados

- Se restablece la altura local del origen de cámara al arrancar el simulador en el Editor: la combinación instalada de XR Interaction Simulator 3.6 y AR Foundation reiniciaba ese desplazamiento durante la carga de escena.
- La interfaz de pruebas en el Editor usa Canvas Overlay para evitar que los textos del escenario se dibujen sobre las fichas. La configuración fuera del Editor conserva el Canvas en espacio de mundo previsto como base para VR.
- Se ocultó la interfaz auxiliar de la muestra del simulador y se añadieron instrucciones compactas propias.
- La escena y los prefabs generados usan Liberation Sans; la relación inicial de recursos, licencias y herramientas de IA está en la guía de ejecución.

## Continuidad

La base queda preparada para la [etapa 2: modo ataque y evidencias](C:/Games/grupo03/Docs/Etapa_02_Modo_Ataque_y_Evidencias.md). Las fichas actuales son provisionales; todavía no se han implementado reloj, envío de órdenes, registro de evidencias ni activación de defensas.

La adaptación y validación de entrega con Cardboard/Android, el video académico desde celular y la integración final de créditos corresponden al cierre posterior indicado en el contexto. Esta etapa se comprobó en el Editor con XR Interaction Simulator.

La carpeta de trabajo no contiene un repositorio Git inicializado; los cambios están guardados en archivos locales. No se publicaron recursos ni se creó un repositorio remoto durante esta implementación.
