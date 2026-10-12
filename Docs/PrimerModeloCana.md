# Abu Garcia Vengeance — primer modelo de caña

Nombre confirmado: **Abu Garcia Vengeance**. Será la **segunda caña más potente del juego**. Esta posición es una decisión de diseño pendiente de balance: sus estadísticas se definirán después.

La identidad reemplaza la caña de bambú provisional que estaba equipada en la escena. El asset `Assets/Data/Canas/Cana_AbuGarciaVengeance.asset` conserva su GUID para mantener las referencias del selector y de la escena. Los valores numéricos existentes siguen siendo provisionales y no representan su potencia definitiva; no se han modificado en este cambio.

Modelo visual de grafito basado en las cuatro referencias de producto: cuerpo gris con bloques oscuros, empuñadura EVA negra, asiento con gatillo, ataduras cobrizas y siete anillas huecas decrecientes. La composición en primera persona toma las dos referencias de uso.

## Uso

Abrir `Assets/Scenes/MainScene.unity` y pulsar Play. `CanaVisual` ya está agregado a «Pesca Amazonas». Pulsar **LANZAR**: la caña permanece recta durante la espera; al pique comienza a doblarse. Mantener pulsada la zona inferior aumenta la carga visual. Al soltar sigue respondiendo al pez con menor carga; al finalizar vuelve suavemente al reposo.

Es una malla procedural 2D sombreada para el fondo y renderer 2D actuales, no un archivo 3D importado. Se dibuja entre el agua y el HUD, no recibe raycasts y no necesita imágenes ni materiales adicionales. El mango permanece rígido y la deformación crece hacia la punta. Las anillas y el hilo comparten la curva del cuerpo.

En el Inspector de `CanaVisual` se pueden ajustar base y punta en coordenadas normalizadas de pantalla, grosor, flexión máxima, suavizado, amortiguación y amplitud de vibración. La base queda parcialmente fuera de pantalla como en las referencias.

El combate mantiene una pose fija con arco hacia la izquierda. Solo se interpola la pose al entrar y salir del combate. Durante la pelea, los dos primeros tercios permanecen inmóviles y el último tercio recibe microsacudidas verticales y pequeños rebotes de un resorte amortiguado. Las variaciones de tensión y la pulsación del jugador afectan únicamente a esa deformación local. El sedal permanece tenso y tiembla ligeramente, con entrada al agua fija, ondas elípticas y gotas discretas. La cámara y el fondo conservan su encuadre. Al terminar desaparecen los efectos y la caña vuelve al reposo.

Este ajuste sigue la descripción de movimiento facilitada por el usuario; el enlace de YouTube no pudo cargarse para inspeccionarlo directamente.

Las anillas están en la cara inferior cóncava. El hilo pasa por sus centros y sale de la punta hacia abajo y a la izquierda, sin comba. El punto de entrada al agua se calcula bajo la punta de la pose base y permanece estable ante sus microsacudidas; las ondas y gotas usan ese mismo punto. La flexión crece progresivamente hacia el extremo fino, dejando la sección gruesa más rígida. Los pequeños tirones desplazan el último tercio hacia abajo y a la izquierda y el resorte permite su retorno.

## Integración futura

Las estadísticas y el cálculo de captura no se han modificado. Abu Garcia tiene su apariencia propia; Shimano Colt Sniper Limited 100H usa una segunda apariencia con las mismas físicas. Equipamiento incluye también Shimano Fx como la caña más básica; las cañas de ejemplo Fibra y Carbono Pro fueron retiradas. Los tirones son una simulación de presentación, no una nueva física de tensión.

RF4 podrá llamar a `CanaVisual.EstablecerTension(valor)` con un valor de 0 a 1 durante el forcejeo. Este valor controla la carga local de la punta; la pose general de combate permanece fija incluso con carga cero. `UsarTironesSimulados()` restaura la animación provisional. Fuera del forcejeo la caña vuelve a su pose recta.

## Verificación en Unity

Con el editor cerrado, ejecutar Unity con `-batchmode -projectPath "ruta del proyecto" -executeMethod ValidarCanaVisual.Ejecutar -logFile "ruta del log"` (sin `-quit`: el verificador cierra Unity al terminar).

La prueba abre la escena y comprueba reposo, espera, pique, máxima carga, descarga, tirones simulados, recuperación y vértices válidos. Guarda capturas en `Logs/CanaQA/` y termina con código 0 al pasar, o 1 al fallar. Los cambios de cámara para las capturas son temporales y no se guardan en la escena.
