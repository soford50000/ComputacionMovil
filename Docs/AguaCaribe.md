# Agua de San Andrés

El fondo es una fotografía en un RawImage de uGUI. El shader `Assets/Resources/Agua/MarCaribe.shader` anima únicamente la superficie marina y conserva el encuadre. `FondoAguaUI` lo carga al entrar en San Andrés y restaura el shader original al volver a Amazonas.

La implementación combina cuatro ondas direccionales de diferentes longitudes y velocidades, distorsión proporcional a la distancia, flujo local mezclado en dos fases para ocultar reinicios y variaciones suaves de luminosidad. La máscara usa las coordenadas originales de la fotografía: su límite superior está en 0,478 desde abajo, según el ajuste proporcionado por el usuario. La región por encima del límite no se anima; el efecto se desvanece al acercarse a este. Los bordes de la foto también se atenúan para evitar estiramientos.

El componente conserva perfiles independientes, guardados en MainScene, con los valores de las capturas del usuario:

| Zona / shader | Intensidad | Velocidad | Límite | Reflejos |
| --- | --- | --- | --- | --- |
| Amazonas / AguaTranquila | 3,51 | 1,03 | 0,433 | — |
| San Andrés / MarCaribe | 2,6 | 1,52 | 0,478 | 0,188 |
| San Andrés / AguaTranquila (respaldo) | 3,94 | 1,48 | 0,478 | — |

Los parámetros generales de San Andrés corresponden al respaldo; el shader marino usa los controles de «Mar de San Andrés». Los límites ya no se sobrescriben al viajar, de modo que los ajustes del Inspector se conservan y vuelven a aplicarse al regresar a cada zona.

Intensidad cero muestra la imagen sin animación. No requiere texturas adicionales, paquetes ni cambios de render pipeline. El shader usa dos lecturas de la fotografía en la zona animada y una fuera de ella; no se ha medido rendimiento en dispositivos móviles.

Se trata de una animación de la fotografía, no de una simulación tridimensional. No puede generar geometría de olas, nueva información oculta bajo la superficie ni reflejos físicamente correctos. Los ajustes priorizan conservar la foto y evitar deformaciones exageradas.

Referencias técnicas consultadas; implementación propia adaptada al fondo y a uGUI:

- [Catlike Coding: Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/): flujo de textura en dos fases para ocultar reinicios.
- [NVIDIA GPU Gems, capítulo 1](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models): superposición de ondas direccionales y pendientes para iluminación.

Validación: compilación del shader, render de 192 fotogramas a 24 fps, comparación de píxeles de tierra y agua, y prueba de cambio de shader al viajar entre zonas. Registro: `Logs/agua-caribe-qa.log`.
