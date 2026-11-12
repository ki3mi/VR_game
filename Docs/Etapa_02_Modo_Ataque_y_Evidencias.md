# Etapa 2 de 3: modo ataque y evidencias

## Propósito y resultado demostrable

Convertir el entorno explorable de la etapa 1 en una reconstrucción completa del ataque, con reloj, órdenes, registros y evidencias.

**Al terminar:** el jugador inicia la reconstrucción a las **20:36**, observa las **36 órdenes por US$951 millones**, revisa el log con huecos y llega al cierre de sesión de las **03:59**. El resultado del ataque puede repetirse sin conservar datos de la ejecución anterior.

**Referencia:** [Contexto del proyecto](C:/Games/grupo03/Docs/Contexto_Proyecto_Escenario_3_Centro_de_Datos.md).

Las fechas, cifras y requisitos funcionales proceden del contexto. La secuencia de inicio, los identificadores de evidencias y los criterios adicionales de repetición son decisiones de este plan. Las casillas registran las tareas completadas.

## Dependencias y requisitos que cubre

**Entrada requerida:** [Etapa 1: mapa y entorno explorable](C:/Games/grupo03/Docs/Etapa_01_Mapa_y_Entorno_Explorable.md) terminada y verificada.

| Referencia | Cobertura de esta etapa |
| --- | --- |
| RF-02 | Reloj visible que avanza de 20:36 a 03:59 en ataque. |
| RF-03 | Log con huecos que el jugador puede marcar como evidencia. |
| RF-05, parte de ataque | Registro y presentación de las 36 órdenes del ataque; la comparación completa se cierra en la etapa 3. |
| Arquitectura común | `GameManager`, `ScenarioData`, modos y métodos de evidencias, defensas y cierre. |
| Experiencia | Integración del entorno con narrativa, interfaz y sonido de la reconstrucción. |

La etapa debe conservar el mapa y la navegación ya comprobados. El resultado de defensa y sus controles se implementan en la etapa 3.

## Secuencia propuesta de la experiencia

1. El jugador entra y explora libremente antes de iniciar la reconstrucción.
2. Desde la terminal selecciona **Iniciar reconstrucción**.
3. Se representa el inicio de sesión del 4 de febrero a las 20:36 y comienza el reloj acelerado.
4. Las órdenes alcanzan un total de 36 durante la primera hora y media simulada, hasta las 22:06.
5. El jugador inspecciona el log, el switch y la impresora y registra evidencias.
6. A las 03:59 del 5 de febrero se representa la salida del intruso y se detiene el reloj.
7. El jugador consulta el resumen del ataque y puede reiniciar la ejecución.

Las evidencias deben seguir siendo accesibles después de detenerse el reloj para que su lectura no dependa de la rapidez del jugador. La duración real del reloj se configurará como dato editable y se registrará junto a los resultados de prueba.

## Jugar la reconstrucción

1. Abre la escena y activa Play. Apunta a la terminal SWIFT y pulsa **Espacio** para abrirla. [DataCenter_VR](C:/Games/grupo03/Assets/Scenes/DataCenter_VR.unity)
2. Pulsa **1** o haz clic en **Iniciar reconstrucción**. El reloj comienza a las 20:36.
3. Vuelve a la terminal para ver el registro. Pulsa **1** en esa ficha para marcar los huecos como evidencia. Usa la misma acción en las fichas del switch y de la impresora. **Esc** cierra cada ficha.
4. Tras el cierre de sesión, pulsa **F** con la ficha cerrada o usa **2** desde el registro para ver el resumen. En el resumen, **1** reinicia el ataque.

El reloj recorre toda la noche en **90 segundos** por defecto. Se puede ajustar esa duración desde el campo `durationSeconds` del recurso del escenario. [DataCenterAttack.asset](C:/Games/grupo03/Assets/AndesSim/Data/DataCenterAttack.asset)

## Tareas de implementación

### 1. Crear los datos y el gestor del escenario

- [x] Crear `ScenarioData` como `ScriptableObject` y un recurso de datos para el escenario 3.
- [x] Incluir textos, fechas, hora inicial, hora final, duración acelerada, total de órdenes y monto de referencia.
- [x] Implementar `SimMode` con `Ataque` y `Defensa`.
- [x] Implementar `GameManager` con acceso común a los datos y al modo actual.
- [x] Implementar `RegisterEvidence(string id)`, `ApplyDefense(string id)` y `EndScenario()` conforme al contrato del contexto.
- [x] Preparar el registro de defensas para su uso en la etapa 3; verificar el modo ataque en esta etapa.
- [x] Separar la gestión del progreso de las acciones de entrada para poder usar el simulador y conservar la adaptación de entrega a Cardboard.

El gestor debe diferenciar preparación, ejecución y resultado. Los nombres concretos de esos estados son una decisión de implementación. Al reiniciar, deben limpiarse evidencias, controles registrados, temporizadores, órdenes e interfaz de resultado.

### 2. Implementar el reloj y la secuencia de órdenes

- [x] Crear `SimClock` y mostrar hora y fecha en la interfaz.
- [x] Iniciar en **04/02/2016 20:36**, pasar por medianoche y terminar en **05/02/2016 03:59**.
- [x] Representar las **36 órdenes** dentro de los primeros 90 minutos simulados.
- [x] Mantener el contador en 36 hasta el cierre de sesión.
- [x] Mostrar **US$951 millones** como monto total de las órdenes intentadas.
- [x] Detener los eventos del ataque al finalizar o reiniciar para evitar ejecuciones duplicadas.

La duración real puede reducirse mediante configuración para probar los límites horarios. Las cifras y los 90 minutos narrativos se conservan con cualquier velocidad. Las órdenes se representan con datos ficticios dentro de la aplicación.

### 3. Implementar el visor de registros y las evidencias

- [x] Crear `LogViewer` con líneas legibles, fechas, horas y huecos identificables como registros borrados.
- [x] Añadir la acción de marcar el log como evidencia y conectarla con `RegisterEvidence`.
- [x] Convertir las inspecciones del switch y de la impresora en evidencias registrables mediante `EvidenceItem`.
- [x] Mostrar confirmación y una lista o contador de las evidencias encontradas.
- [x] Evitar que inspeccionar varias veces la misma evidencia aumente el total.

| Identificador propuesto | Origen | Información que debe comunicar |
| --- | --- | --- |
| `log_huecos` | Terminal SWIFT / visor del log. | Hay discontinuidades porque se borraron registros locales. |
| `impresora_apagada` | Impresora. | Las confirmaciones no se imprimieron durante el ataque. |
| `switch_barato` | Switch. | Es evidencia del contexto de infraestructura; la segmentación de red se desarrolla en el escenario 2. |

Los tres registros son una meta de implementación del plan. RF-03 exige específicamente registrar el log; la fuente también identifica switch e impresora como `EvidenceItem`. Ninguna pista debe revelar por sí sola la identidad del cómplice.

### 4. Integrar presentación y resultado del ataque

- [x] Integrar `UIManager` y `AudioManager` para textos, avisos y sonidos de la reconstrucción.
- [x] Mantener la impresora apagada y representar que el acceso nocturno pasa inadvertido en ataque.
- [x] Incorporar narración o textos breves sobre el acceso, las órdenes y los huecos del log.
- [x] Implementar `EndScenario()` para mostrar el resumen: horario, 36 órdenes, US$951 millones y evidencias encontradas.
- [x] Añadir la opción de reiniciar y volver al estado de preparación.
- [x] Identificar el resumen como resultado del ataque; la comparación con defensa se incorpora en la etapa 3.

## Pruebas y criterios de aceptación

Ejecutar con **XR Interaction Simulator** y registrar la duración acelerada utilizada.

| ID | Procedimiento | Resultado esperado |
| --- | --- | --- |
| E2-P01 | Entrar, explorar y después iniciar la reconstrucción. | El reloj permanece en preparación hasta la acción de inicio y comienza en 04/02/2016 20:36. |
| E2-P02 | Observar los primeros 90 minutos simulados. | El contador alcanza exactamente 36 órdenes a más tardar a las 22:06 y no supera ese valor. |
| E2-P03 | Completar la noche simulada. | El reloj cruza de 04/02/2016 a 05/02/2016 y se detiene en 03:59. |
| E2-P04 | Abrir el log y marcarlo como evidencia. | Se ven los huecos y se registra `log_huecos` una sola vez. |
| E2-P05 | Registrar las tres evidencias y volver a seleccionarlas. | Se mantienen tres evidencias únicas, con confirmación visible y sin duplicados. |
| E2-P06 | Abrir el resumen del ataque. | Muestra 36 órdenes intentadas, US$951 millones y los horarios correctos; no presenta esa cifra como dinero efectivamente robado. |
| E2-P07 | Reiniciar y completar un segundo ataque. | El contador y las evidencias vuelven a cero al iniciar; el segundo resultado vuelve a ser 36, sin sumar órdenes ni eventos anteriores. |
| E2-P08 | Repetir el recorrido y las cinco inspecciones de la etapa 1. | Se conserva la navegación y no aparecen errores de ejecución atribuibles al escenario. |

## Entregables de la etapa

- Escena con un modo ataque completo, visor del log, evidencias y resumen.
- Recurso `ScenarioData` y scripts de gestión, reloj, registros, evidencias e interfaz integrados.
- Registro de resultados E2-P01 a E2-P08, con fecha, responsable, configuración del reloj y resultado observado.
- Capturas del cambio de fecha, del log con huecos y del resultado de 36 órdenes.
- Video de una ejecución del ataque en el simulador y commits de las contribuciones realizadas.

## Condición para avanzar

La etapa termina cuando **E2-P01 a E2-P08 cumplen sus resultados esperados**. Debe existir una ejecución de ataque repetible y una arquitectura lista para incorporar los controles de defensa.

## Resultados de la implementación

Las **ocho pruebas** pasaron con XR Interaction Simulator. La prueba usó una copia de los datos con una duración de **24 segundos** por ejecución. La escena guardada conserva los 90 segundos configurados.

Las 36 órdenes se alcanzaron a las **22:06** y el reloj se detuvo a las **03:59** del día siguiente. El primer recorrido registró tres evidencias únicas, incluso al repetir las acciones. El reinicio dejó órdenes y evidencias en cero. El segundo ataque se inició con clic y terminó con 36 órdenes y cero evidencias. También se comprobaron el portal, las seis estaciones, los cinco objetos y el movimiento libre. [Informe de pruebas](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/Resultados_Etapa_02.md)

- [Reloj al cruzar la medianoche](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/05_cambio_de_fecha.png)
- [Registro con huecos y confirmación de evidencia](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/06b_confirmacion_en_log.png)
- [Resumen de las 36 órdenes por US$951 millones](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/07_resumen_ataque.png)
- [Video de la primera ejecución, sin audio](C:/Games/grupo03/Docs/Evidencias_Etapa_02/Run_20261108_231038/video_ataque.avi)

Los cambios quedaron guardados en el proyecto local. Falta un repositorio Git para registrar los commits.

**Continuación:** [Etapa 3: modo defensa y cierre](C:/Games/grupo03/Docs/Etapa_03_Modo_Defensa_y_Cierre.md).
