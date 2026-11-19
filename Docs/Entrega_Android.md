# ANDES-SIM · Android táctil

La escena DataCenter_VR abre con un menú para comenzar, configurar o salir. Durante el entrenamiento, el objetivo activo está arriba a la izquierda y se actualiza al validar cada paso. Las evidencias guardadas salen de la lista de pendientes.

## Controles

- Arrastra un dedo sobre la sala para orientar la cámara. Toca un equipo o destino de teletransporte y suelta para seleccionarlo. En el Editor, el ratón funciona igual.
- En **Ajustes**, se puede cambiar a **Giroscopio**. Mueve el teléfono para mirar y usa **Recentrar cámara** para tomar la orientación actual como referencia. Si falta el sensor, el juego mantiene el arrastre. También se puede ajustar el volumen.
- **Llévame al objetivo** lleva a la estación correspondiente. El marcador verde indica el equipo que debes tocar.
- Elliot muestra una sección breve a la vez. Si quedan más, **Continuar** registra **Ok** y entrega la siguiente con animación y sonido. Al terminar aparece la acción del objetivo.
- **Menú** y **Ajustes** pausan el recorrido. **Menú → Historial** permite releer la conversación de la sesión.
- La terminal se abre encima del diálogo como una pantalla con un comando de solo lectura y la salida de la consulta. **Ejecutar** se habilita cuando Elliot entrega la instrucción. **Continuar** cierra la pantalla después de revisar el resultado. La pausa conserva el punto de la consulta.

## Compilar y verificar

Usa **Andes-Sim > Android > Preparar y generar APK**. El APK queda en `Build/ANDES-SIM.apk` y su informe en `Build/BuildResult.json`, ambos en la raíz del proyecto. La configuración usa Android horizontal, ARM64, IL2CPP, OpenGLES3 y API mínima 26. El juego utiliza la pantalla del teléfono.

El menú **Andes-Sim > Android > Validar diálogo y recorrido táctil** usa entradas sintéticas del Input System en el Editor. Deja el informe y las capturas 16:9 y 20:9 en `Build/Validacion_Movil/`. Comprueba el arrastre, el toque, las secciones y el bloqueo de **Ejecutar**, la pausa y el reinicio durante una consulta y el cierre de **36 órdenes frente a 0**.

Los archivos de `Docs/Evidencias_Android/` corresponden a la primera entrega móvil. Los informes del APK vigente, su huella y la comprobación de la firma se guardan en `Build/`.

Consulta `Build/BuildResult.json` para el resultado de Unity, `Build/APK_Signature.txt` para la firma y `Build/APK_Metadata.txt` para el paquete, la arquitectura y los niveles de API del APK.

La terminal usa Roboto Mono. La licencia que acompaña a la fuente está en `Assets/AndesSim/Art/Resources/RobotoMono-LICENSE.txt`.

## Retrato

La ilustración de Elliot se generó con la herramienta integrada de OpenAI. El juego la muestra en un círculo a la izquierda del diálogo. [Archivo del retrato](../Assets/AndesSim/Art/Resources/ElliotPortrait.png)

> Create a single original square digital illustration for a tiny circular dialogue avatar in the Unity game ANDES-SIM. Subject: Elliot Alderson as portrayed by Rami Malek in the television series Mr. Robot, a quiet serious young cybersecurity engineer in a black hoodie. Painterly graphic novel portrait with clean restrained detail, high readability at 80 px, centered head and shoulders with whole face comfortably inside the central 68% square, head slightly turned toward the viewer, calm attentive expression. Soft cool navy background with a faint teal rim light, natural subdued skin tones, face bright and easy to distinguish. Full bleed square, no frame, no border, no text, no logo. Original artwork, not an existing still or screenshot.
