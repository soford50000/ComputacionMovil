# Sonidos de interfaz y pesca

`SonidosJuego` se añade a la instancia principal de `FisheryManager`. Sus dos fuentes 2D, bajo `Efectos de sonido`, reproducen la interfaz al 40 % y las acciones al 65 %, separadas de la música de los mapas.

- Los botones de menús, equipo, catálogo, viajes, saldo y cierre emiten un clic de madera suave. `EstiloExpedicion.Boton` registra el sonido una sola vez aunque se vuelva a aplicar el estilo. El acceso al saldo lo registra explícitamente.
- Lanzar reproduce un roce de línea y una salpicadura cuando el lanzamiento se acepta, incluso al usar el respaldo táctil de entrada. No añade otro clic encima.
- El pique emite dos notas cortas; al presionar durante el forcejeo suena un breve carrete, una vez por pulsación.
- Una captura conseguida emite tres notas ascendentes; recoger añade un tintineo y un escape, un tono descendente.

Los sonidos se conectan a los eventos de pesca y resultado, sin cambiar las reglas ni las recompensas. Las suscripciones se retiran al desactivar el componente. Los clics y tirones usan un intervalo mínimo con tiempo real para evitar acumulación al pulsar muy rápido; mantener la pulsación no repite el efecto cada fotograma. Los botones deshabilitados no generan `onClick` y no suenan.

Los siete WAV mono originales están en `Assets/Resources/Audio/Efectos/`, con importación PCM, precarga y descompresión al cargar para efectos cortos. Se cargan una vez al inicializar el componente. `BandaSonora/crear_efectos.py` los regenera con NumPy y `BandaSonora/verificacion_efectos.json` registra duración y niveles. Son sonidos sintetizados, sin muestras externas.
