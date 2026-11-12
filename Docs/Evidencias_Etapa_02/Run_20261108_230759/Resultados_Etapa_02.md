# Validación de etapa 2 con XR Interaction Simulator

- Fecha UTC: 2026-11-08T23:08:07.4018482Z
- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.
- Escena: `Assets/Scenes/DataCenter_VR.unity`.
- Unity: `6000.6.0f1`.
- Duración del recurso: 90 s. Duración de prueba en copia temporal: 24 s.
- Estado: `functional_checks_passed_visual_review_pending`.

Las acciones se ejecutaron con eventos del Input System en Game View. El visor y las acciones se seleccionaron mediante la mirada simulada y las teclas indicadas por el escenario.

| Caso | Resultado | Observación |
| --- | --- | --- |
| E2-P01 | Cumple | Preparación se mantuvo hasta el inicio. Primer minuto observado: 04/02/2016 20:36. |
| E2-P02 | Cumple | Primera observación de 36 órdenes: 04/02/2016 22:06. Máximo observado: 36. |
| E2-P03 | Cumple | Cruce al 05/02 observado y reloj detenido en 05/02/2016 03:59. |
| E2-P04 | Cumple | El log muestra horas y huecos de registros. Dos pulsaciones dejaron una evidencia log_huecos. |
| E2-P05 | Cumple | Tres evidencias únicas después de repetir cada registro y reabrir el log tras el cierre: log_huecos, switch_barato, impresora_apagada. |
| E2-P06 | Cumple | Resumen visible con horas, 36 órdenes intentadas y US$951 millones. FICHA DE INSPECCIÓN RESULTADO DEL ATAQUE 04/02/2016 20:36  →  05/02/2016 03:59 <b>36 órdenes intentadas</b>  ·  <b>US$951 millones</b> Monto total de las órdenes intentadas en la simulación.  <b>Evidencias registradas 3/3</b> Registrada  ·  Huecos en el registro SWIFT Registrada  ·  Switch de la red local Registrada  ·  Impresora de confirmaciones apagada 1  Reiniciar ataque ESC / ESPACIO  ·  CERRAR Y CONTINUAR |
| E2-P07 | Cumple | Reinicio con cero órdenes y evidencias. Segunda ejecución: 36 órdenes y 0 evidencias. |
| E2-P08 | Cumple | Regresión con portal, 6 estaciones, 5 objetos y 0.250 m con W. Errores de ejecución: 0. |

## Capturas

- [01_preparacion_terminal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/01_preparacion_terminal.png)
- [02_reconstruccion_en_curso.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/02_reconstruccion_en_curso.png)
- [04_log_con_huecos.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/04_log_con_huecos.png)
- [03_36_ordenes.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/03_36_ordenes.png)
- [05_cambio_de_fecha.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/05_cambio_de_fecha.png)
- [06_evidencias_registradas.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/06_evidencias_registradas.png)
- [07_resumen_ataque.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/07_resumen_ataque.png)
- [08_reinicio.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/08_reinicio.png)
- [09_segundo_resultado.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_230759/09_segundo_resultado.png)

La primera ejecución produjo 116 fotogramas para el video de validación.

