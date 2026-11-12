# Validación de etapa 1 con XR Interaction Simulator

- Fecha UTC: 2026-11-05T04:49:22.2509211Z
- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.
- Escena: `Assets/Scenes/DataCenter_VR.unity`.
- Unity: `6000.6.0f1`.
- Entrada: InputSystem.QueueStateEvent para teclado y ratón; tracking del XR Interaction Simulator; selección por mirada con Space.
- Estado: `passed`.
- Teletransportes: 16; destinos visitados: 6; objetos inspeccionados: 5.

## Resultados observados

| Caso | Resultado | Observación |
| --- | --- | --- |
| E1-P01 | Cumple funcionalmente | XRSimulatedHMD presente; giro rastreado por entrada RightArrow: 111.60°; altura de cámara: 1.65 m. |
| E1-P02 | Cumple funcionalmente | Portal usado; 6/6 destinos alcanzados por selección con Space; regreso a TP_Entrada. |
| E1-P03 | Cumple funcionalmente | Prueba W sin desplazamiento continuo: True; teletransportes acumulados: 9. |
| E1-P04 | Cumple funcionalmente | Rack_01: presente, Rack_02: presente, Rack_03: presente, Rack_04: presente, Terminal_SWIFT: presente, Switch_Barato: presente, Impresora: presente; audio ambiental reproduciéndose: True; racks animados: 4; mínimo LEDs/rack: 10. |
| E1-P05 | Cumple funcionalmente | Objetos: Rack_01, Terminal_SWIFT, Switch_Barato, Impresora, Panel_Controles; inspecciones únicas: 5. |
| E1-P06 | Cumple funcionalmente | Las cinco fichas se abrieron y cerraron con Escape; textos TMP desbordados: 0. Capturas guardadas para revisión visual de legibilidad. |
| E1-P07 | Cumple funcionalmente | Recorrido terminado: True; errores capturados en Play Mode: 0. Consola del Editor se revisa además por MCP después de salir. |

## Evidencias visuales

Las capturas y los frames proceden de Game View durante la ejecución real. La legibilidad de las fichas requiere revisar las imágenes. La comodidad en visor y Android no se certifican con este simulador.

- [01_entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/01_entrada.png)
- [02_portal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/02_portal.png)
- [TP_Entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/TP_Entrada.png)
- [TP_Racks.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/TP_Racks.png)
- [TP_Terminal.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/TP_Terminal.png)
- [TP_Switch.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/TP_Switch.png)
- [TP_Impresora.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/TP_Impresora.png)
- [TP_Controles.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/TP_Controles.png)
- [inspeccion_Rack_01.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Rack_01.png)
- [inspeccion_Terminal_SWIFT.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Terminal_SWIFT.png)
- [inspeccion_Switch_Barato.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Switch_Barato.png)
- [inspeccion_Impresora.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Impresora.png)
- [inspeccion_Panel_Controles.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/inspeccion_Panel_Controles.png)
- [04_interior.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/04_interior.png)
- [03_regreso.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044916/03_regreso.png)

Se guardaron 139 frames con sus tiempos en `resultados.json`, para elaborar un video del recorrido.


## Revisión visual y consola final

Se revisaron las cinco capturas de inspección y la vista interior. Los textos están completos y legibles; las fichas opacas impiden que los textos del mundo se dibujen sobre su contenido. El HUD muestra seis estaciones y cinco objetos. La consola del Editor, consultada por Unity MCP después de salir de Play Mode, contiene 0 errores y 0 advertencias.

La escena DataCenter_VR queda guardada y activa en Edit Mode. Las pruebas en Android y la comodidad con visor siguen fuera de la validación de esta etapa con el simulador.

