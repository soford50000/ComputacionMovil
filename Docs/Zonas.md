# Viajes entre Amazonas y San Andrés

El icono de mapa a la derecha del saldo abre dos destinos: Amazonas y San Andrés — Caribe Insular. Usa el PNG proporcionado por el usuario, tintado en dorado mediante el shader de siluetas existente. Su margen transparente se recorta con coordenadas UV; el archivo original no se modifica.

El saldo tiene 130 × 58 unidades de Canvas como tamaño mínimo y conserva la fuente de 22 puntos. Su ancho aumenta para importes largos sin reducir el texto. El mapa mide 60 × 58 y tiene un espacio de 12 unidades respecto al saldo. Ambos se ocultan durante la pesca y mientras hay una captura pendiente de recoger.

`ZonaSelectorUI` se añade desde `EconomiaManager` y comparte su Canvas. El selector usa fotografías, fondos nogal, vegetación y el cierre con X de la interfaz. Las tarjetas se apilan en vertical. `FisheryManager.CambiarZona` valida el destino y permite viajar solo en el estado listo; emite `ZonaCambiada` para actualizar el fondo, el catálogo y el indicador del lugar.

La foto suministrada se conserva en `Assets/UI/SanAndres.jpg`. `Assets/Resources/Zonas/SanAndres.asset` contiene la zona y su referencia a la fotografía. Amazonas conserva su asset y sus 25 especies. Al volver a Amazonas se restauran su fondo, su catálogo y la pesca; el saldo, las compras y los descubrimientos no se reinician. Cada sesión comienza en Amazonas.

San Andrés tiene un primer lote de siete especies registradas con nombre e imagen: sábalo atlántico, mero guasa, dorado de mar, raya águila jaspeada, ladyfish, carite lucio y gran barracuda. Sus assets están en `Assets/Data/Peces/SanAndres/` y las imágenes originales en `Assets/Sprites/Peces/SanAndres/`. La segunda imagen del carite no crea una especie duplicada.

El segundo lote añade ocho especies: pez vela, morena verde, peto, pez loro, palometa pompano, pargo de manglar, pez ballesta y anchoa de banco. El total registrado es de quince especies; los nombres se toman de los archivos proporcionados, sin asignar todavía nombres científicos ni características.

Por decisión del usuario, las características se revisarán después de recibir el resto de las especies. Los quince assets tienen `caracteristicasPendientes = true`: aparecen como siluetas en el catálogo con la indicación «Próximamente», pero no participan en el sorteo de capturas. La pesca continúa deshabilitada mientras no haya ninguna especie configurada. Los valores iniciales del ScriptableObject no representan un balance aprobado. Para habilitar cada especie se deben definir sus características y desmarcar el campo de pendientes; las especies existentes de Amazonas conservan su comportamiento.

`FondoAguaUI` conserva el recorte centrado que cubre la pantalla sin deformar la foto y cambia la textura al viajar. San Andrés utiliza `Pesca/MarCaribe`, descrito en `Docs/AguaCaribe.md`, con una máscara que termina bajo la playa. Amazonas conserva `Pesca/AguaTranquila`.

La validación `ValidarMenusExpedicion.Ejecutar` incluye viaje de ida y vuelta, fondo correcto, quince especies distintas con sprites y sin peces ajenos, bloqueo de captura de especies pendientes, conservación del saldo y la fuente, y bloqueo del viaje durante espera y forcejeo. Genera capturas horizontal/vertical del mapa y de San Andrés. Se ejecuta en batchmode con el Editor de ese proyecto cerrado; para validar mientras el proyecto principal está abierto se usa una copia independiente y un `productName` distinto para aislar las preferencias del jugador.

Validado el 18 de octubre de 2026 con Unity 6000.5.10f1: compilación y prueba en Play Mode completadas con código de salida 0. Registro en `Logs/zonas-qa-verificado.log` y capturas en `Logs/ZonasQA/`.

El primer lote de siete especies se importó y validó con el mismo Editor, también con salida 0. Registro en `Logs/san-andres-especies-qa.log`; captura del catálogo actualizado en `Logs/ZonasQA/20-coleccion-san-andres.png`.

Segundo lote validado en Unity con salida 0: quince fichas distintas con sprites, sin capturas habilitadas ni especies de Amazonas mezcladas. Registro en `Logs/san-andres-lote2-qa.log`. Las ocho imágenes conservan exactamente los archivos originales entregados.
