# Contexto del proyecto: escenario 3 «Centro de datos»

## 1. Fuente y alcance del documento

Este documento reúne los objetivos, requisitos y condiciones de entrega del escenario 3 de **ANDES-SIM: La transferencia fantasma**, ambientado en el **BANCO ANDINO**.

- **Fuente:** [Semana10_VR_AR_CASO_BANCO_ANDINO.html](C:/Users/ccent/Downloads/Semana10_VR_AR_CASO_BANCO_ANDINO.html).
- **Modalidad del escenario:** realidad virtual (VR) con Cardboard.
- **Escena indicada:** `DataCenter_VR`.
- **Plataforma de entrega:** aplicación Android desarrollada en Unity 6.
- **Entorno de pruebas definido por el equipo:** XR Interaction Simulator en el Editor de Unity.
- **Navegación de pruebas actualizada por el equipo:** movimiento libre con WASD y E/Q, además de teletransporte.
- **Alcance asignado:** desarrollar el centro de datos y su comparación entre modo ataque y modo defensa.

Los requisitos se extraen de la ficha del escenario 3 y de las secciones generales de arquitectura, requisitos, roles, entregables, reglas y rúbrica del HTML. El uso de XR Interaction Simulator para las pruebas es una decisión posterior del equipo. Las decisiones que la fuente deja abiertas se identifican al final. Las referencias externas incluidas en el HTML no se han consultado para esta síntesis.

## 2. Contexto narrativo

En la ficción del proyecto, en octubre de 2026 la Unidad de Respuesta del BANCO ANDINO detecta indicios de un ataque similar al ocurrido en el Banco de Bangladesh en 2016. ANDES-SIM permite reconstruir la cadena del ataque, analizar sus fallas y simular las defensas que lo habrían detenido. Cada equipo desarrolla un escenario de esa cadena.

El escenario 3 recrea la noche del **jueves 4 al viernes 5 de febrero de 2016**:

- A las **20:36**, con el banco cerrado, un intruso inicia sesión en la terminal SWIFT.
- En una hora y media se envían **36 órdenes por un total de US$951 millones**.
- La impresora de confirmaciones permanece apagada y los registros de la base de datos presentan huecos por información borrada.
- A las **03:59**, el intruso cierra la sesión.

El jugador recorre el centro de datos, inspecciona el registro y activa controles de seguridad. La comparación debe mostrar cómo cambia el resultado al aplicar las defensas.

Las 36 órdenes representan envíos intentados; los US$951 millones no deben presentarse como dinero efectivamente robado. El procesamiento posterior de las órdenes corresponde al escenario 4, con cuyo equipo debe coordinarse la continuidad narrativa.

## 3. Objetivos

### Objetivo general

Desarrollar una experiencia VR educativa para Android que permita reconstruir el acceso nocturno a la terminal SWIFT, identificar las vulnerabilidades que lo facilitaron y aplicar contramedidas simuladas que bloqueen la sesión y eviten el envío de órdenes.

### Objetivos específicos

1. Recrear un centro de datos con cuatro racks, terminal SWIFT, switch e impresora, manteniendo los horarios y cifras del escenario.
2. Permitir la exploración mediante mirada, retícula y teletransporte, con interacciones comprensibles. Las pruebas del Editor admiten también movimiento libre por decisión del equipo; el HTML plantea teletransporte sin movimiento continuo para la experiencia Cardboard.
3. Mostrar registros con huecos y permitir marcarlos como evidencia.
4. Explicar la relación entre accesos fuera de horario, credenciales sin MFA y registros locales manipulables.
5. Implementar los controles de defensa y una alarma audiovisual que acompañe el bloqueo de la sesión.
6. Comparar en la pantalla final **36 órdenes en ataque frente a 0 en defensa**.
7. Entregar un APK funcional, su código y las evidencias de validación requeridas.
8. Validar los recorridos, las interacciones y los resultados de ambos modos con XR Interaction Simulator en el Editor de Unity.

## 4. Vulnerabilidades y contramedidas

| Vulnerabilidad descrita en la fuente | Defensa requerida | Representación en la experiencia |
| --- | --- | --- |
| Inicio de sesión fuera del horario laboral sin alerta. | Alerta y bloqueo de accesos fuera de horario. | En defensa, el intento de acceso dispara una alarma y se bloquea. |
| Credenciales robadas sin doble factor. | Autenticación multifactor (MFA) y control de cuentas privilegiadas. | El panel permite activar MFA y explicar la protección de accesos privilegiados. |
| Registros locales que el atacante pudo borrar. | Registros centralizados e inalterables mediante SIEM, según la formulación del HTML. | El visor permite reconocer los huecos del registro y explicar la defensa. |

**Precisión para el contenido educativo:** la centralización en un SIEM debe acompañarse de protección de integridad y acceso para sostener la condición de registros inalterables exigida por el proyecto. La fuente no especifica el mecanismo técnico de esa protección.

## 5. Flujo del jugador y modos

1. Entrar por el portal al centro de datos.
2. Recorrer el pasillo de racks mediante teletransporte.
3. Acceder a la terminal SWIFT.
4. Revisar el log, reconocer sus huecos y registrar la evidencia.
5. Activar los controles desde el panel en modo defensa.
6. Consultar el resultado y la comparación entre modos.

| Aspecto | Modo ataque | Modo defensa |
| --- | --- | --- |
| Sesión de las 20:36 | Pasa inadvertida. | Dispara la alarma y se bloquea. |
| Controles | Se reconstruyen las condiciones vulnerables de la noche. | Se activan MFA, alerta fuera de horario y SIEM desde el panel. |
| Reloj | Avanza de 20:36 a 03:59 de forma acelerada. | La fuente no exige completar el mismo avance tras el bloqueo. |
| Evidencias | Se observan huecos en el log y la impresora apagada. | Se explica cómo los controles cambian el resultado. |
| Órdenes enviadas | 36, por US$951 millones. | 0. |

## 6. Requisitos funcionales del escenario

Se conserva la numeración RF-01 a RF-05 de la ficha del escenario.

| ID | Requisito |
| --- | --- |
| RF-01 | La escena debe tener un pasillo con **4 racks**, la **terminal SWIFT**, un **switch** y la **impresora**. |
| RF-02 | Un reloj visible en pantalla debe avanzar de **20:36 a 03:59** en modo ataque. |
| RF-03 | El jugador debe poder revisar un log con huecos correspondientes a registros borrados y marcarlo como evidencia. |
| RF-04 | En modo defensa, el jugador debe poder activar **MFA**, **alerta fuera de horario** y **SIEM** desde un panel. |
| RF-05 | La pantalla final debe comparar las órdenes enviadas en cada modo: **36 frente a 0**. |

### Requisitos generales aplicables a este escenario VR

- Usar **Unity 6** y generar un **APK instalable en un celular Android real**.
- Incluir los modos ataque y defensa dentro de la misma aplicación, con resultados diferentes.
- Incorporar portal de entrada, teletransporte por mirada, **al menos 5 objetos interactivos con la retícula**, sonido y pantalla final.
- Usar la arquitectura común con `GameManager`, `ScenarioData`, `RegisterEvidence`, `ApplyDefense` y `EndScenario`.
- Mantener un repositorio de GitHub con commits de todos los integrantes.
- Utilizar recursos con licencia libre e incluir los créditos dentro de la aplicación.
- Realizar las pruebas funcionales de desarrollo con **XR Interaction Simulator**. El HTML establece además pruebas en **2 celulares** como parte de las tareas generales de ciberseguridad y QA para la validación de la entrega Android.
- Mantener coherencia de fechas, cifras, personajes y hechos con los demás escenarios.

Los requisitos de Vuforia, Image Targets y marcadores PDF corresponden a los escenarios AR y quedan fuera del alcance asignado al escenario 3.

## 7. Especificación técnica y entorno de pruebas

### Configuración base VR indicada por el HTML

- Cardboard XR Plugin y entrada por mirada con `CardboardReticlePointer`.
- Android **8 o superior**, API mínima **26**.
- Backend **IL2CPP**, arquitectura **ARM64** y API gráfica **OpenGLES3**.
- Capa `Interactive` para los objetos detectados por la retícula.
- Orientación **Landscape Left**.
- Escena `DataCenter_VR`, con iluminación baja y luces parpadeantes.

### Entorno de pruebas definido por el equipo

Se utilizará **XR Interaction Simulator** en el **Editor de Unity** para las pruebas de desarrollo del escenario. Los recorridos y casos de prueba deben poder ejecutarse en ese entorno para comprobar:

- Entrada por el portal y navegación por teletransporte.
- Movimiento libre del simulador con WASD para traslación horizontal y E/Q para subir y bajar, según la actualización del equipo.
- Selección de los objetos interactivos y registro de evidencias.
- Lectura del log y activación de los tres controles de defensa.
- Avance del reloj, alarma, bloqueo de sesión y pantalla final en ambos modos.

La configuración de entrada para las pruebas debe permitir verificar estas acciones con el simulador. El APK Android y la interacción de entrega con Cardboard conservan los requisitos documentados en el HTML. La instalación en Android y la comodidad de uso en VR requieren la validación en dispositivo prevista por la fuente.

### Objetos y prefabs

| Elemento | Especificación de la fuente |
| --- | --- |
| `Rack_01..04` | Cuatro racks con LEDs mediante material emisivo animado. |
| `Terminal_SWIFT` | Terminal con un Canvas para visualizar el log. |
| `Switch_Barato` | Objeto de evidencia con componente `EvidenceItem`. |
| `Impresora` | Objeto de evidencia con componente `EvidenceItem`; apagada durante el ataque. |
| `Panel_Controles` | Panel con tres interruptores `DefenseControl`. |

El portal, los puntos de teletransporte y los interactuables deben completar los mínimos generales de VR. El diseño debe identificar al menos cinco objetos que respondan efectivamente a la retícula.

### Scripts específicos

| Script | Responsabilidad |
| --- | --- |
| `SimClock` | Ejecutar el reloj acelerado de la noche, contemplando el cambio de día. |
| `LogViewer` | Mostrar las líneas y los huecos del registro. |
| `AlarmSystem` | Activar sonido, luz roja y bloqueo en modo defensa. |
| `LedBlink` | Producir el parpadeo aleatorio de los LEDs. |

### Arquitectura común e integración

El flujo compartido conecta la entrada VR con los interactuables, `GameManager`, la interfaz y el audio, y la pantalla final.

| Elemento | Contrato indicado por la fuente |
| --- | --- |
| `SimMode` | Enumeración con los valores `Ataque` y `Defensa`. |
| `GameManager.I` | Instancia única del gestor. |
| `GameManager.data` | Referencia a `ScenarioData`. |
| `GameManager.mode` | Modo actual; el ejemplo del HTML inicia en `Ataque`. |
| `RegisterEvidence(string id)` | Registrar una pista encontrada. |
| `ApplyDefense(string id)` | Registrar una contramedida activada. |
| `EndScenario()` | Mostrar el resultado del modo actual. |
| `ScenarioData` | `ScriptableObject` para textos, fechas y cifras del escenario. |
| `EvidenceItem` / `DefenseControl` | Interactuables de evidencias y controles. |
| `UIManager` / `AudioManager` | Presentación de interfaz y sonido dentro de la arquitectura compartida. |

Los datos del escenario deben conservar la hora inicial **20:36**, la hora de salida **03:59**, las **36 órdenes** y el monto de **US$951 millones**. La fuente no fija nombres de campos para estos datos.

## 8. Criterios de aceptación y validación

### Criterios explícitos del escenario

- **CA-01 del HTML para la experiencia Cardboard:** el jugador recorre la sala sin mareo mediante teletransporte, sin movimiento continuo. Las pruebas del Editor incorporan además movimiento libre por petición posterior del equipo.
- **CA-02:** la alarma se dispara al iniciar la sesión en modo defensa.

### Pruebas funcionales con XR Interaction Simulator

Las pruebas funcionales se ejecutarán en el Editor de Unity con XR Interaction Simulator. Esta lista traduce los requisitos anteriores en verificaciones; no añade metas de rendimiento ni funciones nuevas.

- [ ] El escenario puede recorrerse y sus interacciones pueden ejecutarse con XR Interaction Simulator.
- [ ] Están presentes los cuatro racks, la terminal, el switch y la impresora.
- [ ] Funcionan el portal, el teletransporte por mirada y al menos cinco objetos interactivos con retícula.
- [ ] En ataque, el reloj cruza la medianoche y termina a las 03:59.
- [ ] El log muestra huecos y permite registrar la evidencia mediante `RegisterEvidence`.
- [ ] Los tres interruptores permiten aplicar las defensas y registrarlas mediante `ApplyDefense`.
- [ ] En defensa, el inicio de sesión activa sonido, luz roja y bloqueo.
- [ ] La pantalla final muestra la comparación de 36 órdenes frente a 0 y explica el resultado.
- [ ] Sonido, interfaz y créditos están disponibles en la aplicación.

### Validación de entrega indicada por el HTML

- [ ] El APK se instala y ejecuta sin errores en un celular Android real; se documentan pruebas en dos celulares.
- [ ] Se verifica la interacción de entrega con Cardboard y la comodidad del recorrido por teletransporte en dispositivo.

## 9. Organización del equipo

La fuente contempla equipos de **3 o 4 estudiantes**. Con tres integrantes, el líder asume también ciberseguridad y QA. Todos deben programar alguna parte y aparecer en los commits.

| Rol | Responsabilidades del escenario y del proyecto |
| --- | --- |
| Líder de proyecto y narrativa | Escribir la narración de la noche; coordinar la línea de tiempo con el escenario 4; organizar sprints semanales en Trello o GitHub Projects; preparar cronograma, guion, ficha y presentación. |
| Desarrollador Unity (C#) | Programar reloj, visor de logs, interruptores y alarma; implementar la arquitectura y la interacción VR; habilitar las pruebas con XR Interaction Simulator; integrar arte y sonido; generar y probar el APK. |
| Artista 3D, UI y sonido | Modelar racks, terminal e impresora; crear la atmósfera sonora, la interfaz y los efectos; preparar recursos, créditos y clips del tráiler según las tareas generales del rol. |
| Analista de ciberseguridad y QA | Diseñar el log con huecos; explicar MFA, SIEM y alertas fuera de horario; investigar las fuentes del caso; validar la contramedida; ejecutar los casos funcionales con XR Interaction Simulator y preparar el informe QA; documentar las pruebas en dos celulares previstas por la fuente. |

El cómplice interno es una incógnita narrativa compartida. La presentación debe proponer una hipótesis sustentada; la ficha del escenario 3 no identifica al culpable.

## 10. Entregables y presentación

1. **APK instalable** en Android.
2. **Enlace al repositorio de GitHub**, con scripts y participación de todos los integrantes.
3. **Video de 2 a 3 minutos grabado desde el celular**, mostrando ataque y defensa.
4. **Ficha del proyecto de máximo 4 páginas**, con escenario, vulnerabilidad, contramedida, conceptos aplicados, capturas, roles y créditos.
5. **Presentación final de 10 minutos:** 7 de demostración y 3 de análisis. Los equipos presentan en orden de escenario, participan en la activación conjunta de defensas y sustentan su hipótesis del cómplice.

El cronograma, guion, análisis técnico, casos de prueba e informe QA también figuran como productos de los roles. La fuente menciona un tráiler en esas tareas, pero no fija su duración ni lo enumera como una entrega adicional separada del video.

## 11. Reglas de contenido y uso exigidas por la fuente

- Ejecutar los ataques únicamente como simulación dentro de la aplicación; no probar técnicas contra sistemas, redes o cuentas reales.
- Usar nombres, documentos, cuentas, tarjetas y correos ficticios.
- Representar entidades ficticias: BANCO ANDINO, Banco Corresponsal Internacional y Banco Pacífico Insular, sin logos ni colores de entidades reales.
- Crear personajes ficticios sin semejanza con docentes, autoridades o personas reales del caso.
- Presentar a Lazarus como un grupo criminal, sin caricaturas ni burlas hacia pueblos o nacionalidades.
- Acreditar autor y licencia de cada recurso de terceros e indicar las herramientas de IA utilizadas.

El HTML establece que incumplir cualquiera de estas reglas anula la nota del equipo y cita las leyes peruanas 30096 y 29733 como parte de su marco académico.

## 12. Evaluación

| Criterio | Puntos | Resultado esperado |
| --- | ---: | --- |
| Funcionamiento técnico | 4 | APK sin errores en un celular real y cumplimiento de los requisitos del escenario. |
| Fidelidad al caso | 3 | Hechos, fechas y cifras coherentes con la línea de tiempo. |
| Análisis de la vulnerabilidad | 3 | Explicación técnica de la falla y de por qué permitió el ataque. |
| Contramedida simulada | 3 | Controles correctos y resultado distinto y coherente en defensa. |
| Interacción y experiencia | 2 | Experiencia comprensible sin ayuda, con tensión, ritmo y recompensa. |
| Arte y sonido | 2 | Estilo coherente y créditos completos. |
| Trabajo en equipo | 1 | Commits de todos y roles cumplidos según la ficha. |
| Presentación final | 2 | Demostración sin fallas, dentro del tiempo e hipótesis sustentada. |
| **Total** | **20** | |

Los niveles de la rúbrica son: Logrado (100 %), En proceso (70 %), Inicio (40 %) y No presenta (0 %).

## 13. Decisiones pendientes de concretar

La fuente no define los siguientes detalles, que deben resolverse durante la planificación e implementación:

- NRC del equipo, nombres de integrantes y distribución nominal de roles.
- Fecha de entrega, calendario de sprints y duración real del reloj acelerado.
- Distribución de puntos de teletransporte e inventario definitivo de los cinco o más objetos interactivos.
- Secuencia de activación de controles y forma de representar el intento de acceso después de activarlos.
- Comportamiento de combinaciones parciales de defensas; la comparación obligatoria corresponde al ataque y a la defensa aplicada.
- Modelo de datos, textos definitivos del log y representación de la protección de registros.
- Configuración de entrada para XR Interaction Simulator y metas cuantitativas de rendimiento.
- Dispositivos Android para la validación de entrega indicada por el HTML.

Estas decisiones deben preservar los cinco requisitos funcionales, los dos criterios de aceptación y el resultado obligatorio de **36 órdenes frente a 0**.
