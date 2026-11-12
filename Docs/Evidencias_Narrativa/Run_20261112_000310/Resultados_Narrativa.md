# Recorrido narrativo con XR Interaction Simulator

Ejecución UTC: 2026-11-12T00:03:16.1568355Z. Escena: `Assets/Scenes/DataCenter_VR.unity`. Unity: 6000.6.0f1.
Estado: `functional_and_visual_checks_passed`. Entradas: teclado y ratón del Input System para simulador XR y botones visibles; texto mediante eventos de teclado de Game View cuando el InputField UGUI requiere Event.PopEvent. No se asigna el texto del campo ni se invocan callbacks de progreso desde la prueba.
Duración configurada del recurso: 90 s. El reloj narrativo avanza entre hitos y se detiene para la lectura. Recurso sin modificación: True.
Tiempo de validación: 85.0 s.

| Caso | Resultado | Observación |
| --- | --- | --- |
| NAR-01 | Cumple | Escena abre con la bienvenida del senior y simulador XR activo. |
| NAR-02 | Cumple | Guíame muestra la explicación de la acción; Repetir restaura el mensaje y mantiene el objetivo. |
| NAR-03 | Cumple | Una consulta incompleta muestra corrección y permite reintentar el objetivo. |
| NAR-04 | Cumple | Consulta e interpretación de cuenta, origen y hora de acceso antes del envío. |
| NAR-05 | Cumple | Hitos exactos 20:36 → 22:06/36 órdenes → 05/02 03:59, con pausas para leer. |
| NAR-06 | Cumple | Tres evidencias registradas en un orden distinto al guion: impresora, switch y registro. |
| NAR-07 | Cumple | El jugador aplica MFA, política de horario y SIEM desde la interfaz del panel. |
| NAR-08 | Cumple | Bloqueo real antes de envíos: cero órdenes, alarma activa y evento protegido. |
| NAR-09 | Cumple | Tres consultas completadas: acceso, pagos y evento centralizado protegido. |
| NAR-10 | Cumple | Comparación final basada en ataque de 36 órdenes y defensa de cero órdenes realizados en esta sesión. |
| NAR-11 | Cumple | Recorrido terminado después de explicaciones, investigación y práctica técnica, con cierre accesible. |
| REC-01 | Cumple | Repetir la prueba protegida conserva expediente 3/3, consultas únicas 3/3 y comparación 36 frente a 0. |
| NAR-12 | Cumple | Reinicio completo devuelve a bienvenida sin evidencias, controles, alarma, consultas ni historial previos. |
| REC-02 | Cumple | La explicación completa del senior aparece en la propia consola al solicitar ayuda. |
| REC-03 | Cumple | El campo recibe foco y teclear W no mueve la cámara ni activa locomoción del simulador. |
| REC-04 | Cumple | Cerrar antes de ejecutar conserva la tarea y ofrece Reabrir consulta; cerrar tras ejecutar conserva su interpretación y permite seguir. |
| REC-05 | Cumple | Reiniciar durante la transmisión cancela su corrutina; tras nueve segundos sigue la bienvenida limpia. |
| REC-06 | Cumple | Llévame lleva a la estación; el jugador selecciona la terminal con la mirada para abrir la práctica. |
| REC-07 | Cumple | Reiniciar con la consola enfocada libera el campo y restaura movimiento WASD real al volver a explorar. |

## Capturas

- [01_bienvenida.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/01_bienvenida.png)
- [02_consulta_accesos.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/02_consulta_accesos.png)
- [03_ordenes_enviadas.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/03_ordenes_enviadas.png)
- [04_evidencias_completas.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/04_evidencias_completas.png)
- [05_defensas_preparadas.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/05_defensas_preparadas.png)
- [06_intento_bloqueado.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/06_intento_bloqueado.png)
- [07_comparacion_36_0.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/07_comparacion_36_0.png)
- [08_conclusiones.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/08_conclusiones.png)
- [09_entrenamiento_completado.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/09_entrenamiento_completado.png)
- [10_defensa_repetida.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/10_defensa_repetida.png)
- [11_guia_dentro_de_consola.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/11_guia_dentro_de_consola.png)
- [12_movimiento_restaurado.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261112_000310/12_movimiento_restaurado.png)

Revisión visual por agentes de IA aprobada: guía práctica visible arriba, sin CTA duplicado; objetivos y acciones legibles. El texto extenso usa desplazamiento con barra visible. Las doce capturas de entrega se contrastaron con las nueve pantallas base aprobadas. No es un estudio con participantes.
