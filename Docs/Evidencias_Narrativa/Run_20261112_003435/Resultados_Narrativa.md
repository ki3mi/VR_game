# Recorrido narrativo con XR Interaction Simulator

Ejecución UTC: 2026-11-12T00:34:41.6235207Z. Escena: `Assets/Scenes/DataCenter_VR.unity`. Unity: 6000.6.0f1.
Estado: `functional_and_visual_checks_passed`. Entradas: teclado y ratón del Input System para simulador XR y botones visibles; texto mediante eventos de teclado de Game View cuando el InputField UGUI requiere Event.PopEvent. No se asigna el texto del campo ni se invocan callbacks de progreso desde la prueba.
Duración configurada del recurso: 90 s. El reloj narrativo avanza entre hitos y se detiene para la lectura. Recurso sin modificación: True.
Tiempo de validación: 101.8 s.

| Caso | Resultado | Observación |
| --- | --- | --- |
| NAR-01 | Cumple | Escena abre con la bienvenida del senior y simulador XR activo. |
| CHAT-02 | Cumple | Escribiendo aparece antes de entregar el mensaje; la recepción incrementa el contador de notificación con clip y volumen activos. |
| NAR-02 | Cumple | Guíame muestra la explicación de la acción; Repetir restaura el mensaje y mantiene el objetivo. |
| CHAT-03 | Cumple | Las acciones del jugador producen mensajes propios y respuestas del mentor en el historial. |
| NAR-03 | Cumple | Una consulta incompleta muestra corrección y permite reintentar el objetivo. |
| CHAT-06 | Cumple | Terminal a la izquierda con teléfono visible a la derecha: comando cian, resultado verde y error coral. |
| CHAT-07 | Cumple | Las instrucciones y explicaciones están en el chat; las tarjetas antiguas de objetivo o explicación no aparecen fuera del teléfono. |
| NAR-04 | Cumple | Consulta e interpretación de cuenta, origen y hora de acceso antes del envío. |
| NAR-05 | Cumple | Hitos exactos 20:36 → 22:06/36 órdenes → 05/02 03:59, con pausas para leer. |
| NAR-06 | Cumple | Tres evidencias registradas en un orden distinto al guion: impresora, switch y registro. |
| NAR-07 | Cumple | El jugador aplica MFA, política de horario y SIEM desde la interfaz del panel. |
| NAR-08 | Cumple | Bloqueo real antes de envíos: cero órdenes, alarma activa y evento protegido. |
| NAR-09 | Cumple | Tres consultas completadas: acceso, pagos y evento centralizado protegido. |
| NAR-10 | Cumple | Comparación final basada en ataque de 36 órdenes y defensa de cero órdenes realizados en esta sesión. |
| NAR-11 | Cumple | Recorrido terminado después de explicaciones, investigación y práctica técnica, con cierre accesible. |
| CHAT-01 | Cumple | Ing. Elliot Alderson permanece visible en su teléfono a la derecha durante bienvenida, exploración, consola, comparación y cierre. |
| CHAT-04 | Cumple | La rueda permite leer mensajes anteriores; el historial crece con una respuesta nueva sin forzar el desplazamiento al final. |
| CHAT-05 | Cumple | Ver últimos reactiva el seguimiento y muestra la última burbuja; los mensajes largos comienzan por su primera línea. |
| REC-01 | Cumple | Repetir la prueba protegida conserva expediente 3/3, consultas únicas 3/3 y comparación 36 frente a 0. |
| NAR-12 | Cumple | Reinicio completo devuelve a bienvenida sin evidencias, controles, alarma, consultas ni resultados comparativos previos. La conversación se conserva durante la sesión. |
| REC-02 | Cumple | La explicación completa del senior aparece en el chat del teléfono mientras la consola permanece abierta. |
| REC-03 | Cumple | El campo recibe foco y teclear W no mueve la cámara ni activa locomoción del simulador. |
| REC-04 | Cumple | Cerrar antes de ejecutar conserva la tarea y ofrece Reabrir consulta; cerrar tras ejecutar conserva su interpretación y permite seguir. |
| REC-05 | Cumple | Reiniciar durante la transmisión cancela su corrutina; tras nueve segundos sigue la bienvenida limpia. |
| REC-06 | Cumple | Llévame lleva a la estación; el jugador selecciona la terminal con la mirada para abrir la práctica. |
| REC-07 | Cumple | Reiniciar con la consola enfocada libera el campo y restaura movimiento WASD real al volver a explorar. |
| CHAT-08 | Cumple | Todos los cuerpos de burbujas del mentor, jugador y sistema tienen altura suficiente para Text.preferredHeight y caben dentro de sus fondos y filas. |

## Capturas

- [01_bienvenida.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/01_bienvenida.png)
- [13_elliot_escribiendo.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/13_elliot_escribiendo.png)
- [02_consulta_accesos.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/02_consulta_accesos.png)
- [03_ordenes_enviadas.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/03_ordenes_enviadas.png)
- [04_evidencias_completas.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/04_evidencias_completas.png)
- [05_defensas_preparadas.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/05_defensas_preparadas.png)
- [06_intento_bloqueado.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/06_intento_bloqueado.png)
- [07_comparacion_36_0.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/07_comparacion_36_0.png)
- [08_conclusiones.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/08_conclusiones.png)
- [09_entrenamiento_completado.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/09_entrenamiento_completado.png)
- [14_historial_y_mensaje_nuevo.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/14_historial_y_mensaje_nuevo.png)
- [10_defensa_repetida.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/10_defensa_repetida.png)
- [11_guia_dentro_de_consola.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/11_guia_dentro_de_consola.png)
- [12_movimiento_restaurado.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_003435/12_movimiento_restaurado.png)

## Revisión visual y geometría de texto

Revisión por agentes de IA aprobada en las catorce capturas: teléfono y terminal separados, colores e indicador de escritura legibles. Las frases finales aparecen completas en accesos, pagos y comparación. El historial permite leer mensajes anteriores y recuperar los últimos. Se midieron **142 cuerpos de burbuja**, con **6 px de margen mínimo** respecto a Text.preferredHeight y fondos/filas suficientes. El desplazamiento por el viewport permite leer los mensajes largos. No es un estudio con participantes.

Al terminar se confirmó la escena guardada en Edit Mode y la consola sin errores ni advertencias. El recurso original conserva 90 segundos.
