# Barra de tensión

El arco aparece durante el forcejeo, por encima de la caña, con el patrón completo
rojo → amarillo → verde → amarillo → rojo. La franja verde central representa la
zona estable; las transiciones son suaves y ambos extremos permanecen rojos.
El borde oscuro con acabado azulado mantiene el contraste sobre el paisaje.

`CanaVisual` crea `BarraTensionGrafico` automáticamente y mueve su indicador usando
la tensión visual existente. La barra no recibe clics ni toques. Su base se calcula
por encima del punto más alto de toda la caña, con margen para las anillas; se adapta
al espacio disponible sin invadir el mensaje de pique. Durante la transición inicial
espera a que la caña baje lo suficiente para mostrar el arco con un tamaño legible.
Se oculta al terminar el forcejeo.

Esta etapa es de presentación: la zona verde aún no interviene en el cálculo de
captura RF5, que conserva su medida actual de tiempo pulsado. El marcador también
acepta la tensión externa que ya expone `CanaVisual.EstablecerTension`.

Validación en Unity: `-batchmode -executeMethod ValidarCanaVisual.Ejecutar`.
Comprueba visibilidad, separación de la caña, límites de pantalla y controles sin
bloqueos, con capturas de combate a 1280×720, 1024×768, 1920×1080 y 720×1280 en
`Logs/CanaQA`.
