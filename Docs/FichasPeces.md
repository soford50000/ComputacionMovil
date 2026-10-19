# Datos para el futuro catálogo

Cada uno de los 25 assets de `Assets/Data/Peces` contiene una descripción dirigida al jugador en `PezData.descripcion`, además de su nombre, imagen, hábitat y rangos de captura. Al implementar la selección del catálogo, leer esos campos del `PezData` seleccionado.

- `pesoMinimo` / `pesoMaximo`: kilogramos (kg).
- `longitudMinima` / `longitudMaxima`: centímetros (cm).
- Son rangos aproximados del juego, no mínimos y máximos biológicos verificados. Mostrar las medidas como «Rango de captura».
- La descripción no duplica cifras: el catálogo deberá leer los campos numéricos para evitar textos desactualizados.

La tabla recibida contiene 25 descripciones, sin valores numéricos. Se conservaron los rangos existentes. No se encontró un pez adicional sin ficha entre los 25 sprites, assets y referencias de la zona Amazonas. La interfaz del catálogo queda pendiente de desarrollo.

## Correspondencias y diferencias

Se adaptaron los textos a las identidades ya configuradas en el juego. Los nombres comunes no siempre designan la misma especie:

| Ficha | Identidad del proyecto | Diferencia con la tabla recibida |
| --- | --- | --- |
| Bagre rayado | Pseudoplatystoma punctifer | La tabla propone corruscans / fasciatum. |
| Barracuda cola roja | Acestrorhynchus sp. | La tabla propone otros géneros; la especie exacta sigue sin determinarse. |
| Bicuda | Boulengerella cuvieri | La tabla propone ocellata / maculata. |
| Cachama | Piaractus brachypomus | Cachama blanca; Colossoma macropomum ya está representado por el tambaquí. |
| Dorado de río | Brachyplatystoma rousseauxii | Es un bagre amazónico, distinto de Salminus brasiliensis. |
| Espátula | Sorubimichthys planiceps | Es un bagre sudamericano, distinto del filtrador norteamericano Polyodon spathula. |
| Gato cabeza plana | Pseudopimelodus sp. | Identificación provisional a nivel de género, distinta de Pylodictis olivaris. |
| Pacú | Mylossoma duriventre | Distinto de Piaractus mesopotamicus. |
| Payara | Hydrolycus scomberoides | La tabla propone Hydrolycus armatus. |
| Tigre de río | Pseudoplatystoma tigrinum | Es un bagre; la tabla describe Hoplias aimara. |

## Fuentes consultadas (9 de octubre de 2026)

- [Smithsonian: diversidad de anguilas eléctricas](https://www.si.edu/newsdesk/releases/smithsonian-scientists-triple-number-known-electric-eel-species). Los 860 V corresponden a Electrophorus voltai; se omitió esa cifra en la ficha de E. electricus.
- [WCS Perú: dorado](https://peru.wcs.org/es-es/especies/dorado.aspx) y [estudio de migraciones amazónicas](https://pmc.ncbi.nlm.nih.gov/articles/PMC5292968/). Respaldan la descripción del bagre dorado y sus migraciones.
- [USGS: Sorubimichthys planiceps](https://nas.er.usgs.gov/queries/factsheet.aspx?SpeciesID=2565). Referencia para el bagre espátula sudamericano.
- [FishBase: Pseudoplatystoma tigrinum](https://www.fishbase.se/summary/Pseudoplatystoma_tigrinum.html). Distribución, actividad nocturna y alimentación del bagre tigre.
- [Animal Diversity Web: Piaractus brachypomus](https://animaldiversity.org/accounts/Piaractus_brachypomus/). Alimentación de la cachama blanca.

Estas consultas complementan las descripciones recibidas; no constituyen una revisión biológica de todos los rangos numéricos existentes ni una identificación de las especies a partir de sus sprites.
