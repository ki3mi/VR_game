# Validación de etapa 2 con XR Interaction Simulator

- Fecha UTC: 2026-11-08T23:10:45.1442034Z
- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.
- Escena: `Assets/Scenes/DataCenter_VR.unity`.
- Unity: `6000.6.0f1`.
- Duración del recurso: 90 s. Duración de prueba en copia temporal: 24 s.
- Estado: `passed`.

Las acciones se ejecutaron con eventos del Input System en Game View. El visor y las acciones se seleccionaron mediante la mirada simulada y las teclas indicadas por el escenario.

| Caso | Resultado | Observación |
| --- | --- | --- |
| E2-P01 | Cumple | Preparación se mantuvo hasta el inicio. Primer minuto observado: 04/02/2016 20:36. |
| E2-P02 | Cumple | Primera observación de 36 órdenes: 04/02/2016 22:06. Máximo observado: 36. |
| E2-P03 | Cumple | Cruce al 05/02 observado y reloj detenido en 05/02/2016 03:59. |
| E2-P04 | Cumple | El log muestra horas y huecos de registros. Dos pulsaciones dejaron una evidencia log_huecos. |
| E2-P05 | Cumple | Tres evidencias únicas y confirmación en el HUD después de repetir cada registro y reabrir el log tras el cierre: log_huecos, switch_barato, impresora_apagada. |
| E2-P06 | Cumple | Resumen visible con horas, 36 órdenes intentadas y US$951 millones. FICHA DE INSPECCIÓN RESULTADO DEL ATAQUE 04/02/2016 20:36  →  05/02/2016 03:59 <b>36 órdenes intentadas</b>  ·  <b>US$951 millones</b> Monto total de las órdenes intentadas en la simulación.  <b>Evidencias registradas 3/3</b> Registrada  ·  Huecos en el registro SWIFT Registrada  ·  Switch de la red local Registrada  ·  Impresora de confirmaciones apagada 1  Reiniciar ataque ESC / ESPACIO  ·  CERRAR Y CONTINUAR |
| E2-P07 | Cumple | Reinicio por tecla 1 con cero órdenes y evidencias. Segundo ataque iniciado por clic: 36 órdenes y 0 evidencias. |
| E2-P08 | Cumple | Regresión con portal, 6 estaciones, 5 objetos y 0.253 m con W. Errores de ejecución: 0. |

## Capturas

- [01_preparacion_terminal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/01_preparacion_terminal.png)
- [02_reconstruccion_en_curso.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/02_reconstruccion_en_curso.png)
- [04_log_con_huecos.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/04_log_con_huecos.png)
- [03_36_ordenes.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/03_36_ordenes.png)
- [05_cambio_de_fecha.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/05_cambio_de_fecha.png)
- [06_evidencias_registradas.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/06_evidencias_registradas.png)
- [06b_confirmacion_en_log.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/06b_confirmacion_en_log.png)
- [07_resumen_ataque.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/07_resumen_ataque.png)
- [08_reinicio.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/08_reinicio.png)
- [09_segundo_resultado.png](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/09_segundo_resultado.png)

La primera ejecución produjo 118 fotogramas para el video de validación.


## Revisión visual y consola

Las capturas de Game View muestran 36 órdenes a las 22:06, el log final con dos huecos y el resumen con US$951 millones en órdenes intentadas. El contador 3/3 y el aviso de evidencia se leen al mismo tiempo que el log.

La consola de Unity registró cero errores y cero advertencias al volver a Edit Mode. DataCenter_VR quedó guardada, con Game View enfocada y el recurso de 90 segundos restaurado.

