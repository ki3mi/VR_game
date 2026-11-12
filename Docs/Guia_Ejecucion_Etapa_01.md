# Ejecutar la etapa 1: centro de datos

## Abrir y explorar

1. Abrir el proyecto `C:/Games/grupo03` en Unity 6.
2. Abrir la escena [DataCenter_VR.unity](C:/Games/grupo03/Assets/Scenes/DataCenter_VR.unity).
3. Entrar en Play Mode y dar foco a Game View.
4. Moverse libremente con **W/A/S/D** y subir o bajar con **E/Q**.
5. Orientar la mirada con el ratón manteniendo el botón derecho. La tecla **H** selecciona el dispositivo de cabeza del XR Interaction Simulator si se ha cambiado de dispositivo.
6. Centrar la retícula sobre el portal, un destino de suelo o un objeto de inspección. Seleccionar con **Espacio** o **clic izquierdo**, soltando antes el botón derecho.
7. Cerrar una ficha con **Esc**, **Espacio** o **clic izquierdo** y continuar el recorrido.

El XR Interaction Simulator permite **movimiento libre y teletransporte** en las pruebas del Editor, según la actualización solicitada por el equipo. Se restauraron sus velocidades nativas de traslación a `0.2` por eje, con el multiplicador de cuerpo `5` del prefab, equivalente a aproximadamente 1 m/s. Los seis destinos siguen disponibles para desplazamientos directos.

## Contenido de esta versión

- Portal de entrada al BANCO ANDINO y centro de datos con suelo, paredes, techo y señalización.
- Cuatro racks con servidores y LEDs animados.
- Terminal SWIFT, switch, impresora apagada y panel de controles.
- Seis destinos: entrada, racks, terminal, switch, impresora y controles.
- Cinco fichas: `Rack_01`, `Terminal_SWIFT`, `Switch_Barato`, `Impresora` y `Panel_Controles`.
- Movimiento libre con el simulador, retícula, indicación del objeto seleccionado, progreso del recorrido y sonido ambiental de servidores.

El visor de logs, las órdenes de pago y los interruptores de defensa se incorporarán en las etapas 2 y 3. Las fichas de esta versión identifican los objetos y explican esas ampliaciones.

## Archivos y herramientas de mantenimiento

| Elemento | Ubicación |
| --- | --- |
| Escena | [DataCenter_VR.unity](C:/Games/grupo03/Assets/Scenes/DataCenter_VR.unity) |
| Comportamiento de exploración | [ExplorationController.cs](C:/Games/grupo03/Assets/AndesSim/Scripts/ExplorationController.cs) |
| Objetos inspeccionables y destinos | [ExplorationTarget.cs](C:/Games/grupo03/Assets/AndesSim/Scripts/ExplorationTarget.cs) |
| Constructor inicial | [DataCenterSceneBuilder.cs](C:/Games/grupo03/Assets/AndesSim/Editor/DataCenterSceneBuilder.cs) |
| Validación con el simulador | [StageOneValidation.cs](C:/Games/grupo03/Assets/AndesSim/Editor/StageOneValidation.cs) |
| Materiales | `Assets/AndesSim/Art/Materials/` |
| Prefabs | `Assets/AndesSim/Prefabs/` |

El menú **Andes-Sim > Etapa 1 > Crear centro de datos** crea la escena cuando todavía no existe. Si ya existe, la abre y conserva las modificaciones realizadas.

## Repetir la validación

Con la escena abierta y fuera de Play Mode, ejecutar **Andes-Sim > Etapa 1 > Validar en simulador**. La herramienta entra en Play Mode, prueba el seguimiento del HMD simulado y el movimiento libre, selecciona destinos y objetos mediante eventos del Input System y sale al terminar.

Los resultados JSON, el informe Markdown, las capturas y los frames se guardan en `Docs/Evidencias_Etapa_01/Run_<fecha UTC>/`. Cada ejecución conserva su propio resultado, incluidas las que hayan fallado. El informe de cierre identifica la ejecución validada.

La comprobación usa el XR Interaction Simulator real y la selección por raycast desde la cámara; no llama directamente a los eventos de selección o teletransporte. La orientación horizontal del origen se ajusta en el recorrido automatizado para hacerlo repetible, y la inclinación se produce mediante entrada del ratón consumida por el simulador.

La revisión de legibilidad se realiza sobre las capturas. La comodidad con visor y la validación Android se mantienen como verificaciones de entrega en dispositivo, según el contexto del proyecto.

## Recursos y créditos iniciales

| Recurso | Autor o procedencia | Licencia o condición documentada |
| --- | --- | --- |
| Arquitectura, racks, objetos, materiales y señalización | Creación original para este proyecto mediante el constructor de escena. | Sin modelos ni texturas externos añadidos. |
| Zumbido de servidores | Síntesis original en `ServerAmbience.cs`. | Sin grabaciones externas. |
| XR Interaction Simulator y XR Interaction Toolkit | Unity Technologies, paquete y muestra ya instalados. | Unity Companion License; archivo local del paquete `LICENSE.md`. |
| XR Core Utilities | Unity Technologies, paquete ya instalado. | Consultar `LICENSE.md` del paquete local. |
| Liberation Sans y su recurso SDF | Fuente incluida en los recursos de TextMesh Pro; créditos Google Corporation y Red Hat, Inc. según el archivo adjunto. | [SIL Open Font License 1.1](<C:/Games/grupo03/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>). |
| Asistencia para código, geometría y documentación | Codex, OpenAI. | Herramienta de IA utilizada durante la implementación. |

Las escenas de la instalación se conservan como referencia. La escena nueva y sus recursos pertenecen a la etapa 1; la incorporación definitiva de créditos a la experiencia completa forma parte del cierre previsto para la etapa 3.
