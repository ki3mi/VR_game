# Recorrido narrativo con XR Interaction Simulator

Ejecución UTC: 2026-11-11T23:46:25.9013322Z. Escena: `Assets/Scenes/DataCenter_VR.unity`. Unity: 6000.6.0f1.
Estado: `failed`. Entradas: teclado y ratón reales del Input System, selección por mirada y botones visibles.
Duración configurada del recurso: 90 s. El reloj narrativo avanza entre hitos y se detiene para la lectura. Recurso sin modificación: False.
Tiempo de validación: 7.4 s.

| Caso | Resultado | Observación |
| --- | --- | --- |
| NAR-01 | Cumple | Escena abre con la bienvenida del senior y simulador XR activo. |
| NAR-02 | Cumple | Repetir la explicación mantiene el mismo objetivo. |

## Capturas

- [01_bienvenida.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261111_234620/01_bienvenida.png)

## Incidencias

```text
System.InvalidOperationException: Resultado inesperado para consulta: andes access
FUENTE: registro local / datos ficticios
04/02/2016 20:36  LOGIN ACCEPTED
cuenta: operador.swift   origen: 10.24.3.17
segundo_factor: AUSENTE   horario: FUERA DE 08:00–18:00

→ La cuenta accedió sin segundo factor fuera del horario autorizado.
  at AndesSim.Editor.NarrativeValidation+<Query>d__24.MoveNext () [0x001e7] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:294 
  at AndesSim.Editor.NarrativeValidation.Tick () [0x0006b] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:132 
```
