# Plan de implementación de la narrativa guiada

Fecha de preparación: 11 de noviembre de 2026.

**Estado de la versión entregada:** recorrido completo por texto implementado en `DataCenter_VR`. La sección 12 recoge las adaptaciones autorizadas en la petición de implementación. La guía operativa vigente es `Docs/Guia_Juego_Narrativo.md`. Los apartados sobre producción de voz y video conservan el diseño inicial como antecedente; la versión textual es un entregable final y no depende de esos medios.

## 1. Objetivo y dirección del diseño

Convertir ANDES-SIM en un entrenamiento acompañado para un ingeniero de ciberseguridad recién incorporado al BANCO ANDINO. El ingeniero senior presenta el incidente, propone una tarea concreta, observa su resultado y explica el siguiente paso.

El recorrido será lineal y estará organizado por objetivos verificables. El jugador aprende al consultar la terminal, encontrar evidencias, aplicar controles y reproducir el intento de acceso. Las consultas repetidas, los errores recuperables y las pistas se resuelven dentro del mismo objetivo.

La petición del usuario orienta este plan. Los documentos de las etapas 1, 2 y 3 describen la base funcional inicial. La implementación posterior incorpora la guía de Elliot Alderson, los objetivos, las consultas de terminal y el cierre comparativo descritos en la sección 12.

Se propone una primera experiencia de **8 a 12 minutos**, ajustable tras probarla con personas del público objetivo. Los tiempos de locución, las pausas y los criterios de prueba siguientes son decisiones iniciales del plan. El nombre y la apariencia del senior quedan editables como datos narrativos.

## 2. Premisa y papel del senior

El jugador participa en una reconstrucción controlada de un incidente pasado. Primero observa cómo se produjo el envío fraudulento, después investiga las evidencias y finalmente prepara las defensas antes de repetir el intento.

Esta estructura permite que el senior conozca lo ocurrido y enseñe a reproducirlo. La primera ejecución alcanza 36 órdenes. En la segunda, los controles se aplican antes de intentar acceder y el resultado es 0. El senior presenta los US$951 millones como el monto de las órdenes intentadas.

El senior cumple tres funciones durante cada objetivo:

1. **Encargar.** Explica qué comprobar y para qué sirve.
2. **Acompañar.** Ofrece una pista contextual si el jugador la solicita o permanece inactivo.
3. **Confirmar.** Comenta el resultado observado y enlaza con la siguiente tarea.

Cada intervención inicial tendrá preferentemente una o dos frases y un concepto técnico. Los detalles adicionales se podrán volver a consultar desde el teléfono. El senior responde al estado real del simulador.

## 3. Recorrido narrativo

| Momento | Acción del jugador | Intervención del senior | Condición para avanzar |
| --- | --- | --- | --- |
| Bienvenida | Contestar la videollamada y comenzar el entrenamiento. | Se presenta y resume el incidente. Explica que van a reconstruirlo y prevenir su repetición. | Finaliza u omite la introducción y confirma el inicio. |
| Orientación | Llegar a la terminal SWIFT e inspeccionarla. | Indica cómo mirar, seleccionar y desplazarse. Señala la terminal como primer destino. | Se activa `terminal_swift`. |
| Reconstrucción | Iniciar la noche y observar la sesión, las órdenes y el cierre. | Explica qué cambia a las 20:36, a las 22:06 y a las 03:59. | El ataque termina con 36 órdenes y el reloj en la fecha final. |
| Investigación | Consultar el registro y revisar el switch y la impresora. | Ayuda a interpretar los huecos y a relacionar las fuentes. | Se registran `log_huecos`, `switch_barato` e `impresora_apagada`. |
| Preparación de la defensa | Pasar a Defensa y activar los tres controles en el panel. | Explica MFA, la política de horario y la protección del registro mientras se aplican. | Los tres controles están activos. |
| Reproducción protegida | Volver a la terminal y simular el acceso. | Pide observar el bloqueo y la alarma. | Sesión bloqueada a las 20:36, 0 órdenes y evento protegido disponible. |
| Verificación | Abrir el registro protegido y la comparación. | Vincula el resultado con las defensas y diferencia el papel del SIEM. | Se consultan el evento y la comparación 36 frente a 0. |
| Cierre | Revisar lo aprendido y elegir repetir o terminar. | Resume las evidencias y los controles efectivamente utilizados. | El jugador confirma el cierre o inicia un entrenamiento completo nuevo. |

La investigación permite visitar las tres evidencias en cualquier orden. El senior adapta su comentario al objeto consultado y recuerda las pistas pendientes. El progreso depende de acciones comprobables.

## 4. Guion de referencia

Los siguientes textos son un borrador para implementar y probar el ritmo.

**Presentación del senior**

> «Bienvenido al equipo de seguridad del Banco Andino. Vamos a revisar un incidente de nuestra terminal de pagos. Una cuenta inició sesión a las 20:36, la impresora dejó de emitir confirmaciones y salieron 36 órdenes. Primero reconstruiremos la noche. Después te acompañaré a preparar los controles y repetir la prueba».

**Llegada a la terminal**

> «Esta terminal envía órdenes de pago. Abre el registro de accesos. Vamos a comprobar qué cuenta aparece y a qué hora comienza la sesión».

**Inicio del ataque**

> «La sesión se abre fuera del horario previsto. Observa cómo cambia el contador. El acceso consiguió llegar a la función de envío».

**Hito de las 36 órdenes**

> «A las 22:06 ya se enviaron las 36 órdenes. Su monto total intentado es de 951 millones de dólares. Vamos a revisar qué señales quedaron de esa actividad».

**Huecos del registro**

> «La numeración salta entre estas entradas. En esta reconstrucción se borraron registros locales. Guarda la evidencia y comprueba las otras señales de la sala».

**Impresora y switch**

> «La impresora está apagada, aunque la terminal siguió enviando órdenes. Para saber si una operación ocurrió, conviene contrastar la impresión con la cola y los registros».

> «El switch conecta equipos de la red. Guarda su identificación como contexto de infraestructura. La investigación de la segmentación corresponde al escenario de red».

**MFA y horario**

> «MFA exige un factor adicional a la contraseña. La política de horario decide si ese acceso está autorizado en ese momento. Activa ambos controles y comprueba sus indicadores».

**SIEM**

> «El SIEM reúne los eventos para investigarlos. El control de acceso y la integridad del registro ayudan a conservar la evidencia si alguien modifica una copia local».

**Resultado protegido**

> «El intento quedó bloqueado antes del envío. Revisa el evento protegido y compara el contador con la primera ejecución».

**Cierre**

> «Reconstruiste la sesión, conservaste tres evidencias y preparaste las defensas. La primera ejecución envió 36 órdenes. La repetición protegida terminó con cero. Ahora puedes relacionar cada resultado con los controles que aplicaste».

## 5. Presentación, teléfono y ayudas

### Introducción cinematográfica

- Preparar un encuadre del centro de datos como fondo de bienvenida, con un recorrido visual breve y lento.
- Mostrar un teléfono con una videollamada local del senior, su nombre y rol, subtítulos y controles para continuar, repetir u omitir.
- Proponer una introducción de 45 a 60 segundos como máximo, incluyendo la presentación y el objetivo inicial.
- Usar una cámara de presentación separada de la cámara XR. Al comenzar el recorrido, entregar el control a la cámara con seguimiento del jugador.
- Para la visualización en visor, presentar el movimiento cinematográfico dentro de una superficie de bienvenida y conservar el seguimiento de cabeza del usuario.

El prototipo narrativo inicial puede usar retrato y subtítulos mientras se ajusta el guion. La fase audiovisual incorpora el clip de videollamada, las locuciones y su sincronización con los subtítulos.

### Acompañamiento durante la exploración

El teléfono pasa a un formato compacto cuando comienza el recorrido. Muestra el objetivo activo, la intervención del senior y los accesos a repetir explicación y pedir ayuda. La versión ampliada aparece cuando el jugador la abre.

Se propone una acción consistente para seleccionar objetos y acciones, compatible con ratón en el simulador y con mirada en el flujo XR. Los accesos de teclado pueden servir como atajos. Las acciones de avanzar y pedir ayuda deben ser visibles en la interfaz.

Para cada objetivo se preparan tres niveles de ayuda:

| Nivel | Activación propuesta | Contenido |
| --- | --- | --- |
| Recordatorio | Petición del jugador o 25 segundos de inactividad pertinente. | Repetir qué debe conseguir. |
| Pista contextual | Petición adicional o 50 segundos de inactividad pertinente. | Señalar el objeto o el área de la salida que debe revisar. |
| Demostración | Petición explícita. | Mostrar un ejemplo de la acción y permitir que el jugador la complete. |

Los intervalos se ajustarán con pruebas. Se cuenta inactividad cuando el jugador puede actuar. La lectura de fichas, la reproducción de una explicación y las transiciones suspenden ese contador. La exploración pertinente reinicia el recordatorio.

## 6. Aprendizaje técnico con una consola simulada

Introducir la consola después de que el jugador conozca la terminal y tenga una pregunta concreta que resolver. El primer ejercicio consiste en ejecutar una consulta preparada. El segundo pide completar un filtro y el tercero interpretar la salida resaltando una línea pertinente.

La consola ejecutará un conjunto acotado de operaciones sobre datos ficticios del escenario. Cada consulta incluirá una explicación del propósito, sus parámetros y el resultado que puede revelar. La salida se deriva del estado de la simulación.

| Consulta de entrenamiento | Información buscada | Aprendizaje observable |
| --- | --- | --- |
| Consultar accesos de la terminal en un intervalo. | Cuenta, origen y hora de la sesión. | El jugador relaciona la entrada de las 20:36 con el inicio del incidente. |
| Consultar el estado de la cola de pagos. | Número de órdenes enviadas y horario. | Reconoce que los envíos continuaron con la impresora apagada. |
| Consultar el evento centralizado del intento protegido. | Resultado de bloqueo y controles presentes. | Identifica la evidencia conservada después de la defensa. |

La sintaxis definitiva se elegirá durante la fase técnica del plan. Si se utilizan comandos de una plataforma real, verificar su funcionamiento y explicar qué parte de la salida es una representación ficticia. La ejecución se resolverá dentro de Unity mediante operaciones permitidas sobre el modelo del juego.

Una entrada incorrecta devuelve una explicación breve y un ejemplo editable. El progreso se confirma al obtener el resultado pertinente y señalar la evidencia. Se admite corregir y repetir la consulta.

## 7. Coordinación entre relato y simulación

La reconstrucción histórica y la prueba de defensa son dos ejecuciones distintas. El senior indica expresamente cuándo comienza la segunda y qué preparación necesita.

El reloj actual utiliza 90 segundos de avance continuo. Para el recorrido guiado se proponen pausas en los hitos de las 20:36, las 22:06 y las 03:59. La explicación y su confirmación deben quedar disponibles antes de continuar. Esos 90 segundos representan avance del reloj, con las pausas narrativas contabilizadas por separado.

Implementar pausa y reanudación del reloj del escenario para coordinar las explicaciones. Mantener activos la navegación, la lectura y los controles de la llamada durante las pausas pertinentes.

Cada explicación tiene un identificador y se completa una sola vez dentro de su objetivo. Repetir el audio reproduce la explicación. Omitir una intervención completa su presentación. La condición del objetivo se comprueba por separado mediante el estado y las acciones del jugador.

El senior puede recordar una tarea pendiente y comentar una acción fuera de secuencia. Por ejemplo, si el jugador visita primero el panel, puede indicarle que la investigación de la terminal le permitirá entender los controles. Al regresar al objetivo, conserva las evidencias válidas que ya haya recogido.

## 8. Integración con el código existente

Todas las ampliaciones propias se ubican bajo `Assets/AndesSim/` y usan el namespace `AndesSim`. Se preservan los contratos del núcleo al incorporar el recorrido guiado.

| Elemento | Trabajo previsto |
| --- | --- |
| `TrainingDirector`, nuevo | Coordinar el objetivo activo, las condiciones de avance, la cola de intervenciones y las ayudas. Mantener el progreso narrativo durante el cambio entre ataque y defensa. |
| Datos del entrenamiento, nuevos | Guardar identificadores, orden de objetivos, textos, pistas, clips, subtítulos y destinos de atención como recursos editables. |
| `MentorPresenter`, nuevo | Presentar el teléfono, la videollamada, las locuciones y los subtítulos. Gestionar repetir, omitir y errores de medios. |
| `GameManager` y `SimClock` | Incorporar pausa del avance del reloj y conservar la lógica de órdenes, evidencias, bloqueo y reinicio por ejecución. |
| `ExplorationController` | Reutilizar mirada y selección. Informar sobre el objeto activado y mostrar la acción disponible en contexto. |
| `UIManager` | Coordinar objetivo, teléfono, terminal y comparación para mantener legibles los contenidos. Adaptar el cambio de modos al recorrido guiado. |
| `LogViewer` y terminal | Mostrar las consultas y las líneas pertinentes en cada momento. Registrar la consulta completada y la evidencia interpretada. |
| `DefenseControl` y `AlarmSystem` | Usar la activación real de cada control y el bloqueo para disparar los comentarios del senior. |
| Validaciones del Editor | Añadir el recorrido narrativo a las comprobaciones con XR Interaction Simulator y revisar las pruebas afectadas por las nuevas interacciones. |

`GameManager.Changed` se emite también al avanzar minutos y órdenes. El director debe detectar cambios de condición y completar objetivos una sola vez. Los eventos `TargetActivated` e `InspectionClosed` sirven para observar las interacciones de exploración. Las consultas de consola necesitan una señal específica de finalización.

Se diferencian dos operaciones de reinicio:

- **Repetir una ejecución.** Limpia el estado de ataque o defensa y conserva los resultados que corresponden a la comparación. Usa el comportamiento actual de `ResetScenario`.
- **Comenzar otro entrenamiento.** Restablece objetivos, ayudas, intervenciones y resultados previos para comenzar con una sesión nueva. Requiere una operación explícita para limpiar el historial comparativo, que actualmente se conserva al reiniciar.

Al cambiar de objetivo o reiniciar, cancelar las intervenciones pendientes y sus callbacks. Las suscripciones al núcleo se retiran al salir del recorrido. Los clips que fallen permiten continuar mediante subtítulos y controles de presentación.

## 9. Fases de implementación y resultados medibles

| Fase | Implementación | Resultado verificable |
| --- | --- | --- |
| 1. Recorrido guiado de extremo a extremo | Datos narrativos, director, teléfono con textos, objetivo activo y acciones de continuar, omitir, repetir y ayuda. Integrar los ocho momentos con el núcleo. | Desde una sesión nueva se llega a la comparación con una acción siguiente visible en cada paso. Los objetivos se completan por condiciones reales y cada transición ocurre una sola vez. |
| 2. Ritmo y presencia del senior | Encuadre de bienvenida, videollamada local, voz, subtítulos y pausas de los hitos. Resolver competencia entre voz, alarma y ambiente. | La introducción puede completarse u omitirse. El reloj espera las explicaciones previstas y se reanuda una sola vez. Cada clip tiene subtítulos y una salida funcional ante error de carga. |
| 3. Práctica técnica acompañada | Consultas simuladas, filtros editables, explicación contextual e interpretación de evidencia. | El jugador completa las tres consultas, puede corregir entradas y observa resultados consistentes con cada ejecución. |
| 4. Revisión de comprensión y cierre | Ajustar duración, pistas y señalización. Integrar conclusiones del senior con comparación, créditos y reinicios. | Se aprueba el recorrido con el simulador y una prueba de uso con personas del público objetivo. El cierre muestra las acciones realizadas y la comparación correcta. |

Las fases se implementan en ese orden para poder probar primero el acompañamiento completo. Antes de integrar cada cambio, revisar los recursos y la escena actual del usuario. Registrar evidencia nueva para el comportamiento modificado.

## 10. Pruebas de aceptación

Ejecutar las verificaciones funcionales con **XR Interaction Simulator** y registrar la duración configurada del reloj. Las pruebas de comprensión requieren observar a una persona que use el recorrido.

| ID | Prueba | Criterio de aceptación |
| --- | --- | --- |
| NG-01 | Entrar al entrenamiento y omitir la videollamada. | Se obtiene el mismo primer objetivo y se recupera el control del jugador. |
| NG-02 | Repetir una explicación y pulsar continuar varias veces. | Se mantiene una sola reproducción activa y una sola transición por objetivo. |
| NG-03 | Consultar el teléfono durante exploración. | El objetivo y la ayuda son accesibles y se puede retomar la tarea pendiente. |
| NG-04 | Permanecer inactivo, leer una ficha y solicitar ayuda. | Las pistas corresponden al objetivo y el contador respeta los estados de lectura y locución. |
| NG-05 | Reconstruir el ataque con las pausas narrativas. | Los hitos conservan sus fechas, las órdenes llegan a 36 a las 22:06 y el cierre queda a las 03:59. |
| NG-06 | Registrar y volver a consultar las tres evidencias. | El registro mantiene tres IDs únicos y el senior confirma cada hallazgo una sola vez. |
| NG-07 | Escribir una consulta incompleta y corregirla. | Se explica cómo corregirla y se puede completar la consulta válida dentro del mismo objetivo. |
| NG-08 | Activar los controles y reproducir la defensa. | El senior acompaña los tres controles. El intento termina a las 20:36 con alarma, evento protegido y 0 órdenes. |
| NG-09 | Abrir la comparación y el cierre del entrenamiento. | Se distinguen las 36 órdenes del ataque y las 0 de defensa, con su procedencia y comentarios acordes a lo observado. |
| NG-10 | Reiniciar una ejecución y comenzar otro entrenamiento. | Cada operación conserva o limpia el historial según su alcance y restaura reloj, controles, alarma y relato. |
| NG-11 | Simular un clip ausente o fallido. | El objetivo sigue siendo realizable con texto y controles de presentación. |
| NG-12 | Revisar las pantallas con teléfono, terminal y subtítulos abiertos. | Los textos y las acciones se leen completos, con un objetivo activo identificable. |

Para la prueba de uso se proponen tres participantes con nociones tecnológicas. Registrar el tiempo hasta la primera interacción, las ayudas solicitadas y los puntos de bloqueo. Como meta inicial, al menos dos de los tres deberían llegar al cierre usando las ayudas integradas y explicar al final qué aportan MFA, horario y SIEM. Esta meta orienta la revisión del prototipo y se ajustará con los resultados.

## 11. Entregables del rediseño

- Escena con recorrido guiado, videollamada de bienvenida y acompañamiento contextual del senior.
- Guion, subtítulos, pistas y clips organizados por objetivo, con autoría y licencias de los medios.
- Terminal de entrenamiento con consultas explicadas y datos del escenario.
- Comparación final y reinicios del recorrido y de cada ejecución.
- Informe de pruebas con el simulador, capturas de los momentos principales y observaciones de la prueba de uso.

Las fechas, cifras y conceptos del caso se conservan como referencia técnica del contenido. La interfaz y el ritmo se diseñan para el entrenamiento acompañado solicitado por el usuario.

## 12. Decisiones de la implementación final por texto

La petición posterior del usuario autorizó adaptar el plan para entregar un recorrido completo con el senior guiando mediante texto. La interfaz vigente es un **teléfono móvil permanente a la derecha** con un chat con **Elliot Alderson**. Los objetivos, las instrucciones y las explicaciones aparecen dentro de esa interfaz. El historial conserva los mensajes del jugador y del ingeniero; se revisa mediante scroll con barra lateral. Antes de cada respuesta se muestra el indicador animado de escritura y su llegada activa una notificación sonora sintetizada en el proyecto.

Cada mensaje recibido enfoca automáticamente la última burbuja. Si hay varias indicaciones antes de una acción, se entrega una sola y el botón verde muestra **Siguiente**. Ese botón envía **Ok** y solicita la siguiente indicación. Las acciones del objetivo se restauran al terminar la secuencia; no se ejecutan al confirmar la lectura. El nombre visible del senior no lleva prefijo.

Las acciones relevantes del jugador producen mensajes de avance al ingeniero. La terminal ocupa el lado izquierdo y conserva el teléfono visible: comandos en cian, salidas válidas en verde y errores en coral. La interpretación y las explicaciones pedagógicas se reciben en el chat. No se requieren retratos, videos ni voces por producir.

El recorrido utiliza acciones explícitas y explicaciones completas, con guía paso a paso disponible durante la exploración y las consultas. Los botones de confirmación acompañan la interpretación; no hay un examen ni un árbol de decisiones. Un marcador indica el equipo pertinente y **Llévame al objetivo** ofrece acceso directo a su estación.

La sintaxis elegida es la consola ficticia `andes`, con consultas de accesos, pagos enviados y eventos bloqueados. Se interpreta completamente dentro del juego. La guía de uso especifica sus tres comandos y parámetros.

El reloj se mantiene detenido durante la lectura. La reproducción guiada del intervalo 20:36–22:06 dura ocho segundos y actualiza las órdenes a través del núcleo existente. Después de verificar la cola, el jugador confirma el salto al cierre exacto de las 03:59. El recurso original de 90 segundos no se modifica. Este ritmo evita esperar durante el aprendizaje y conserva las fechas y cifras del caso.

Las fases de medios audiovisuales se sustituyen por una interfaz textual completa, con mensajes desplazables, consola, objetivos, explicación de cada control y cierre. Los detalles operativos de la versión entregada están en `Docs/Guia_Juego_Narrativo.md`. Las pruebas de comprensión con participantes y la entrega Android se deben distinguir de la validación funcional en XR Interaction Simulator.
