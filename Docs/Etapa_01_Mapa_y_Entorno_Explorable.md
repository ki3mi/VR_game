# Etapa 1 de 3: mapa y entorno explorable

## Propósito y resultado demostrable

Construir el centro de datos como un espacio VR que pueda recorrerse e inspeccionarse con **XR Interaction Simulator en el Editor de Unity**.

**Al terminar:** una persona entra por el portal, explora con movimiento libre en el simulador, visita todas las estaciones mediante teletransporte y selecciona al menos cinco objetos distintos, obteniendo una respuesta visible. Esta versión constituye la base jugable de las siguientes etapas.

**Referencia:** [Contexto del proyecto](C:/Games/grupo03/Docs/Contexto_Proyecto_Escenario_3_Centro_de_Datos.md).

Los mínimos del escenario proceden del contexto y del HTML. La distribución de estaciones, los seis puntos de teletransporte y las verificaciones de esta etapa son decisiones de este plan para medir el avance. Las casillas reflejan el avance verificado de la implementación.

## Requisitos que cubre

| Referencia | Cobertura de esta etapa |
| --- | --- |
| RF-01 | Pasillo con cuatro racks, terminal SWIFT, switch e impresora. |
| Requisitos generales VR | Portal, teletransporte, cinco objetos interactivos, retícula y sonido ambiental. |
| CA-01 | Teletransporte disponible para el recorrido previsto en VR. Por actualización del equipo, el simulador del Editor también permite movimiento libre. La comodidad en dispositivo pertenece a la validación final. |
| Entorno de pruebas del equipo | Recorrido e interacciones ejecutables con XR Interaction Simulator. |

El reloj, el registro de evidencias y la simulación de órdenes se implementan en la etapa 2. El panel tendrá su lugar y una respuesta de inspección en esta etapa; la activación de defensas corresponde a la etapa 3.

## Dependencias y preparación

- Usar el proyecto Unity existente. En la revisión inicial contiene Unity `6000.6.0f1`, XR Interaction Toolkit `3.6.0`, escenas base y recursos de la plantilla VR.
- Revisar los recursos disponibles y reutilizar los que sirvan para la navegación y la interacción.
- Crear la escena `DataCenter_VR`, conservando las escenas base como referencia.
- Separar el entorno, la navegación, los objetos interactivos, la interfaz y el audio para poder ampliarlos después.
- Registrar autor y licencia desde que se incorpore cada recurso externo.

## Mapa propuesto

El recorrido parte de una pequeña zona de entrada con portal, continúa por el pasillo de cuatro racks y permite llegar a la terminal, al switch, a la impresora y al panel de controles. Todas las estaciones deben tener una posición desde la que se puedan inspeccionar sus objetos.

| Zona | Contenido | Punto de teletransporte propuesto |
| --- | --- | --- |
| Entrada | Portal, nombre del escenario e instrucciones breves. | `TP_Entrada` |
| Pasillo | `Rack_01`, `Rack_02`, `Rack_03`, `Rack_04`. | `TP_Racks` |
| Operación | `Terminal_SWIFT` y espacio reservado para el visor del log. | `TP_Terminal` |
| Red | `Switch_Barato`, accesible para inspección. | `TP_Switch` |
| Confirmaciones | `Impresora`, inicialmente apagada. | `TP_Impresora` |
| Seguridad | `Panel_Controles`, con espacio para tres interruptores. | `TP_Controles` |

La escala y las dimensiones se ajustarán al probar el recorrido. Los puntos deben quedar fuera de muebles y paredes y permitir continuar hacia el siguiente destino y volver a la entrada.

## Tareas de implementación

### 1. Crear la estructura espacial

- [x] Crear `DataCenter_VR` con suelo, paredes, techo y pasillo.
- [x] Colocar exactamente cuatro racks y los objetos obligatorios de RF-01.
- [x] Dar nombre estable a los objetos que usarán las etapas siguientes.
- [x] Añadir colisiones para delimitar el espacio y evitar destinos dentro de la geometría.
- [x] Incorporar señalización para localizar entrada, terminal y panel de controles.

Se pueden utilizar formas simples o modelos provisionales, siempre que cada elemento sea reconocible. El mapa debe poder probarse antes del acabado visual.

### 2. Habilitar navegación con el simulador

- [x] Configurar el origen XR y XR Interaction Simulator para probar la escena en Play Mode.
- [x] Establecer la posición inicial en la zona de entrada y hacer que el portal lleve al centro de datos.
- [x] Implementar los seis destinos de teletransporte de la tabla.
- [x] Permitir apuntar y seleccionar los destinos desde la mirada simulada para comprobar el flujo previsto por el contexto.
- [x] Habilitar movimiento libre con WASD y E/Q en XR Interaction Simulator, conservando los destinos de teletransporte, según la actualización del equipo.
- [x] Mostrar instrucciones de los controles que realmente se hayan configurado para el simulador.

La adaptación a Cardboard debe conservarse como requisito de entrega. El uso de un controlador simulado puede facilitar la depuración, pero la prueba del recorrido debe comprobar también la selección desde la mirada.

### 3. Crear la inspección básica

Cada objeto de esta tabla debe resaltar o mostrar una indicación al apuntarlo y responder al seleccionarlo. La respuesta inicial puede ser una ficha breve; su lógica educativa se amplía en las etapas 2 y 3.

| Objeto | Respuesta mínima medible |
| --- | --- |
| `Terminal_SWIFT` | Mostrar identificación de la terminal y un panel de inspección. |
| `Switch_Barato` | Mostrar su ficha de inspección. |
| `Impresora` | Indicar que está apagada. |
| `Rack_01` | Mostrar su identificación y función en el entorno. |
| `Panel_Controles` | Mostrar los nombres MFA, alerta fuera de horario y SIEM. |

- [x] Configurar detección, selección y respuesta para los cinco objetos.
- [x] Incorporar la capa `Interactive` indicada por el contexto y comprobar los filtros usados por las interacciones.
- [x] Permitir cerrar una ficha y continuar el recorrido.
- [x] Mantener la lógica de inspección separada de la futura lógica de evidencias y defensas.

### 4. Establecer ambiente y legibilidad

- [x] Aplicar iluminación baja sin ocultar rutas, objetos ni textos.
- [x] Añadir LEDs a los racks y el comportamiento visual previsto para `LedBlink`.
- [x] Incorporar sonido ambiental de servidores con volumen que permita escuchar efectos posteriores.
- [x] Mantener coherencia visual con un centro de datos del BANCO ANDINO y recursos acreditados.

## Pruebas y criterios de aceptación

Todas las pruebas funcionales de esta tabla se ejecutan con **XR Interaction Simulator**.

| ID | Procedimiento | Resultado esperado |
| --- | --- | --- |
| E1-P01 | Abrir `DataCenter_VR` e iniciar Play Mode. | Se inicia en la entrada y se puede orientar la vista e interactuar. |
| E1-P02 | Usar el portal y visitar las seis posiciones propuestas. | Se alcanzan las seis posiciones y se puede regresar a la entrada. |
| E1-P03 | Probar movimiento libre con WASD y E/Q y completar desplazamientos mediante teletransporte. | La cámara responde a las entradas del simulador y los destinos siguen funcionando. |
| E1-P04 | Inspeccionar el inventario del escenario. | Se identifican los cuatro racks, la terminal, el switch y la impresora. |
| E1-P05 | Apuntar y seleccionar cada objeto de la tabla de inspección. | Cinco objetos distintos ofrecen indicación de apuntado y respuesta de selección. |
| E1-P06 | Leer las fichas desde sus posiciones de inspección. | Los textos se pueden leer y cerrar sin atravesar objetos ni perder acceso a la navegación. |
| E1-P07 | Entrar, recorrer y salir de Play Mode. | No aparecen errores de ejecución atribuibles a la escena. |

## Entregables de la etapa

- Escena `DataCenter_VR` guardada, con entorno, portal, navegación, cinco inspecciones y ambiente básico.
- Prefabs y recursos reutilizables del mapa, con nombres consistentes.
- Registro de resultados E1-P01 a E1-P07, incluyendo fecha, responsable, resultado observado y captura o referencia a video.
- Video corto del recorrido en el simulador, mostrando portal, destinos y las cinco interacciones.
- Relación inicial de recursos y créditos, y commits de las contribuciones realizadas.

## Condición para avanzar

La etapa se considera terminada cuando **E1-P01 a E1-P07 cumplen sus resultados esperados** y están guardadas las evidencias. Las fichas provisionales de inspección deben quedar identificadas para reemplazarlas o ampliarlas en la siguiente etapa.

**Continuación:** [Etapa 2: modo ataque y evidencias](C:/Games/grupo03/Docs/Etapa_02_Modo_Ataque_y_Evidencias.md).

