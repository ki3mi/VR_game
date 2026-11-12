# Recorrido narrativo con XR Interaction Simulator

Ejecución UTC: 2026-11-11T23:45:01.1844419Z. Escena: `Assets/Scenes/DataCenter_VR.unity`. Unity: 6000.6.0f1.
Estado: `failed`. Entradas: teclado y ratón reales del Input System, selección por mirada y botones visibles.
Duración configurada del recurso: 0 s. El reloj narrativo avanza entre hitos y se detiene para la lectura. Recurso sin modificación: False.
Tiempo de validación: 0.3 s.

| Caso | Resultado | Observación |
| --- | --- | --- |

## Capturas


## Incidencias

```text
System.NullReferenceException: Object reference not set to an instance of an object
  at AndesSim.Editor.NarrativeValidation.Read (System.String property) [0x00000] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:421 
  at AndesSim.Editor.NarrativeValidation.ReadString (System.String property) [0x00000] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:425 
  at AndesSim.Editor.NarrativeValidation.get_Stage () [0x00000] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:428 
  at AndesSim.Editor.NarrativeValidation.Tick () [0x00015] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:128 
```
