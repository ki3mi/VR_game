# Entrega del entrenamiento narrativo guiado

Fecha local: 11 de noviembre de 2026. Proyecto ANDES-SIM, Banco Andino, escenario 3: Centro de datos.

## Juego entregado

La escena `Assets/Scenes/DataCenter_VR.unity` contiene un recorrido completo acompañado por **Elliot Alderson, ingeniero senior de ciberseguridad**. Un teléfono con chat permanece visible a la derecha desde la bienvenida hasta el cierre. Allí se presentan el objetivo, las instrucciones, las explicaciones y las acciones. La guía por texto es el medio final de esta entrega y no requiere videos, locuciones, servicios externos ni contenido por completar para jugar.

El jugador reconstruye el acceso nocturno, consulta los envíos, conserva tres evidencias, configura las defensas y repite el intento protegido. Sus acciones producen resultados comprobables en el modelo del juego. La comparación utiliza las dos ejecuciones realizadas: **36 órdenes de ataque frente a 0 órdenes de defensa**.

## Cómo jugar

1. Abre `Assets/Scenes/DataCenter_VR.unity` en Unity y pulsa **Play**. Da foco a **Game View**.
2. Espera el mensaje de Elliot y pulsa **Comenzar entrenamiento**. Sigue el objetivo, el marcador verde y las explicaciones del teléfono.
3. En la versión Android, orienta la cámara con joystick o giroscopio. Toca un equipo para inspeccionarlo o céntralo y pulsa Inspeccionar. Usa los destinos de teletransporte o Llévame para desplazarte. Consulta `Docs/Entrega_Android.md` para instalar y configurar esta versión.
4. Usa **Llévame al objetivo**, **Guíame**, **Repetir** y **Usar ejemplo** cuando los necesites. Puedes corregir consultas sin perder el progreso. Revisa el historial con la rueda o la barra lateral y pulsa **Ver últimos** para retomar los mensajes recientes.

La [guía completa del juego](C:/Games/grupo03/Docs/Guia_Juego_Narrativo.md) explica los controles, el recorrido, los comandos y los dos alcances de reinicio.

## Implementación

- `TrainingDirector` coordina veinte estados del recorrido, los objetivos, las explicaciones y sus condiciones reales de avance.
- `MentorPresenter` presenta el teléfono persistente, el historial del chat, los objetivos y las acciones. El indicador animado **Elliot está escribiendo** precede cada respuesta, ahora con un retardo de 1,2 segundos. Su recepción reproduce una notificación sonora sintética propia.
- Las acciones relevantes producen mensajes del jugador y respuestas del ingeniero. El historial conserva las conversaciones de la sesión de Play, incluidas las repeticiones y los reinicios; un aviso separa los nuevos entrenamientos. Cada mensaje nuevo enfoca automáticamente la última burbuja. Si quedan indicaciones pendientes, el botón verde muestra **Siguiente**: envía **Ok** y entrega solo la siguiente antes de restaurar las acciones del objetivo. **Ver últimos** permite regresar manualmente a la indicación reciente.
- La consola editable se presenta a la izquierda mientras el teléfono permanece a la derecha. El comando es cian, el resultado válido verde y el error coral; las etiquetas COMANDO y SALIDA también diferencian su función. Las explicaciones y la interpretación se entregan en el chat.
- El reloj avanza entre hitos y se detiene durante la lectura. La transmisión hacia las 22:06 dura ocho segundos; el recurso original mantiene su configuración de 90 segundos para la simulación continua.
- Las tres consultas internas enseñan a investigar accesos, interpretar órdenes enviadas y verificar un evento central protegido. Elliot explica su sintaxis en el chat; la terminal ofrece un ejemplo editable y devuelve una salida técnica. Una entrada incorrecta permite corregir y volver a ejecutar.
- Las tres evidencias pueden guardarse en cualquier orden. El expediente del entrenamiento se conserva al preparar la defensa y no duplica hallazgos.
- Los controles MFA, horario con bloqueo y SIEM protegido se aplican mediante las acciones del panel y actualizan sus indicadores. La prueba protegida activa la alarma, la luz y la baliza con cero órdenes.
- Al escribir o leer una pantalla modal, se suspenden las acciones del simulador que podrían desplazar la cámara o activar sus atajos. Al continuar se recuperan las acciones previamente activas.
- Repetir la defensa conserva el expediente, las tres consultas únicas y el resultado de ataque. Comenzar otro entrenamiento limpia los objetivos, el reloj, las evidencias, los controles, las consultas, la alarma y el historial comparativo, conservando la conversación dentro de la misma sesión de Play.

El menú **Andes-Sim > Narrativa > Integrar recorrido guiado** conecta los componentes sin reconstruir el entorno. El menú **Andes-Sim > Narrativa > Validar recorrido en simulador** reproduce las comprobaciones funcionales.

## Contenido técnico

La experiencia distingue autenticación, autorización, prevención y observabilidad. El SIEM centraliza la evidencia del bloqueo y se acompaña de integridad y acceso restringido. La política de acceso produce el rechazo; el SIEM conserva su evento.

Las cuentas, direcciones IP, consultas y datos operativos son ficticios. `andes` es una consola pedagógica interna, sin acceso al sistema operativo ni a una red. Las 36 órdenes representan US$951 millones **intentados**, no dinero confirmado como robado. El procesamiento posterior corresponde al escenario 4.

## Validación en XR Interaction Simulator

Se verificó el recorrido en Unity `6000.6.0f1`, Input System `1.20.0` y XR Interaction Toolkit `3.6.0`. La ejecución vigente aprobó **31 de 31 comprobaciones**: doce del recorrido, siete de recuperación, ocho del teléfono y cuatro de la secuencia Siguiente/Ok y del nombre sin prefijo. No se registraron errores durante la ejecución. El recorrido automatizado duró 109,2 segundos; ese tiempo no mide la lectura de una persona. La configuración original del recurso de escenario se conservó.

| Grupo | Comprobación |
| --- | --- |
| Bienvenida y acompañamiento | Inicio, ayuda explícita, repetición de la explicación y objetivos. |
| Investigación técnica | Consulta incorrecta corregible y tres consultas válidas con interpretación. |
| Reconstrucción | 20:36 → 22:06 con 36 órdenes → 03:59 del día siguiente. |
| Evidencias | Tres hallazgos guardados en un orden distinto al del guion. |
| Defensa | Tres controles aplicados, bloqueo a las 20:36, alarma y cero órdenes. |
| Comparación y cierre | Resultados reales 36 frente a 0, debrief y finalización. |
| Repetición | Defensa repetible sin duplicar consultas ni perder el expediente y el resultado previo. |
| Recuperación | Cerrar la consola antes y después de consultar; retomar el objetivo. |
| Entradas | La escritura no activa locomoción; WASD vuelve a funcionar tras el reinicio. |
| Reinicios | Reinicio completo desde consola enfocada y durante la transmisión; la corrutina anterior queda cancelada. |
| Teléfono persistente | Elliot Alderson visible a la derecha durante bienvenida, exploración, terminal, comparación y cierre. |
| Conversación | Mensajes del jugador, indicador de escritura previo y notificación al entregar cada respuesta. |
| Historial | Desplazamiento real con rueda, enfoque automático de cada mensaje nuevo y recuperación con Ver últimos. |
| Secuencia de indicaciones | Una sola entrega antes de Siguiente; respuesta Ok, entrega del siguiente mensaje, restauración de las acciones y cancelación correcta al reiniciar. |
| Presentación | Terminal a la izquierda, colores diferenciados y explicaciones dentro del teléfono. |
| Texto completo | 153 cuerpos de burbuja medidos contra Text.preferredHeight, con al menos 6 px de margen y fondos/filas suficientes. |

La prueba utiliza entradas del Input System para el simulador XR, selección por mirada y clics en botones reales. El campo UGUI recibe texto mediante eventos de teclado de Game View cuando requiere `Event.PopEvent`. El validador no asigna el texto del campo directamente ni llama a los callbacks de progreso del juego.

Las dieciséis capturas y los resultados detallados se encuentran en la [ejecución del chat secuencial](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_010136/Resultados_Narrativa.md). La ejecución registró 73 mensajes entregados por el mentor, 71 mensajes del jugador y 73 notificaciones sonoras. Estas cifras incluyen repeticiones y ejercicios de recuperación del validador, y no representan la duración o cantidad de mensajes de una partida típica.

La revisión visual por IA comprobó las capturas de Siguiente, la respuesta Ok y el enfoque de la última burbuja tras revisar mensajes antiguos. La geometría de todos los cuerpos del historial se verificó en la misma ejecución. No constituye un estudio con participantes. Las validaciones anteriores se conservan como evidencia histórica de las interfaces previas.

La validación corresponde al juego ejecutado en el Editor con XR Interaction Simulator. No acredita una compilación Android ni pruebas en un visor o teléfonos físicos.

## Estado final del Editor

Comprobado mediante Unity MCP tras la última ejecución: escena DataCenter_VR activa, guardada y sin cambios pendientes; Edit Mode, sin compilación en curso; director, guía y explorador conectados. La consola final devolvió **cero errores y cero advertencias**. El recurso original conserva 90 segundos.
