# Carnadas y aparición de peces

El botón CARNADA, debajo del equipo de cañas, abre ocho opciones. Seleccionar una
tarjeta muestra sus peces favorecidos; EQUIPAR confirma la elección. SIN CARNADA
permite quitarla. El juego comienza sin carnada salvo que se asigne una en el
Inspector. Solo se permite cambiar antes del lanzamiento.

Cada carnada multiplica por 2 el peso del sorteo de sus especies objetivo, una sola
vez por especie. La selección final normaliza todos los pesos. Por ejemplo, 10
boletos de 100 pasan a 20 de 110: 10 % pasa a 18,18 %, no a 20 %. No modifica
kilogramos, tamaño, fuerza ni recompensa de un mismo ejemplar. Otros peces siguen
pudiendo aparecer. Se conserva la firma anterior de FisheryManager para sus otros
consumidores; TensionSystem pasa el CeboData equipado al lanzar.

| Carnada | Amazonas | San Andrés |
| --- | --- | --- |
| Lombriz | Óscar | — |
| Grillo / saltamontes | Arawana plateada, óscar | — |
| Camarón | Raya motoro, óscar | Mero guasa, raya águila jaspeada, pargo de manglar, pez ballesta |
| Frutos del bosque | Pacu, cachama blanca, tambaquí | — |
| Pececillo vivo | Pez hoja, barracuda de cola roja, bicuda | Ladyfish, dorado de mar, gran barracuda, peto, carite lucio, pez vela |
| Pez de carnada entero | Tucunaré, payara, paiche, anguila eléctrica, bagre rayado, surubí, bagre tigre, dorado de río, manguruyú, bagre espátula, bagre de cabeza plana | Mero guasa, gran barracuda, dorado de mar, peto, carite lucio, pez vela |
| Trozos de pescado | Piraña roja, tiburón toro | Mero guasa, gran barracuda, morena verde, pargo de manglar |
| Engodo vegetal de fondo | Curimatá, jaraquí, pleco | Pez loro, palometa pompano, anchoa de banco, sábalo atlántico |

Las asociaciones de San Andrés son una primera configuración de jugabilidad
para reutilizar el inventario y el sistema RF11 en la zona nueva; pueden ajustarse
desde los `CeboData` sin cambios de código. Las carnadas sin especies asignadas
para esa zona conservan la probabilidad base de sus peces.

El engodo es una adaptación para el juego inspirada en la dieta detritívora,
no una eficacia de pesca demostrada para las tres especies. Pseudopimelodus sp.
y Acestrorhynchus sp. conservan las asignaciones generales de la propuesta,
pendientes de precisar la especie. Los multiplicadores son decisiones de balance.

## Datos e imágenes

`Assets/Resources/CatalogoCebos.asset` referencia los ocho `CeboData` de
`Assets/Data/Cebos`. Cada uno permite cambiar nombre, imagen, descripción,
multiplicador y especies desde el Inspector. Las imágenes están en
`Assets/Sprites/Cebos`; se copian las imágenes suministradas sin alterar su contenido.
`Lombriz.png` y `Camaron.png` utilizan las nuevas imágenes enviadas por el usuario,
conservando los GUID de sus sprites para mantener las referencias del catálogo.

La segunda entrega añade `PezEntero.png` (adjunto 1000446591) y `Trozos.png`
(adjunto 1000446590), ya asignados. El usuario indicó ignorar el pez plano
(1000446571). El engodo utiliza `Engodo.png`, la imagen de la bolsa Sonubaits
enviada por el usuario. Las ocho carnadas ya tienen imagen asignada.
El pececillo utiliza ahora `Pececillo.png`, la imagen nueva enviada por el usuario,
conservando la referencia del sprite anterior. Las tarjetas muestran imagen y
nombre, sin la etiqueta repetida «MAYOR APARICIÓN».

Por ahora no hay precio, consumo de unidades ni persistencia entre sesiones:
se implementa la selección y el efecto de aparición aprobados.

## Referencias de la selección aprobada

- Óscar y cebos documentados: https://myfwc.com/wildlifehabitats/profiles/freshwater/oscar/
- Arawana: https://ojs.ucp.edu.pe/index.php/cienciaamazonica/article/view/21
- Raya motoro: https://www.scielo.br/j/bjb/a/NCcpPShcvtYwPdSJHqQRLMs/
- Frutos: https://onlinelibrary.wiley.com/doi/10.1111/j.1744-7429.2008.00415.x
- Bagres: https://www.scielo.br/j/aa/a/9ydHfsvsKvzg9DwpdqX6vPD/
- Detritivoría: https://onlinelibrary.wiley.com/doi/abs/10.1111/j.1095-8649.1998.tb00961.x

## Validación

Unity: `-batchmode -executeMethod ValidarCebos.Ejecutar`.
Comprueba catálogo, ocho sprites, cobertura de 25 peces, 180 000 sorteos con
semilla fija (sin cebo y ocho cebos), conservación de medidas y fuerza, controles
de equipar/quitar y bloqueo durante espera/combate. Capturas en
`Logs/CebosQA/*.png`; resultado en `Logs/cebos-qa.log`.
