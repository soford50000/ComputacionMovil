# Menús de expedición

Los menús de carnadas, cañas, colección y captura comparten una presentación de cuaderno de campo: papel crema, tinta verde y acentos dorados, sin franja lateral de madera.

## Ajustar el estilo

`Assets/Scripts/UI/EstiloExpedicion.cs` contiene la paleta, las cabeceras, los marcos, la tipografía y los estados de los botones. `EntradaExpedicion` realiza un fundido de 0,2 segundos con tiempo no escalado y cancela la animación al cerrar.

`DisposicionExpedicion` alterna las posiciones de las fichas entre horizontal y vertical. `CuadriculaExpedicion` ajusta las columnas de la colección al viewport. Los modales tienen su propio orden de dibujo y bloquean los accesos que quedan detrás.

Se mantiene la UI uGUI creada en tiempo de ejecución y sus controladores existentes. No requiere asignar objetos en el Inspector ni importar paquetes. Los textos existentes conservan `UnityEngine.UI.Text`; se evita una migración de componentes ajena al rediseño.

Toda la interfaz usa Rye Regular: títulos, información, botones, saldo y mensajes de pesca. `EstiloExpedicion.Fuente` carga `Assets/Resources/Fonts/Rye-Regular.ttf`, proporcionada por el usuario, y se comparte con los textos creados fuera de los menús. Su licencia se conserva en `Rye-OFL.txt`. Se usa el estilo normal de la fuente y se mantiene el ajuste automático de tamaño de cada texto.

## Comportamiento

El catálogo muestra el degradado de fondo liso, sin vetas horizontales ni línea separadora bajo la cabecera. Conserva la vegetación y la barra de progreso. Las carnadas mantienen sus vetas; `FondoNogalCatalogo.MostrarVetas` permite elegir el acabado por menú.

Los menús se cierran con una X sin fondo ni marco en la esquina superior derecha. `EstiloExpedicion.Cierre` comparte el control entre colección, ficha de especie, equipo, carnada y oro; mantiene un área de pulsación de 56 × 56 unidades del Canvas, escalada con la pantalla.

Los accesos a equipo, carnada y oro se ocultan al lanzar el anzuelo y permanecen ocultos durante la espera, el forcejeo y el resultado pendiente de recoger. Reaparecen al volver al estado listo, tras recoger el pez o fallar el intento. Sus ventanas se cierran al comenzar la pesca. El aviso de pique muestra solo «¡PICÓ!», sin revelar la especie ni mostrar una cuenta regresiva; el tiempo interno del forcejeo se conserva.

Los modales «Peces del Amazonas» y «Carnadas del río» comparten un acabado nogal: gradiente de 135° `#CDAC7B` → `#BE9A68`, veta con opacidad máxima 0,1 y sombra interior suave. `FondoNogalCatalogo` genera la superficie en el `Image` existente, sin añadir capas a la jerarquía ni modificar las tarjetas. Las casillas ocultas usan `#B38F5C` y siluetas uniformes `#8F6F42`; las descubiertas, `#F5E9D0`. El shader de siluetas conserva el alfa del pez y el recorte del viewport. Los textos principales usan `#2A1D10`; los secundarios, `#5A4326` sobre crema y `#332411` sobre nogal, ya que el primero solo da 3,09:1 sobre `#B38F5C`. La X de cierre usa tinta marrón y el progreso conserva el verde. La validación de paleta exige al menos 4,5:1 sobre la madera y las tarjetas, incluidos los estados de los botones; la composición final con vegetación requiere revisión visual.

El catálogo y las carnadas incorporan las cuatro imágenes originales del usuario en `Assets/Resources/DecoracionCatalogo`. `VegetacionCatalogo` distribuye ramas al 42 % de opacidad por todo el panel, incluidos cabecera, márgenes y pie, con tamaño, giro y espejo variables. La capa es el primer hijo del panel, queda debajo de textos, botones y tarjetas opacas, y permanece fija al desplazar las especies. Un `RectMask2D` recorta las ramas únicamente al límite exterior del panel. La distribución se recalcula al cambiar de tamaño u orientación, independientemente de la cantidad de tarjetas. `raycastTarget = false` y `CanvasGroup.blocksRaycasts = false` permiten clicar y arrastrar a través de la decoración.

- Carnadas: comparte el acabado `FondoNogalCatalogo`, la tinta marrón y el fondo vegetal de panel completo del catálogo. Las carnadas pendientes de compra usan nogal `#B38F5C`; las disponibles o seleccionadas y la ficha usan crema `#F5E9D0`. Mantiene la selección con borde dorado, botones verdes, estado de compra/equipado, ilustración destacada, tres ejemplos visuales de peces favorecidos y lista completa desplazable.
- Cañas: ilustraciones ampliadas, estadísticas separadas y marca de caña equipada.
- Colección: acceso con icono de cuaderno, contador y barra de descubrimientos, tarjetas de especies y ficha de campo. La cuadrícula se adapta al ancho real disponible.
- Captura: ilustración ampliada, ganancia destacada con «+$» y aviso «¡Nueva especie!» en texto verde, sin recuadro, únicamente al registrar el primer descubrimiento de una especie. No modifica las probabilidades, medidas ni recompensas.

Los importes visibles usan «$» en lugar de «oro»: saldo del HUD y su ventana, precios de cañas y carnadas, mensajes de saldo insuficiente y recompensa de captura. Las claves de guardado y los valores de la economía se conservan.

Los precios de las cañas comprables se aumentaron diez veces para alargar la progresión: Abu Garcia Vengeance, $3.500; Daiwa DSK25-B/F662M, $8.000; Shimano Colt Sniper Limited 100H, $15.000. La Shimano Fx inicial sigue gratis. Las compras ya guardadas se conservan. Este ajuste no modifica recompensas ni estadísticas: las cuatro cañas aún tienen poder provisional 1 y tolerancia 0, con estadísticas pendientes.

La tensión actual depende de `fuerzaPez`, el poder de la caña, la pulsación del jugador y los tirones aleatorios. No usa directamente rareza ni peso del ejemplar; este último se sortea en `CapturaResultado` después de resolver la captura. Rareza, peso y longitud sí intervienen en la recompensa. Aumentar precios no cambia esa mecánica.

Al soltar la pantalla, la velocidad base de bajada de tensión es 0,44 (antes 0,34: aproximadamente un 29 % más rápida). El valor está actualizado tanto en `TensionSystem` como en `MainScene`. Se mantiene la subida en 0,38, el rango seguro de 0,17 a 0,83 y el margen de 1,1 segundos continuos en rojo antes de perder. Volver al rango seguro reinicia ese margen; la bajada sigue siendo gradual y proporcional al tiempo transcurrido.

## Validación reproducible

Con el Editor del proyecto cerrado, ejecutar Unity con:

```text
-batchmode -projectPath "RUTA_DEL_PROYECTO" -executeMethod ValidarMenusExpedicion.Ejecutar -logFile "RUTA_DEL_PROYECTO/Logs/ExpedicionQA/unity-validacion.log"
```

La prueba entra en Play Mode, ejerce selección, compra, retirada, descubrimiento, recompensa y reapertura; guarda capturas en `Logs/ExpedicionQA`. Respalda las claves de progreso antes de probar y las restaura después de salir de Play Mode. También conserva un respaldo JSON en esa carpeta para recuperación si el proceso se interrumpe inesperadamente. No ejecutar simultáneamente con una partida del mismo proyecto.

Las capturas son renders del Canvas a 1280 × 720 y 720 × 1280. No sustituyen una prueba táctil en dispositivo ni una compilación Android/iOS.
