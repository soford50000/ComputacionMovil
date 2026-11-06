# Interfaz marina de San Andrés

San Andrés tiene una paleta propia para el catálogo, las fichas de especie, la ventana de captura, el mapa, el equipo, las carnadas y el saldo. Se aplica al viajar y restaura el estilo original al volver a Amazonas, incluso si los menús estaban cerrados.

| Elemento | Color |
| --- | --- |
| Texto principal y marcos oscuros | Azul marino `#103D59` |
| Papel y tarjetas descubiertas | Azul espuma `#E9F7FC` |
| Campos de medidas y selección | Celeste `#C6E4EF` |
| Especies por descubrir | Azul suave `#B0D6E5` |
| Botones secundarios | Turquesa profundo `#0A596E` |
| Acción principal y acentos | Turquesa claro `#A5E2EB` |
| Fondo del catálogo y mapa | Degradado `#D6F0F7` → `#B5DCEB`, con ondas sutiles |

En el catálogo de peces de San Andrés, las hojas se sustituyen por dos imágenes de corales transparentes (`DecoracionCatalogo/Coral1` y `Coral2`), distribuidas al 28 % de opacidad por el panel, incluidos cabecera, márgenes y pie. La capa permanece fija al desplazar la colección, se recorta al borde del panel y no intercepta clics. Las tarjetas opacas conservan la lectura de peces y textos. Las carnadas siguen ocultando la decoración vegetal en San Andrés. Las fotografías conservan sus colores; las rarezas mantienen sus familias de color con tonos más oscuros para facilitar la lectura. La cabecera identifica «SAN ANDRÉS · CARIBE» y la captura dice «Una historia más del mar».

## Integración

`TemaZonaUI` conserva los colores originales y los estados de los botones por Canvas. Escucha `FisheryManager.ZonaCambiada`, actualiza también controles inactivos y desconecta sus eventos al desactivarse. Los cambios de selección, compra y descubrimiento registran su color base para que los viajes posteriores conserven ese estado. No consulta la jerarquía en cada frame.

`FondoNogalCatalogo` reutiliza su textura para el acabado marino y recupera el nogal en Amazonas. `VegetacionCatalogo` reutiliza sus elementos y sustituye texturas, opacidad y giros al cambiar de zona, incluso sin cambios de tamaño. El catálogo habilita los corales mediante `mostrarCoralesEnSanAndres`; al regresar a Amazonas recupera las cuatro ramas originales. No se requiere configurar componentes a mano en la escena.

## Comprobaciones realizadas

- Compilación del código de juego y de Editor mediante el compilador y las referencias locales de Unity: sin errores. Las advertencias del código de juego corresponden a campos que asigna el Inspector.
- Cálculo de contraste de texto principal, secundario, botones y rarezas de la paleta: las combinaciones normales revisadas superan 4.5:1.
- Revisión del diff y de las suscripciones, actualización de controles ocultos y restauración del estilo original.

La integración de corales compila sin errores tanto en el código de juego como en el de Editor, con el compilador y las referencias locales de Unity. La validación existente de menús comprueba los dos corales en San Andrés, las cuatro ramas en Amazonas y que la decoración no intercepte clics ni scroll. No se pudo ejecutar esa prueba: Unity terminó con código 1 antes de producir el registro solicitado. Queda pendiente la comprobación visual en Play Mode: abrir el catálogo de San Andrés, desplazarlo, cambiar la orientación y regresar a Amazonas para comprobar la restauración de las hojas.
