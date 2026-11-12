# Validación de etapa 3 con XR Interaction Simulator

Codex ejecutó la validación mediante Unity MCP el 2026-11-08T23:27:10.9004702Z.
La escena `Assets/Scenes/DataCenter_VR.unity` se ejecutó en Unity 6000.6.0f1 con entradas de teclado y ratón en el simulador.
El recurso configura 90 s de ataque. La validación usó una copia temporal de 24 s.

Estado `failed`.

| Caso | Resultado | Observación |
| --- | --- | --- |
| E3-P01 | Cumple | MFA, fuera de horario y SIEM activos. Cada pulsación repetida conservó el contador. |
| E3-P02 | Cumple | Los intentos con cero, uno y dos controles indicaron pendientes y mantuvieron el estado en preparación y las órdenes en cero. |
| E3-P03 | Cumple | Intento a las 04/02/2016 20:36. Sonido, luz roja y mensaje de sesión bloqueada activos con cero órdenes. |
| E3-P04 | Cumple | La terminal muestra el evento SIEM protegido del intento bloqueado. Órdenes enviadas en defensa: 0. |
| E3-P05 | Falla | Comparación 36 frente a 0 en ambos modos. La defensa se mostró como referencia antes de ejecutarse y luego como ejecución previa. |
| E3-P06 | Cumple | Ciclo 36 → 0 → 36 en una sola ejecución de Play Mode. Reinicios con reloj, contadores, evidencias, controles y alarma limpios. |
| E3-P07 | Cumple | Portal, seis destinos alcanzados por teletransporte y cinco fichas abiertas por mirada. |
| E3-P08 | Falla | Ambos modos completados. I abrió instrucciones, 1 abrió créditos y Esc cerró la ficha. Errores de ejecución: 0. |

## Capturas

- [01_ataque_con_defensa_de_referencia.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/01_ataque_con_defensa_de_referencia.png)
- [02_control_pendiente.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/02_control_pendiente.png)
- [03_tres_controles_activos.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/03_tres_controles_activos.png)
- [04_alarma_roja_y_bloqueo.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/04_alarma_roja_y_bloqueo.png)
- [05_evento_protegido_SIEM.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/05_evento_protegido_SIEM.png)
- [06_comparacion_defensa_actual.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/06_comparacion_defensa_actual.png)
- [07_reinicio_sin_alarma.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/07_reinicio_sin_alarma.png)
- [08_segundo_ataque_36_ordenes.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/08_segundo_ataque_36_ordenes.png)
- [09_instrucciones.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/09_instrucciones.png)
- [10_creditos.png](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261108_232703/10_creditos.png)
