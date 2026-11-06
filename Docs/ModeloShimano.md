# Shimano Shore Jigging Rod Colt Sniper Limited 100H

Segunda caña modelada, prevista como **la más potente del juego**. Abu Garcia Vengeance queda en el segundo puesto. Las estadísticas y el precio definitivo se decidirán después: los valores de captura actuales son provisionales y no expresan ese orden de potencia.

## Modelo

Basado en las cuatro fotografías proporcionadas: cuerpo negro satinado, reflejos discretos, empuñadura trasera larga de EVA, empuñadura delantera alargada y afinada, talón ensanchado, portacarrete cilíndrico sin gatillo y collares plateados. Las anillas inferiores decrecen hacia la punta y tienen soportes metálicos dobles. No usa el patrón de cuadros ni los detalles cobrizos de la Abu Garcia.

## Uso e integración

En `MainScene`, abrir **EQUIPO** y elegir **Shimano Shore Jigging Rod Colt Sniper Limited 100H** antes de lanzar. El modelo cambia al equiparla; Abu Garcia continúa equipada por defecto. El selector muestra ambas cañas con una miniatura del modelo y sus estadísticas pendientes.

`CanaData.modeloVisual` elige la apariencia. `CanaGrafico` construye las dos variantes y `CanaVisual` sigue la caña equipada. Ambas comparten exactamente el mismo controlador, curva, pose, resorte, microsacudidas del último tercio y trayectoria del sedal hacia abajo e izquierda. El cambio de modelo no introduce multiplicadores de fuerza ni de animación.

La lista de equipo se carga desde `Assets/Resources/CatalogoCanas.asset` al iniciar: Shimano Fx, Daiwa DSK25-B/F662M, Abu Garcia Vengeance y Shimano Colt Sniper Limited 100H. También están asignadas en la escena. Esto evita que una lista antigua de la escena abierta o un prefab deje fuera a Shimano al entrar en Play. Si el juego ya estaba ejecutándose al actualizar los archivos, detener Play y volver a iniciarlo para reconstruir el menú.

`puestoPotenciaPrevisto` es solo información de diseño. `estadisticasPendientes` evita presentar los valores provisionales como estadísticas finales en el selector. Ninguno cambia el cálculo de captura.

## Verificación

El verificador `ValidarCanaVisual.Ejecutar` equipa Shimano, comprueba el cambio de apariencia y compara 97 puntos de ambas curvas bajo la misma carga y vibración. Después recorre pique, carga, recuperación y validación de malla, conservando las comprobaciones del sedal y las anillas. Genera capturas en `Logs/CanaQA` del proyecto usado en la prueba.
