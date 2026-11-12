# ANDES-SIM · Android táctil

La escena DataCenter_VR inicia con un menú para comenzar, configurar o salir. La configuración permite ajustar el volumen general y alternar entre cámara por joystick y giroscopio. Las preferencias se guardan en el dispositivo. El recorrido y sus explicaciones se conservan en el teléfono de Elliot.

## Controles

- Arrastra el joystick de la izquierda para orientar la cámara.
- En modo giroscopio, mueve el teléfono para mirar. **Recentrar cámara** toma la orientación actual como referencia. Si no hay sensor, el juego informa y mantiene el joystick.
- Toca directamente un equipo para inspeccionarlo, o céntralo en la retícula y pulsa **Inspeccionar**.
- Desplázate tocando los destinos de teletransporte o usando **Llévame** en el chat.
- **Ajustes** y **Menú** pausan el recorrido; al continuar se conserva la tarea.
- No hay desplazamiento libre por teclado. El simulador XR está desactivado en la escena y el control de cámara se realiza con `MobileExperience`.
- La animación de escritura del chat dura 1,2 segundos por mensaje. Las secuencias **Siguiente → Ok** y el historial siguen disponibles.

## Compilar

Menú del Editor: **Andes-Sim > Android > Preparar y generar APK**. Genera `Build/ANDES-SIM.apk` y `Build/BuildResult.json`. Configura ARM64, IL2CPP, OpenGLES3, orientación horizontal y API mínima 26. La versión es monoscópica táctil y no requiere un visor XR. Los proveedores XR de Android se desactivan para esta entrega.

## Verificación realizada

Se revisaron el arranque desde el menú, la apertura de ajustes, el cambio de volumen (valor observado 0,42), el arranque del entrenamiento y la ausencia del simulador de movimiento libre activo. En el Editor sin sensor se comprobó la alternativa automática al joystick. Una entrada sintética mediante los handlers de puntero del joystick produjo giro de cámara; el resultado está en `Docs/Evidencias_Android/JoystickCheck.json`.

Las capturas del menú, los ajustes y el juego están en `Docs/Evidencias_Android/`. Estas comprobaciones del Editor no sustituyen una prueba física multitáctil ni la calibración del giroscopio en un teléfono Android. No se ha realizado una prueba en un teléfono físico durante esta entrega.

Se verificó también que una entrada W/E/Q mediante Input System no desplaza la cámara (`KeyboardRemovedCheck.json`, distancia observada 0). El recorrido llegó a la terminal mediante Llévame y la selección por punto de pantalla abrió AccessQuery. Una comprobación del chat durante ajustes conservó el mensaje sin entregarlo y lo reanudó al volver (`PauseCheck.json`: ambos resultados verdaderos). Las acciones de menú y del recorrido se comprobaron mediante callbacks de botones y handlers del Editor; no se presentan como una prueba multitáctil sobre hardware.

El resultado de compilación y el tamaño real del APK se registran en `Build/BuildResult.json`.

## APK entregado

- Archivo: `Build/ANDES-SIM.apk`, **39.189.271 bytes** (39,2 MB).
- Resultado confirmado con el informe nativo de Unity: **Succeeded**, cero errores. Hay cuatro advertencias de TextMesh Pro: una directiva de shader obsoleta y tres avisos de distribución del código C++ generado.
- Firma comprobada con `apksigner verify`: válida, esquema APK v2, un firmante.
- Metadatos comprobados con `aapt`: paquete `com.andessim.datacenter`, versión 1.0, Android mínimo API 26, objetivo API 36 y arquitectura `arm64-v8a`. No exige un visor XR.
- Huella del archivo en `Build/SHA256.txt`; metadatos en `Docs/Evidencias_Android/APK_Metadata.txt`.

Para instalar, copia el APK a un teléfono Android ARM64 con Android 8 o posterior, ábrelo desde el administrador de archivos y completa el diálogo de instalación. Inicia en horizontal. Las opciones de cámara y volumen se encuentran en Configuración/Ajustes.

El campo `bytes` del informe de Unity corresponde a su tamaño total de construcción, no al tamaño comprimido del APK indicado arriba.
