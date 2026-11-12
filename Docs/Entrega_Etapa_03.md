# ANDES-SIM · Entrega del centro de datos

## Ficha del proyecto

La ficha está organizada en cuatro bloques para una exportación de hasta cuatro páginas. Los guiones y las instrucciones de entrega quedan como anexos.

### Página 1 · Escenario y objetivo

**Centro de datos · Escenario 3 · BANCO ANDINO.** La Unidad de Respuesta reconstruye un acceso nocturno a la terminal SWIFT en una simulación educativa con entidades y registros ficticios. El jugador recorre seis estaciones en una sala con cuatro racks e inspecciona un rack, la terminal, el switch, la impresora y el panel de controles. Puede repetir el mismo intento con las defensas activadas.

El ataque empieza a las **20:36 del 4 de febrero de 2016**, alcanza **36 órdenes por US$951 millones intentados** a las **22:06** y termina a las **03:59 del 5 de febrero**. El procesamiento posterior de las 36 órdenes pertenece al escenario 4. Con las defensas activadas, el intento de las 20:36 queda bloqueado y se envían **0 órdenes**. [Contexto del escenario](C:/Games/grupo03/Docs/Contexto_Proyecto_Escenario_3_Centro_de_Datos.md)

### Página 2 · Vulnerabilidades y controles

| Vulnerabilidad simulada | Control | Aporte en la reconstrucción |
| --- | --- | --- |
| Credenciales comprometidas sin segundo factor | MFA y protección de accesos privilegiados | Añade una verificación independiente antes de autorizar la sesión. |
| Acceso fuera del horario autorizado | Detección y bloqueo por horario | Rechaza el intento nocturno y activa la alarma sonora y la luz roja. |
| Registros locales borrables | SIEM con integridad y acceso restringido | Centraliza y conserva el evento protegido de bloqueo para su revisión. |

El visor de ataque muestra huecos en el registro y permite marcar el log, el switch y la impresora apagada como evidencias. En defensa, la autenticación y la política de horario impiden el acceso. El SIEM conserva la evidencia del intento y permite investigarlo.

### Página 3 · Comparación y capturas

| Resultado | Ataque | Defensa |
| --- | --- | --- |
| Acceso a las 20:36 | Inadvertido | Detectado y bloqueado |
| Órdenes enviadas en la simulación | **36** | **0** |
| Monto de órdenes intentadas | US$951 millones | US$0 en órdenes enviadas |
| Evidencia en el visor | Huecos en el log local | Evento centralizado y protegido |

- Bloqueo de las 20:36 con la baliza encendida, los tres controles activos y 0 órdenes. [Captura de la alarma](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261109_050428/04_alarma_roja_y_bloqueo.png)
- Las dos columnas muestran las 36 órdenes del ataque anterior y las 0 de la defensa actual. [Captura de la comparación](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261109_050428/06_comparacion_defensa_actual.png)

### Página 4 · Roles y créditos

| Rol | Responsabilidad prevista |
| --- | --- |
| Liderazgo y narrativa | Coordinar cronología, continuidad con el escenario 4, guiones y entrega. |
| Desarrollo Unity | Integrar navegación, reloj, visor, controles, alarma, comparación y APK. |
| Arte, interfaz y sonido | Preparar el entorno, señalización, indicadores, legibilidad, audio y capturas. |
| Ciberseguridad y QA | Revisar los controles, documentar la evidencia y verificar los flujos en simulador y dispositivos. |

Completar el NRC, los nombres y la distribución real de responsabilidades antes de exportar la ficha. El contexto contempla que liderazgo asuma también ciberseguridad y QA en un equipo de tres personas.

- Geometría, materiales y señalización originales del proyecto. Ambiente y señales sonoras sintetizados en el proyecto.
- XR Interaction Toolkit, XR Interaction Simulator y XR Core Utilities de Unity Technologies bajo Unity Companion License para proyectos Unity. [Créditos y procedencia de los recursos](C:/Games/grupo03/Docs/Guia_Ejecucion_Etapa_01.md)
- Liberation Sans y recurso SDF de TextMesh Pro. Créditos de la fuente a Google Corporation y Red Hat, Inc. [SIL Open Font License 1.1](<C:/Games/grupo03/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>)
- Asistencia de Codex de OpenAI en código, geometría y documentación.

## Ejecutar en Unity con el simulador

1. Abre el proyecto en Unity `6000.6.0f1`, carga la escena, entra en Play y da foco a Game View. [Escena DataCenter_VR](C:/Games/grupo03/Assets/Scenes/DataCenter_VR.unity)
2. Usa **WASD** y **E/Q** para moverte en el simulador. Mantén el botón derecho y mueve el ratón para mirar. Centra la retícula y pulsa **Espacio** para elegir el portal, un destino de teletransporte o un objeto. **Esc** cierra una ficha.
3. Abre la terminal SWIFT. En preparación, **2** cambia entre Ataque y Defensa. En Ataque, **1** inicia el reloj. Inspecciona las pistas durante el recorrido y revisa las 36 órdenes al cierre.
4. En Defensa, abre el panel de controles y usa **1** para MFA, **2** para el bloqueo por horario y **3** para SIEM. Vuelve a la terminal y usa **1** para simular el intento. Con los tres activos, observa la alarma, el bloqueo y las 0 órdenes. Reabre el visor para revisar el evento protegido.
5. Pulsa **F** tras un resultado para abrir la comparación y **1** en el resumen para reiniciar. Con la ficha cerrada, pulsa **I** para abrir las instrucciones y **1** para consultar los créditos.

Para repetir las pruebas, abre la escena fuera de Play Mode y ejecuta **Andes-Sim > Etapa 3 > Validar en simulador**. El recorrido automatizado usa una copia de los datos con 24 segundos de ataque. La escena usa los 90 segundos del recurso original.

## Guion del video · 2 min 30 s

| Tiempo | Imagen y narración |
| --- | --- |
| 0:00–0:15 | Identificar el escenario y recorrer del portal a la terminal por teletransporte. |
| 0:15–0:55 | Iniciar Ataque a las 20:36. Mostrar el hueco del registro y una evidencia. Usar un corte rotulado para llegar a las 36 órdenes a las 22:06 y al cierre de las 03:59. Explicar el monto intentado. |
| 0:55–1:30 | Reiniciar, seleccionar Defensa y activar MFA, horario y SIEM. Mostrar la confirmación de los tres interruptores. |
| 1:30–2:10 | Simular el acceso. Dejar oír la alarma, mostrar la luz roja, las 0 órdenes y el evento protegido en el visor. |
| 2:10–2:30 | Mostrar la comparación 36 frente a 0, identificar la ejecución actual y cerrar con los créditos. |

Grabar la versión de entrega desde el celular una vez validado el APK. La grabación del Editor corresponde a evidencia de desarrollo.

## Guion de presentación · 10 min

| Tiempo | Demostración y análisis |
| --- | --- |
| 0:00–1:00 | Presentar objetivo y recorrido por mirada y teletransporte. |
| 1:00–3:00 | Ejecutar Ataque. Inspeccionar el registro y señalar la impresora apagada. Mostrar la cronología y las 36 órdenes. |
| 3:00–5:30 | Reiniciar, activar las tres defensas y ejecutar el intento de las 20:36. Mostrar la alarma y el evento protegido. |
| 5:30–7:00 | Mostrar la comparación, reiniciar y abrir instrucciones y créditos. |
| 7:00–9:00 | Relacionar MFA y horario con la prevención. Explicar qué conserva el SIEM y cómo se conecta el resultado del ataque con el escenario 4. |
| 9:00–10:00 | Presentar la hipótesis de que alguien con conocimiento de los horarios pudo facilitar la elección de una ventana con baja supervisión. Contrastar con turnos, origen de credenciales y registros de acceso. Mantener abierta la autoría. |

## Preparación de Android

1. Usar la instalación activa de Unity `6000.6.0f1`. La comprobación en el Editor confirmó soporte de compilación Android, SDK, NDK y OpenJDK.
2. Incorporar Cardboard XR Plugin y configurar su proveedor para Android. Adaptar el recorrido y las acciones a la retícula `CardboardReticlePointer` y a la selección disponible en el visor. Usar la capa `Interactive` para los objetos de mirada. [Configuración de entrega solicitada](C:/Games/grupo03/Docs/Contexto_Proyecto_Escenario_3_Centro_de_Datos.md)
3. Añadir la escena al perfil de compilación Android y configurar API mínima 26, IL2CPP, ARM64, OpenGLES3 y orientación Landscape Left. El proyecto actual registra API mínima 34 y tiene paquetes OpenXR en el manifiesto. Cardboard queda pendiente. [Configuración del proyecto](C:/Games/grupo03/ProjectSettings/ProjectSettings.asset) · [Manifiesto de paquetes](C:/Games/grupo03/Packages/manifest.json)
4. Generar un APK instalable y probarlo en dos celulares. Registrar por dispositivo arranque, interacción por mirada, teletransporte, legibilidad, sonido, alarma y ambos resultados. Después, grabar el video final desde el celular y publicar el repositorio del equipo con su historial de contribuciones.

## Estado de entrega

| Entregable | Estado |
| --- | --- |
| Validación E3-P01 a E3-P08 | **8 de 8 aprobadas** con XR Interaction Simulator. Se comprobó la secuencia **36 → 0 → 36**, la alarma, el registro SIEM, la navegación y los créditos. [Informe de pruebas](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261109_050428/Resultados_Etapa_03.md) |
| APK instalable | Herramientas Android disponibles en el Editor activo. Pendientes Cardboard, adaptación de entrada y compilación. |
| Validación en dos celulares | Pendiente de un APK y de los dispositivos del equipo. |
| Repositorio y contribuciones | Proyecto guardado localmente. Falta un repositorio Git para registrar los commits y enlazar GitHub. |
| Video final desde el celular | Guion preparado. Grabación pendiente de la validación en dispositivo. |
| Ficha y presentación | Texto, capturas y guiones preparados. Quedan la asignación nominal y la exportación de la ficha. |
