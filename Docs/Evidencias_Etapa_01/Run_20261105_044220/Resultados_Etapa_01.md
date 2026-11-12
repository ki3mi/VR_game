# Validación de etapa 1 con XR Interaction Simulator

- Fecha UTC: 2026-11-05T04:42:26.8439512Z
- Responsable: validación automatizada ejecutada por Codex mediante Unity MCP.
- Escena: `Assets/Scenes/DataCenter_VR.unity`.
- Unity: `6000.6.0f1`.
- Entrada: InputSystem.QueueStateEvent para teclado y ratón; tracking del XR Interaction Simulator; selección por mirada con Space.
- Estado: `failed`.
- Teletransportes: 0; destinos visitados: 1; objetos inspeccionados: 0.

## Resultados observados

| Caso | Resultado | Observación |
| --- | --- | --- |
| E1-P01 | Cumple funcionalmente | XRSimulatedHMD presente; giro rastreado por entrada RightArrow: 115.20°. |
| E1-P07 | Falla | Recorrido terminado: False; errores capturados en Play Mode: 1. Consola del Editor se revisa además por MCP después de salir. |

## Evidencias visuales

Las capturas y los frames proceden de Game View durante la ejecución real. La legibilidad de las fichas requiere revisar las imágenes. La comodidad en visor y Android no se certifican con este simulador.

- [01_entrada.png](C:/Games/grupo03/Docs/Evidencias_Etapa_01/Run_20261105_044220/01_entrada.png)

Se guardaron 7 frames con sus tiempos en `resultados.json`, para elaborar un video del recorrido.

## Errores capturados

```text
System.InvalidOperationException: La mirada no alcanza Portal_Entrada. Objetivo actual: ninguno. cameraPos=(7.239, 0.000, -4.594); cameraWorldEuler=(347.302, 180.000, 0.000); cameraLocalEuler=(347.302, 115.200, 0.000); originEuler=(0.000, 64.800, 0.000); aim=(0.000, 1.650, -5.700); primerCollider=ninguno
  at AndesSim.Editor.StageOneValidation+<AimAndSelect>d__26.MoveNext () [0x001ea] in C:\Games\grupo03\Assets\AndesSim\Editor\StageOneValidation.cs:328 
  at AndesSim.Editor.StageOneValidation.Tick () [0x000fd] in C:\Games\grupo03\Assets\AndesSim\Editor\StageOneValidation.cs:182 
```

