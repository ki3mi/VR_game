# Validación de etapa 1 con XR Interaction Simulator

- Fecha UTC: 2026-11-05T04:59:00.3990680Z
- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.
- Escena: `Assets/Scenes/DataCenter_VR.unity`.
- Unity: `6000.6.0f1`.
- Entrada: InputSystem.QueueStateEvent para teclado y ratón; movimiento libre WASD/QE y tracking del XR Interaction Simulator; selección por mirada con Space.
- Estado: `passed`.
- Teletransportes: 16; destinos visitados: 6; objetos inspeccionados: 5.

## Resultados observados

| Caso | Resultado | Observación |
| --- | --- | --- |
| E1-P01 | Cumple funcionalmente | XRSimulatedHMD presente; giro rastreado por entrada RightArrow: 133.20°; giro por clic derecho y ratón: 2.40°; altura de cámara: 1.65 m. |
| E1-P02 | Cumple funcionalmente | Portal usado; 6/6 destinos alcanzados por selección con Space; regreso a TP_Entrada. |
| E1-P03 | Cumple funcionalmente | Un único simulador; velocidades XYZ: (0.20, 0.20, 0.20); multiplicador corporal: 5.0; desplazamientos reales: W=0.251 m, S=0.252 m, A=0.250 m, D=0.250 m, E=0.250 m, Q=0.250 m; teletransportes acumulados: 9. |
| E1-P04 | Cumple funcionalmente | Rack_01: presente, Rack_02: presente, Rack_03: presente, Rack_04: presente, Terminal_SWIFT: presente, Switch_Barato: presente, Impresora: presente; audio ambiental reproduciéndose: True; racks animados: 4; mínimo LEDs/rack: 10. |
| E1-P05 | Cumple funcionalmente | Objetos: Rack_01, Terminal_SWIFT, Switch_Barato, Impresora, Panel_Controles; inspecciones únicas: 5. |
| E1-P06 | Cumple funcionalmente | Las cinco fichas se abrieron y cerraron con Escape; textos TMP desbordados: 0. Capturas guardadas para revisión visual de legibilidad. |
| E1-P07 | Cumple funcionalmente | Recorrido terminado: True; errores capturados en Play Mode: 0. Consola del Editor se revisa además por MCP después de salir. |

## Evidencias visuales

Las capturas y los frames proceden de Game View durante la ejecución real. La legibilidad de las fichas requiere revisar las imágenes. La comodidad en visor y Android no se certifican con este simulador.

- [01_entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/01_entrada.png)
- [movimiento_libre.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/movimiento_libre.png)
- [02_portal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/02_portal.png)
- [TP_Entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/TP_Entrada.png)
- [TP_Racks.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/TP_Racks.png)
- [TP_Terminal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/TP_Terminal.png)
- [TP_Switch.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/TP_Switch.png)
- [TP_Impresora.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/TP_Impresora.png)
- [TP_Controles.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/TP_Controles.png)
- [inspeccion_Rack_01.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/inspeccion_Rack_01.png)
- [inspeccion_Terminal_SWIFT.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/inspeccion_Terminal_SWIFT.png)
- [inspeccion_Switch_Barato.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/inspeccion_Switch_Barato.png)
- [inspeccion_Impresora.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/inspeccion_Impresora.png)
- [inspeccion_Panel_Controles.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/inspeccion_Panel_Controles.png)
- [04_interior.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/04_interior.png)
- [03_regreso.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_045854/03_regreso.png)

Se guardaron 149 frames con sus tiempos en `resultados.json`, para elaborar un video del recorrido.


## Movimiento libre medido

Los desplazamientos siguientes proceden de pulsaciones reales encoladas en Input System y consumidas por el único XR Interaction Simulator de la escena. Cada pulsación duró aproximadamente 0.25 segundos.

| Tecla | Posición inicial (m) | Posición final (m) | Distancia (m) |
| --- | --- | --- | ---: |
| W | 0.000, 1.650, -8.000 | 0.176, 1.650, -8.179 | 0.251 |
| S | 0.176, 1.650, -8.179 | -0.001, 1.650, -7.999 | 0.252 |
| A | -0.001, 1.650, -7.999 | 0.178, 1.650, -7.824 | 0.250 |
| D | 0.178, 1.650, -7.824 | -0.001, 1.650, -7.999 | 0.250 |
| E | -0.001, 1.650, -7.999 | -0.001, 1.900, -7.999 | 0.250 |
| Q | -0.001, 1.900, -7.999 | -0.001, 1.651, -7.999 | 0.250 |

## Revisión visual y consola final

El HUD presenta las cuatro líneas de controles WASD, E/Q, mirada y selección. El pie indica movimiento libre y teletransporte. La vista interior y la ficha de controles fueron revisadas: los textos están completos y legibles, con fondo opaco en las fichas. Las comprobaciones automáticas encontraron cero textos de inspección desbordados.

La consola del Editor consultada por Unity MCP después de salir de Play Mode contiene 0 errores y 0 advertencias. La mejora posterior del harness para medir Camera.worldPosition XZ recompiló sin errores ni advertencias del código del proyecto.

La escena DataCenter_VR queda guardada y activa en Edit Mode. Esta validación corresponde al simulador de desarrollo; Android y la comodidad con visor se verifican por separado.
