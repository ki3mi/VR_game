# ANDES-SIM · Entrenamiento guiado en el centro de datos

Escena: `Assets/Scenes/DataCenter_VR.unity`. Mentor: **Elliot Alderson, ingeniero senior de ciberseguridad del Banco Andino**. La experiencia es un laboratorio interactivo con entidades, cuentas, direcciones IP y comandos ficticios.

## Iniciar y navegar

**Versión Android vigente:** inicia en un menú con Comenzar, Configuración y Salir. Usa el joystick para mirar o selecciona giroscopio en Ajustes. Toca los equipos, usa Inspeccionar y navega por teletransporte o Llévame. El volumen se controla desde Ajustes. El movimiento libre por teclado está eliminado. La guía de instalación y las comprobaciones móviles están en `Docs/Entrega_Android.md`.

Abre la escena en Unity y entra en Play. Da foco a Game View. El teléfono de la derecha presenta a Elliot Alderson y el propósito del entrenamiento. Espera a que llegue el mensaje y pulsa **Comenzar entrenamiento** para entrar al recorrido.

| Acción | Entrada |
| --- | --- |
| Desplazarse | Tocar un destino de teletransporte |
| Recentrar el sensor | Botón Recentrar cámara |
| Orientar la mirada | Arrastrar el joystick o mover el teléfono en modo giroscopio |
| Inspeccionar un equipo | Tocarlo o centrarlo y pulsar Inspeccionar |
| Usar una acción de interfaz | Tocar el botón visible |
| Cambiar cámara y volumen | Botón Ajustes |
| Llegar al equipo del objetivo | Botón **Llévame al objetivo** |
| Leer un bloque largo | Deslizar el historial o usar la barra lateral |
| Ejecutar la consulta escrita | Botón **Ejecutar** |
| Volver a la sala desde la consola | Botón **Volver a la sala** |

El teléfono permanece visible a la derecha durante todo el recorrido, incluida la terminal y el cierre. Contiene el objetivo actual, el estado de la simulación, las instrucciones, las explicaciones y las acciones disponibles. El marcador verde señala el equipo pertinente. **Guíame** solicita instrucciones completas; **Repetir** pide otro mensaje con la explicación del objetivo. La cámara se detiene al usar la consola y el entrenamiento se pausa al abrir el menú o los ajustes.

Las acciones relevantes generan mensajes del jugador al ingeniero. Antes de su respuesta aparece el indicador animado **escribiendo** y, al recibirla, se reproduce una notificación sonora. Cada mensaje nuevo desplaza automáticamente el chat hasta la última burbuja, aunque antes estuvieras leyendo el historial. Los mensajes largos se enfocan desde su primera línea para permitir leerlos completos con la barra lateral.

Si hay varias indicaciones antes de una acción, se presenta **una por vez**: el botón verde muestra **Siguiente**, envía una respuesta **Ok** del jugador y entrega la siguiente indicación. La acción original del objetivo vuelve a estar disponible al terminar la secuencia. El historial permanece y se revisa con la rueda o la barra lateral; **Ver últimos** permite regresar al mensaje reciente. Se conserva durante las repeticiones y los nuevos entrenamientos dentro de la misma sesión de Play; no se guarda al cerrar la aplicación.

## Recorrido completo

1. **Reconstruir el acceso.** Llega a la terminal SWIFT y selecciónala. La reconstrucción se detiene a las 20:36 del 4 de febrero de 2016. Ejecuta la consulta de accesos y señala el acceso nocturno. Elliot explica en el chat la cuenta, el origen, la ausencia de un segundo factor y el horario.
2. **Observar los envíos.** La reproducción avanza durante ocho segundos hasta las 22:06; el contador llega a 36. Selecciona nuevamente la terminal, consulta la cola y confirma que las órdenes salieron aunque la impresora estaba apagada. La lectura confirmada permite avanzar al cierre de las 03:59 del 5 de febrero.
3. **Conservar evidencias.** Inspecciona la terminal, la impresora y el switch, en cualquier orden. Elliot explica cada hallazgo en el chat. Pulsa **Guardar evidencia** en cada uno. Volver a consultar un objeto no duplica el hallazgo.
4. **Preparar la defensa.** Con las tres evidencias guardadas, comienza una ejecución nueva en modo Defensa. Ve al panel y selecciónalo. Activa **MFA**, **horario y bloqueo**, y **SIEM protegido** mientras Elliot explica la función de cada control. Comprueba sus indicadores verdes.
5. **Realizar la prueba protegida.** Regresa a la terminal, selecciónala y pulsa **Simular intento protegido**. El acceso de las 20:36 queda bloqueado con **0 órdenes**. Se activa la alarma y la baliza roja. Puedes silenciar el sonido conservando la señal visual y el evento.
6. **Verificar.** Selecciona la terminal y consulta el evento bloqueado. Identifica su hora, el motivo del rechazo y la evidencia central protegida. Confirma la lectura para abrir la comparación.
7. **Cerrar el caso.** Revisa en el teléfono las **36 órdenes de la ejecución previa** frente a las **0 de la ejecución protegida actual**. Elliot resume lo que hiciste y diferencia autenticación, autorización, prevención y observabilidad. Finaliza el entrenamiento para acceder a la repetición y los créditos.

No hay respuestas ocultas ni penalizaciones. Los botones del teléfono acompañan la interpretación que explica Elliot; los objetivos se apoyan en consultas, hallazgos, controles y resultados reales del modelo del juego. Las acciones esperan a que termine de llegar la explicación, evitando avanzar mientras el ingeniero aún escribe.

## Consola de entrenamiento

La consola interpreta únicamente estas tres consultas internas. No abre una shell, no accede a una red ni ejecuta operaciones del sistema. **Usar ejemplo** carga el comando correspondiente para leerlo, editarlo y ejecutarlo. Una entrada incorrecta muestra cómo corregirla sin perder el progreso.

La terminal se abre a la izquierda y el teléfono sigue visible a la derecha. Los comandos aparecen en **cian**, las salidas válidas en **verde** y los errores en **coral**; las etiquetas COMANDO y SALIDA permiten distinguirlos también por su nombre. Las explicaciones de parámetros y la interpretación llegan al chat del teléfono.

El botón de interpretación del teléfono aparece después de ejecutar una consulta válida. Si sales antes, **Reabrir consulta de terminal** permite retomar la tarea desde el teléfono. Si sales después de obtener un resultado, la confirmación de su lectura sigue disponible allí. La ayuda presenta primero los pasos de la acción y se conserva como otro mensaje del historial.

| Consulta | Explicación |
| --- | --- |
| `andes access --from 20:00 --to 21:00` | `access` consulta accesos; `--from` y `--to` delimitan el intervalo. La salida muestra la cuenta, el origen y la sesión de las 20:36. |
| `andes payments --status sent` | `payments` consulta la cola; `--status sent` selecciona las órdenes enviadas. El contador proviene de la ejecución de ataque. |
| `andes events --result blocked` | `events` consulta la copia central; `--result blocked` selecciona el acceso rechazado. La salida depende del evento protegido de la defensa. |

La cuenta `operador.swift`, el origen `10.24.3.17` y el horario autorizado de 08:00 a 18:00 forman parte de la ficción del laboratorio. El comando `andes` es una herramienta pedagógica creada para esta experiencia.

## Interpretación técnica

- **MFA:** añade un factor independiente a la contraseña para verificar el acceso privilegiado.
- **Horario:** aplica una política de autorización que rechaza la sesión fuera del intervalo permitido y genera una alerta. La alerta informa; la regla de bloqueo impide continuar.
- **SIEM:** centraliza eventos. En el laboratorio se acompaña de acceso restringido y protección de integridad. Conserva la evidencia del rechazo; no sustituye a los controles de acceso.
- **Evidencias:** el hueco del log indica que faltan entradas, pero no revela por sí solo su contenido. La impresora apagada no demuestra ausencia de operaciones. El switch aporta contexto de infraestructura; su precio o aspecto no prueban una intrusión.
- **Monto:** las 36 órdenes suman US$951 millones intentados. No se presentan como dinero efectivamente robado. El procesamiento posterior pertenece al escenario 4.

## Repetición y reinicio

**Reiniciar**, en el teléfono, solicita confirmación y comienza un entrenamiento nuevo. Limpia objetivos, consultas, expediente, controles, reloj, alarma y ambos resultados. El historial entregado permanece con un separador de entrenamiento; los mensajes aún pendientes de la tarea anterior se cancelan. Si cancelas el reinicio, puedes seguir la tarea pendiente.

**Repetir prueba protegida**, disponible después del cierre, conserva la investigación, las tres consultas únicas y el resultado de ataque para la comparación. Limpia la ejecución de defensa y te lleva otra vez al panel para aplicar y probar los controles. No duplica el contador de consultas.

## Integración

`TrainingDirector` coordina el recorrido; `MentorPresenter` presenta la guía y la consola. Se reutilizan el `GameManager`, las evidencias, los controles, la alarma y el explorador de la escena existente. La interfaz antigua se mantiene para escenas sin `TrainingDirector`.

El menú **Andes-Sim > Narrativa > Integrar recorrido guiado** conecta estos componentes sin reconstruir la sala. El menú de validación narrativa realiza un recorrido automatizado con entradas del Input System y XR Interaction Simulator.

La entrega narrativa utiliza texto como medio final: no requiere videos, voz, servicios externos ni recursos audiovisuales por completar. La compilación Android y las pruebas en dispositivos corresponden a una entrega distinta de la validación de este recorrido en el Editor.
