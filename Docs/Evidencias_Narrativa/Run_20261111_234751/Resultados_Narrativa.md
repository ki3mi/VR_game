# Recorrido narrativo con XR Interaction Simulator

Ejecución UTC: 2026-11-11T23:47:56.9247946Z. Escena: `Assets/Scenes/DataCenter_VR.unity`. Unity: 6000.6.0f1.
Estado: `failed`. Entradas: teclado y ratón del Input System para simulador XR y botones visibles; texto mediante eventos de teclado de Game View cuando el InputField UGUI requiere Event.PopEvent. No se asigna el texto del campo ni se invocan callbacks de progreso desde la prueba.
Duración configurada del recurso: 90 s. El reloj narrativo avanza entre hitos y se detiene para la lectura. Recurso sin modificación: False.
Tiempo de validación: 7.4 s.

| Caso | Resultado | Observación |
| --- | --- | --- |
| NAR-01 | Cumple | Escena abre con la bienvenida del senior y simulador XR activo. |
| NAR-02 | Cumple | Repetir la explicación mantiene el mismo objetivo. |

## Capturas

- [01_bienvenida.png](C:/Games/grupo03/Docs/Evidencias_Narrativa/Run_20261111_234751/01_bienvenida.png)

## Incidencias

```text
System.InvalidOperationException: Etapa esperada Orientation, actual Welcome
  at AndesSim.Editor.NarrativeValidation+<AwaitStage>d__26.MoveNext () [0x000f8] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:328 
  at AndesSim.Editor.NarrativeValidation.Tick () [0x0006b] in C:\Games\grupo03\Assets\AndesSim\Editor\NarrativeValidation.cs:132 
```
