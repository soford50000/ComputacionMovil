# Economía de capturas — propuesta inicial implementada

Cada captura exitosa genera sus medidas una sola vez y calcula `Oro` como entero.
La ventana y el evento `CapturaRecogida` utilizan ese mismo resultado inmutable.
Un escape no genera recompensa. Al recoger la captura, el monedero acredita
el premio una sola vez y guarda el saldo mediante `EconomiaManager`.

## Cálculo

```text
P = (peso - pesoMínimo) / (pesoMáximo - pesoMínimo)
L = (longitud - longitudMínima) / (longitudMáxima - longitudMínima)
D = 1 + 0.04 × (fuerza - 1)
E = 0.70 + 0.60 × (0.70 × P + 0.30 × L)
monedas = redondear(baseRareza × D × E)
```

Bases: común 40, poco común 75, rara 140, épica 260, legendaria 500.
Fuerza limitada a 1–10; dificultad entre 1 y 1.36. Peso aporta el 70 %
de la variación del ejemplar y longitud el 30 %. El multiplicador del ejemplar
va de 0.70 a 1.30. Una medida fija aporta 0.5 a su componente.
Se rechazan rangos inválidos, valores no finitos y medidas fuera del rango.
El redondeo se hace una sola vez al entero más cercano, con mitades hacia arriba.

La comparación es relativa a la especie: un pez hoja grande puede ser valioso
sin competir en kilos con un paiche. La fuerza añade un premio moderado; el
equipo no reduce el precio del pez al facilitar su captura. No hay un sorteo
adicional de dinero: iguales medidas, especie y configuración dan igual premio.
Por el redondeo, dos ejemplares ligeramente diferentes pueden valer lo mismo.

Se conserva el sorteo uniforme e independiente de peso y longitud existente.
Cambiar su distribución o correlacionarlos requerirá revisar este balance.

## Recompensas de los 25 peces de Amazonas

Centro significa ambas medidas en la mitad de su rango, no promedio redondeado
de todas las recompensas. Mínimo y máximo corresponden a ambas medidas extremas.

| Especie | Mínimo | Centro | Máximo |
| --- | ---: | ---: | ---: |
| Anguila eléctrica | 240 | 343 | 446 |
| Paiche | 476 | 680 | 884 |
| Arawana plateada | 118 | 168 | 218 |
| Bagre rayado amazónico | 233 | 333 | 433 |
| Barracuda de cola roja | 122 | 174 | 226 |
| Bicuda | 63 | 90 | 117 |
| Cachama blanca | 63 | 90 | 117 |
| Curimatá | 30 | 43 | 56 |
| Dorado de río | 476 | 680 | 884 |
| Bagre espátula | 233 | 333 | 433 |
| Bagre de cabeza plana | 63 | 90 | 117 |
| Surubí rayado | 233 | 333 | 433 |
| Pez hoja amazónico | 102 | 146 | 189 |
| Jaraquí | 30 | 43 | 56 |
| Manguruyú | 248 | 354 | 460 |
| Óscar | 59 | 84 | 109 |
| Pacu | 30 | 43 | 56 |
| Payara | 233 | 333 | 433 |
| Piraña roja | 31 | 45 | 58 |
| Pleco | 30 | 43 | 56 |
| Raya motoro | 233 | 333 | 433 |
| Tambaquí | 122 | 174 | 226 |
| Tiburón toro | 476 | 680 | 884 |
| Bagre tigre de río | 448 | 640 | 832 |
| Tucunaré | 122 | 174 | 226 |

## Recompensas de San Andrés

Las 15 especies usan la misma fórmula y el mismo flujo de cobro. Sus rangos,
fuerzas iniciales, descripciones y premios mínimo/centro/máximo están en
[Peces de San Andrés](PecesSanAndres.md). Dorado de mar, pez vela y raya águila
jaspeada conservan la rareza legendaria. No se introducen precios fijos por pez.

## Balance de ingresos y precios futuros de Amazonas

`FisheryManager` aplica pesos por especie, no porcentajes globales por rareza.
Con los 25 peces y pesos actuales 50/25/15/8/2, los pesos agregados son
250/100/75/56/8, total 489. Las probabilidades por rareza son aproximadamente
51.12 %, 20.45 %, 15.34 %, 11.45 % y 1.64 %. El sistema de selección no se modifica.

Con todos los intentos exitosos, sin cebo y medidas uniformes, el ingreso esperado
es aproximadamente **115.53 monedas por intento**. Es una media estadística;
los premios individuales siempre son enteros. Los comunes aportan ingresos
frecuentes y los legendarios conservan valor sin multiplicadores ilimitados.

Para ajustar precios con datos de juego:

```text
ingresoPorIntento = suma(probabilidadEspecie × éxitoEspecie × premioMedioEspecie)
ingresoPorMinuto = 60 × ingresoPorIntento / segundosMediosPorIntento
precioObjetivo = redondear(minutosObjetivo × ingresoNetoPorMinuto)
```

El tiempo debe incluir espera del pique, forcejeo, escapes y recogida. Restar
cebos u otros consumibles para obtener ingreso neto. Una tasa de éxito global
no basta si los peces difíciles escapan más. Como referencia simplificada,
con éxito uniforme del 70 % y ciclos de 11 segundos serían unas 441 monedas
por minuto antes de gastos y tiempo de interfaz; no es una medición del juego.
Una mejora de cinco minutos costaría unas 2205 monedas bajo ese supuesto.

Este es un balance inicial acotado, no una garantía de equilibrio completo:
ajustar precios, cebo, tasas de éxito y tiempos con partidas reales. Recalcular
si cambia el catálogo, las rarezas, sus pesos o la distribución de medidas.

## Verificación

`pwsh -File Tests/VerificarRecompensas.ps1` compila el cálculo C# de producción
con un DTO de datos mínimo, sin ejecutar Unity. Recorre 255025 combinaciones
de medidas de los assets y comprueba enteros, límites y monotonía en ambos ejes;
también verifica medidas fijas y rechazo de entradas inválidas. Imprime la tabla
y la media ponderada. Con `-Zona SanAndres` recorre los 15 assets marinos
(153015 combinaciones). La interacción de la ventana requiere prueba en Play.
Estas pruebas no se ejecutaron en la entrega de San Andrés: el usuario realizará
la verificación dentro del juego.
