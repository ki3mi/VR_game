# Etapa 3 de 3: modo defensa, comparación y cierre

## Propósito y resultado demostrable

Completar la experiencia mediante los controles de defensa y una comparación verificable entre el ataque y su prevención.

**Al terminar:** el jugador activa MFA, alerta fuera de horario y SIEM, reproduce el intento de acceso de las **20:36** y observa alarma, bloqueo y **0 órdenes enviadas**. Puede comparar ese resultado con las **36 órdenes del ataque** y repetir ambos modos dentro de la misma aplicación.

**Referencia:** [Contexto del proyecto](C:/Games/grupo03/Docs/Contexto_Proyecto_Escenario_3_Centro_de_Datos.md).

Las defensas, la comparación y los entregables proceden del contexto. La preparación previa de los controles y la secuencia de repetición son decisiones de este plan para obtener una demostración consistente. Las casillas distinguen el cierre funcional y los pendientes de entrega.

## Dependencias y requisitos que cubre

**Entrada requerida:** [Etapa 2: modo ataque y evidencias](C:/Games/grupo03/Docs/Etapa_02_Modo_Ataque_y_Evidencias.md) terminada y verificada, conservando el entorno de la [etapa 1](C:/Games/grupo03/Docs/Etapa_01_Mapa_y_Entorno_Explorable.md).

| Referencia | Cobertura de esta etapa |
| --- | --- |
| RF-04 | Tres controles funcionales: MFA, alerta fuera de horario y SIEM. |
| RF-05 | Pantalla final que compara 36 órdenes en ataque frente a 0 en defensa. |
| CA-02 | Alarma al iniciar el intento de sesión en defensa. |
| Requisitos generales | Modos en una misma aplicación, sonido, pantalla final, créditos y preparación de entrega. |
| Arquitectura común | Uso completo de `ApplyDefense`, `EndScenario` y la gestión de modos. |

## Secuencia propuesta del modo defensa

1. Elegir modo defensa desde la preparación del escenario.
2. Recorrer el centro de datos y activar los tres interruptores del panel.
3. Mostrar cada control activado y su explicación.
4. Habilitar **Simular intento de acceso** cuando estén activos los tres controles.
5. Al ejecutar el intento de las 20:36, activar alarma sonora y luz roja, bloquear la sesión y registrar el evento protegido.
6. Mantener el contador de órdenes en cero y mostrar el resultado comparativo.
7. Permitir reiniciar para elegir otro modo.

La espera hasta activar los tres controles es una decisión de alcance para esta implementación: con controles incompletos se muestra qué falta y el intento de defensa permanece pendiente. La demostración evaluable se realiza con las tres defensas aplicadas. El bloqueo debe ejecutarse desde el primer intento en modo defensa; no se representa una sesión exitosa antes de la alarma.

## Tareas de implementación

### 1. Implementar los tres controles

- [x] Sustituir la inspección provisional del panel por tres interruptores `DefenseControl`.
- [x] Asignar un identificador único a cada control y conectarlo con `ApplyDefense(string id)`.
- [x] Mostrar visualmente cuáles están activos y cuántos faltan.
- [x] Evitar registros duplicados al seleccionar un control ya activado.
- [x] Reiniciar el estado de los tres controles al comenzar una ejecución nueva.

| Control | Identificador propuesto | Explicación y comportamiento esperado |
| --- | --- | --- |
| MFA y protección de accesos privilegiados | `mfa` | Explicar que una contraseña robada no basta y que las cuentas privilegiadas requieren controles adicionales. |
| Alerta y bloqueo fuera de horario | `fuera_horario` | Detectar el intento nocturno, disparar la alarma y bloquear la sesión. |
| SIEM y protección de registros | `siem` | Mostrar un evento centralizado y protegido que permanece disponible como evidencia del intento. |

El contenido debe explicar que la protección de los registros requiere integridad y control de acceso además de la centralización. La prevención del acceso se atribuye a los controles de autenticación y horario; el SIEM aporta visibilidad y conservación de evidencias.

### 2. Implementar alarma, bloqueo y resultado de defensa

- [x] Crear `AlarmSystem` con activación coordinada de sonido, luz roja y mensaje de sesión bloqueada.
- [x] Ejecutar la alarma al dispararse el intento de las 20:36 con la defensa preparada.
- [x] Impedir que se inicie la secuencia de envío del modo ataque.
- [x] Mantener el total de órdenes de defensa en **0**.
- [x] Incorporar en el visor el evento protegido del intento y su resultado de bloqueo.
- [x] Finalizar el modo defensa mediante `EndScenario()`.

La condición del intento se evalúa al comenzar la simulación, sin depender de que el reloj alcance después otra hora. Al reiniciar, deben detenerse el sonido y la luz de alarma y restablecerse los estados de la escena.

### 3. Completar comparación, selección y reinicio

- [x] Permitir elegir ataque o defensa dentro de la misma aplicación antes de ejecutar la reconstrucción.
- [x] Mostrar el modo actual en la interfaz y mantenerlo fijo durante cada ejecución.
- [x] Incorporar un resumen final con ambas columnas de comparación.
- [x] Indicar qué columna corresponde al modo recién ejecutado.
- [x] Presentar los datos del otro modo como referencia del escenario si todavía no se ha ejecutado, para distinguirlos del resultado de la sesión actual.
- [x] Permitir reiniciar, elegir otro modo y repetir el recorrido sin recargar manualmente la escena desde el Editor.

| Dato | Ataque | Defensa |
| --- | --- | --- |
| Intento de sesión | 04/02/2016, 20:36; acceso inadvertido. | 04/02/2016, 20:36; intento detectado y bloqueado. |
| Órdenes enviadas | **36**. | **0**. |
| Monto de órdenes intentadas | **US$951 millones**. | **US$0 en órdenes enviadas**. |
| Registros | Huecos en el log local. | Evento centralizado y protegido. |
| Conclusión | Las vulnerabilidades permiten el envío. | Las defensas evitan el envío y conservan evidencia del intento. |

En el ataque se conserva el cierre de sesión a las 03:59. En defensa, el bloqueo se produce al intentar acceder a las 20:36 y no requiere reproducir toda la noche.

### 4. Afinar la experiencia y preparar la entrega

- [x] Revisar legibilidad, señalización, instrucciones, volúmenes y respuesta de las interacciones con el simulador.
- [x] Completar créditos de recursos, autores, licencias y herramientas de IA dentro de la app.
- [x] Revisar fechas, cifras y continuidad con el escenario 4.
- [x] Verificar que todos los datos usados sean ficticios y que las acciones del ataque solo existan dentro de la simulación.
- [ ] Preparar el repositorio con instrucciones de apertura, ejecución con XR Interaction Simulator y generación del APK.
- [x] Preparar la ficha, el guion del video y la demostración final.

## Pruebas y criterios de aceptación funcional

Ejecutar estas pruebas con **XR Interaction Simulator en el Editor de Unity**.

| ID | Procedimiento | Resultado esperado |
| --- | --- | --- |
| E3-P01 | Entrar en defensa y seleccionar los tres controles. | Se registran tres defensas distintas; repetir selecciones no aumenta el total. |
| E3-P02 | Intentar iniciar la simulación con cero, uno y dos controles. | La aplicación indica qué falta; no comienza el intento ni se envían órdenes. |
| E3-P03 | Activar los tres controles e iniciar el intento de acceso. | A las 20:36 se activan sonido, luz roja y mensaje de bloqueo, antes de cualquier envío. |
| E3-P04 | Revisar el resultado y el visor después del bloqueo. | Hay cero órdenes y un registro protegido del intento. |
| E3-P05 | Revisar la pantalla final después de cada modo. | Compara 36 frente a 0, identifica el modo ejecutado y distingue los valores de referencia. |
| E3-P06 | Completar ataque, reiniciar, completar defensa, reiniciar y completar otro ataque. | Los resultados son 36, 0 y 36; los reinicios limpian evidencias, controles, reloj, alarma y contadores. |
| E3-P07 | Recorrer nuevamente todas las estaciones e inspeccionar los objetos. | Se conservan portal, teletransporte y al menos cinco objetos interactivos funcionales. |
| E3-P08 | Ejecutar el flujo completo de ambos modos y abrir instrucciones y créditos. | La experiencia se completa y no aparecen errores de ejecución atribuibles al escenario. |

Registrar fecha, responsable, modo, configuración de prueba, resultado observado y capturas o referencia al video para E3-P01 a E3-P08. Conservar especialmente la evidencia de la alarma y la secuencia **36 → 0 → 36**.

## Entregables y validación de la entrega Android

Las pruebas de desarrollo de las tres etapas usan el simulador, por decisión del equipo. El HTML también exige APK y validación en dispositivo; su estado debe registrarse por separado para que una prueba en el Editor no se contabilice como prueba en Android.

| Entregable | Criterio de cierre | Estado |
| --- | --- | --- |
| APK | Instalación Android con ambos modos y la configuración del contexto. | Pendientes Cardboard, entrada para visor y compilación. |
| Validación en dispositivo | Dos celulares con pruebas de arranque, Cardboard, sonido, legibilidad y teletransporte. | Pendiente del APK y los celulares del equipo. |
| Repositorio GitHub | Enlace, instrucciones de ejecución y commits de los integrantes. | Instrucciones preparadas. Falta el repositorio Git para registrar contribuciones. |
| Video final | Grabación de 2 a 3 minutos desde un celular que muestre ataque y defensa. | Guion de 2:30 preparado. Grabación móvil pendiente. |
| Ficha del proyecto | Hasta 4 páginas con escenario, vulnerabilidad, controles, capturas, roles y créditos. | Contenido y capturas listos. Quedan nombres del equipo y exportación. |
| Presentación | 7 minutos de demostración y 3 de análisis con una hipótesis sustentada. | Guion preparado. |

## Condición de cierre

- [x] E3-P01 a E3-P08 cumplen sus resultados esperados y tienen evidencias.
- [x] La comparación final muestra **36 órdenes frente a 0** y la repetición no arrastra estados de ejecuciones anteriores.
- [x] Se conserva la cobertura de RF-01 a RF-05 y del recorrido por teletransporte de las etapas previas.
- [ ] Están preparados los entregables y el registro de créditos.
- [x] La tabla de entrega Android tiene un resultado observado para cada elemento; cualquier validación aún no realizada permanece señalada como pendiente.

La implementación funcional queda verificada cuando pasa las pruebas con el simulador. El cierre de la entrega académica requiere además completar los elementos Android y documentales exigidos por el HTML.

## Cierre funcional en Unity

La escena quedó guardada con los modos Ataque y Defensa. El jugador selecciona el modo en la terminal y activa MFA, alerta fuera de horario y SIEM en el panel. La simulación de defensa bloquea el intento de las **20:36**, enciende la baliza roja, reproduce la alarma y conserva el evento protegido en el visor. [Escena DataCenter_VR](C:/Games/grupo03/Assets/Scenes/DataCenter_VR.unity)

Se aprobaron **8 de 8 pruebas** con XR Interaction Simulator. Se comprobaron los intentos con controles incompletos, la comparación **36 frente a 0**, el ciclo **36 → 0 → 36**, el reinicio de la alarma y la apertura de instrucciones y créditos. El informe incluye diez capturas. [Informe de validación](C:/Games/grupo03/Docs/Evidencias_Etapa_03/Run_20261109_050428/Resultados_Etapa_03.md)

La ficha, los pasos para repetir las pruebas, los guiones y los pendientes de entrega están reunidos en el documento de entrega. [Entrega de la etapa 3](C:/Games/grupo03/Docs/Entrega_Etapa_03.md)
