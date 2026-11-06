# Animación procedural de la caña

La configuración está en **Pesca Amazonas > CanaVisual** en `MainScene`.
No hay referencias nuevas que arrastrar: `TensionSystem` encuentra `CanaVisual`
en el mismo objeto; este crea y configura `CanaGrafico` al iniciar.

Valores iniciales: `curvaMaxima = 1.35` radianes, `rigidez = 100` (masa unitaria),
`amortiguacion = 0.55` (razón de amortiguación), `inicioFlexion = 0.25`,
`potenciaFlexion = 2`, `caidaGravedad = 0.045` radianes,
`intensidadVibracion = 0.002` de la altura y `frecuenciaVibracion = 12` Hz.
La caña equipada conserva su factor visual de resistencia.

El resorte analítico usa `Time.deltaTime`, admite sobrepaso y rebote y recibe
los tirones mediante `CanaVisual.AplicarTiron(intensidad)` (0–1). `EfectoTiron`
se conserva como alias. La tensión de juego no se modifica. La vibración se
activa solo por encima del límite superior real de `TensionSystem` (0.83).

`CanaGrafico` conserva sus 96 tramos y 97 puntos; cachea además los 96 pesos
de flexión y solo los recalcula al cambiar los parámetros. Las anillas y el
sedal usan la misma curva. Los 24 segmentos exteriores forman una parábola
Bézier con ambos extremos anclados, aproximación ligera a una catenaria;
su caída es proporcional a `(1 - tensionVisual)^2`. No hay Rigidbody, Joint,
objetos por segmento ni nuevas colecciones en el bucle de animación. Los
buffers internos de uGUI pueden crecer durante el primer llenado de la malla.

`depurarAnimacion` muestra el eje, la salida del sedal y el ancla, sin escribir
logs por fotograma. En Play, modificar CanaVisual sirve para ajustar los
valores, pero hay que copiarlos fuera de Play para conservarlos.

Validación determinista: menú **Pesca > Validar animacion procedural de cana**.
Comprueba respuesta a 30/60/120 FPS, pausa, frames largos, impulso y rebote,
longitud, base rígida, extremos y holgura del sedal; también ejecuta la
regresión existente del ancla. La prueba antigua `ValidarCanaVisual` contiene
expectativas de la animación anterior (parte baja inmóvil hasta 0.6 y mínima
oscilación); no representa los nuevos criterios de flexión.

Revisión visual recomendada en móvil: mantener/soltar durante el forcejeo,
observar un tirón, superar 0.83, terminar la captura y repetir lanzamiento.
Perfilar CPU y GC en dispositivo; las pruebas de geometría no sustituyen
una medición de rendimiento en hardware móvil.
