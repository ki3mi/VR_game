# Validación de etapa 1 con XR Interaction Simulator

- Fecha UTC: 2026-11-05T04:47:25.0261389Z
- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.
- Escena: `Assets/Scenes/DataCenter_VR.unity`.
- Unity: `6000.6.0f1`.
- Entrada: InputSystem.QueueStateEvent para teclado y ratón; tracking del XR Interaction Simulator; selección por mirada con Space.
- Estado: `functional_checks_passed_visual_review_pending`.
- Teletransportes: 16; destinos visitados: 6; objetos inspeccionados: 5.

## Resultados observados

| Caso | Resultado | Observación |
| --- | --- | --- |
| E1-P01 | Cumple funcionalmente | XRSimulatedHMD presente; giro rastreado por entrada RightArrow: 90.00°; altura de cámara: 1.65 m. |
| E1-P02 | Cumple funcionalmente | Portal usado; 6/6 destinos alcanzados por selección con Space; regreso a TP_Entrada. |
| E1-P03 | Cumple funcionalmente | Prueba W sin desplazamiento continuo: True; teletransportes acumulados: 9. |
| E1-P04 | Cumple funcionalmente | Rack_01: presente, Rack_02: presente, Rack_03: presente, Rack_04: presente, Terminal_SWIFT: presente, Switch_Barato: presente, Impresora: presente; audio ambiental reproduciéndose: True; racks animados: 4; mínimo LEDs/rack: 10. |
| E1-P05 | Cumple funcionalmente | Objetos: Rack_01, Terminal_SWIFT, Switch_Barato, Impresora, Panel_Controles; inspecciones únicas: 5. |
| E1-P06 | Cumple funcionalmente | Las cinco fichas se abrieron y cerraron con Escape; textos TMP desbordados: 0. Capturas guardadas para revisión visual de legibilidad. |
| E1-P07 | Cumple funcionalmente | Recorrido terminado: True; errores capturados en Play Mode: 0. Consola del Editor se revisa además por MCP después de salir. |

## Evidencias visuales

Las capturas y los frames proceden de Game View durante la ejecución real. La legibilidad de las fichas requiere revisar las imágenes. La comodidad en visor y Android no se certifican con este simulador.

- [01_entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/01_entrada.png)
- [02_portal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/02_portal.png)
- [TP_Entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/TP_Entrada.png)
- [TP_Racks.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/TP_Racks.png)
- [TP_Terminal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/TP_Terminal.png)
- [TP_Switch.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/TP_Switch.png)
- [TP_Impresora.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/TP_Impresora.png)
- [TP_Controles.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/TP_Controles.png)
- [inspeccion_Rack_01.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/inspeccion_Rack_01.png)
- [inspeccion_Terminal_SWIFT.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/inspeccion_Terminal_SWIFT.png)
- [inspeccion_Switch_Barato.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/inspeccion_Switch_Barato.png)
- [inspeccion_Impresora.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/inspeccion_Impresora.png)
- [inspeccion_Panel_Controles.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/inspeccion_Panel_Controles.png)
- [04_interior.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/04_interior.png)
- [03_regreso.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044717/03_regreso.png)

Se guardaron 139 frames con sus tiempos en `resultados.json`, para elaborar un video del recorrido.

