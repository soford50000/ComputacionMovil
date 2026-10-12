using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// RF3: inicia el lanzamiento desde un botón, espera el pique y habilita la
/// mitad inferior de la pantalla como control de mantener/soltar.
/// </summary>
public class TensionSystem : MonoBehaviour
{
    private enum EstadoPesca
    {
        Listo,
        EsperandoPique,
        EnForcejeo,
        EsperandoRecogida
    }

    [Header("RF3: lanzamiento y pique")]
    [SerializeField] private bool simularPiqueAutomatico = true;
    [SerializeField] private Vector2 demoraPique = new Vector2(2f, 4f);
    [SerializeField, Min(1f)] private float duracionForcejeo = 8f;
    [SerializeField] private CanaData canaEquipada;
    [SerializeField] private UnityEvent alLanzar = new UnityEvent();
    [SerializeField] private UnityEvent alPique = new UnityEvent();

    [Header("RF3: zona táctil inferior")]
    [SerializeField] private bool zonaActiva;
    [SerializeField] private bool mostrarIndicador = true;
    [SerializeField, Range(0f, 1f)] private float opacidadZona = 0.24f;
    [SerializeField] private UnityEvent alPresionar = new UnityEvent();
    [SerializeField] private UnityEvent alSoltar = new UnityEvent();

    private EstadoPesca estado = EstadoPesca.Listo;
    private float tiempoParaPique;
    private GameObject hudLanzamiento;
    private Image fondoZona;
    private Text textoZona;
    private Button botonLanzar;
    private Text textoBotonLanzar;
    private Text subtituloBotonLanzar;
    private Text marcaBotonLanzar;
    private Text textoEstado;
    private Sprite spriteBotonCircular;
    private Texture2D texturaBotonCircular;
    private CapturaResultUI interfazResultado;
    private PezData pezEnAnzuelo;
    private float tiempoForcejeo;
    private float tiempoEnZonaSegura;
    private bool capturaTouch;
    private bool capturaMouse;
    private bool estaPresionado;

    /// <summary>True mientras el jugador mantiene pulsada la zona inferior.</summary>
    public bool EstaPresionado => estaPresionado;
    /// <summary>Estado de pique para la presentación; no modifica el cálculo de captura.</summary>
    public bool EnForcejeo => estado == EstadoPesca.EnForcejeo && zonaActiva;
    public bool PuedeCambiarCana => estado == EstadoPesca.Listo;
    public CanaData CanaEquipada => canaEquipada;

    /// <summary>Actualiza la caña usada por la evaluación de captura RF5.</summary>
    public bool EquiparCana(CanaData cana)
    {
        if (cana == null || !PuedeCambiarCana)
            return false;

        canaEquipada = cana;
        return true;
    }

    /// <summary>Notifica true al presionar y false al soltar.</summary>
    public event Action<bool> PresionCambiada;

    /// <summary>Señala que se pulsó el botón para lanzar el anzuelo.</summary>
    public event Action LanzamientoIniciado;

    /// <summary>Señala que comenzó el forcejeo y se habilitó la zona inferior.</summary>
    public event Action PiqueDetectado;

    private void Awake()
    {
        interfazResultado = GetComponent<CapturaResultUI>();
        if (interfazResultado != null)
            interfazResultado.alRecoger.AddListener(VolverAListo);

        AsegurarEventSystem();
        CrearInterfazPesca();
        SetZonaActiva(false);
        VolverAListo();
    }

    private void Update()
    {
        if (estado == EstadoPesca.Listo)
            LeerEntradaDeLanzamiento();

        if (estado == EstadoPesca.EsperandoPique && simularPiqueAutomatico)
        {
            tiempoParaPique -= Time.deltaTime;
            if (tiempoParaPique <= 0f)
                NotificarPique();
        }

        if (estado != EstadoPesca.EnForcejeo || !zonaActiva)
        {
            if (estaPresionado)
                CambiarEstadoPresion(false);
            return;
        }

        LeerEntradaDeZona();
        ActualizarForcejeo();
    }

    // Respaldo al Button de Unity: permite lanzar incluso si el EventSystem de
    // la escena no entrega el callback de UI. La prueba conserva la forma circular.
    private void LeerEntradaDeLanzamiento()
    {
        TouchControl touch = Touchscreen.current != null
            ? Touchscreen.current.primaryTouch
            : null;

        if (touch != null && touch.press.wasPressedThisFrame &&
            EstaDentroDelBoton(touch.position.ReadValue()))
        {
            IniciarLanzamiento();
            return;
        }

        Pointer pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame &&
            EstaDentroDelBoton(pointer.position.ReadValue()))
        {
            IniciarLanzamiento();
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
            EstaDentroDelBoton(Mouse.current.position.ReadValue()))
            IniciarLanzamiento();
    }

    private bool EstaDentroDelBoton(Vector2 posicionPantalla)
    {
        var rect = botonLanzar.transform as RectTransform;
        if (rect == null)
            return false;

        Vector2 centroPantalla = RectTransformUtility.WorldToScreenPoint(
            null, rect.TransformPoint(rect.rect.center));
        Vector2 mitad = Vector2.Scale(rect.rect.size, rect.lossyScale) * 0.46f;
        if (mitad.x <= 0f || mitad.y <= 0f)
            return false;

        Vector2 distanciaNormalizada = new Vector2(
            (posicionPantalla.x - centroPantalla.x) / mitad.x,
            (posicionPantalla.y - centroPantalla.y) / mitad.y);
        return distanciaNormalizada.sqrMagnitude <= 1f;
    }

    /// <summary>Callback del botón táctil «Lanzar / Pescar».</summary>
    public void IniciarLanzamiento()
    {
        if (estado != EstadoPesca.Listo)
            return;

        var gestorPeces = FisheryManager.Instance;
        if (gestorPeces == null)
        {
            Debug.LogError("No se encontró FisheryManager en la escena.", this);
            textoEstado.text = "No se pudo iniciar: falta el gestor de peces.";
            return;
        }

        pezEnAnzuelo = gestorPeces.GenerarPezAleatorio();
        if (pezEnAnzuelo == null)
        {
            textoEstado.text = "No se pudo iniciar: revisa los peces de Amazonas.";
            return;
        }

        estado = EstadoPesca.EsperandoPique;
        SetZonaActiva(false);
        botonLanzar.interactable = false;
        textoBotonLanzar.text = "ESPERA";
        subtituloBotonLanzar.text = "EL PIQUE";
        textoEstado.text = simularPiqueAutomatico
            ? "El anzuelo cayó al agua. Esperando un pique..."
            : "El anzuelo cayó al agua. Esperando la señal de pique...";

        float minimo = Mathf.Max(0f, Mathf.Min(demoraPique.x, demoraPique.y));
        float maximo = Mathf.Max(minimo, Mathf.Max(demoraPique.x, demoraPique.y));
        tiempoParaPique = UnityEngine.Random.Range(minimo, maximo);
        LanzamientoIniciado?.Invoke();
        alLanzar.Invoke();

        // En modo manual, RF2 u otro controlador debe llamar NotificarPique().
        if (simularPiqueAutomatico && tiempoParaPique <= 0f)
            NotificarPique();
    }

    /// <summary>
    /// Llama este método cuando el sistema real de pesca detecte un pique.
    /// También se usa automáticamente si está activa la simulación de prueba.
    /// </summary>
    public void NotificarPique()
    {
        if (estado != EstadoPesca.EsperandoPique)
            return;

        estado = EstadoPesca.EnForcejeo;
        tiempoForcejeo = 0f;
        tiempoEnZonaSegura = 0f;
        SetZonaActiva(true);
        hudLanzamiento.SetActive(false);
        ColocarMensajeDePique();
        textoEstado.text = "¡PICÓ! " + pezEnAnzuelo.nombreEspecie;
        PiqueDetectado?.Invoke();
        alPique.Invoke();
    }

    /// <summary>
    /// RF5 consume el porcentaje de control simulado por RF4. Por ahora se mide
    /// como la fracción del forcejeo durante la que el jugador mantiene pulsada
    /// la zona inferior; RF4 podrá sustituir esta medida por su barra de tensión.
    /// </summary>
    private void ActualizarForcejeo()
    {
        float delta = Time.deltaTime;
        tiempoForcejeo += delta;
        if (estaPresionado)
            tiempoEnZonaSegura += delta;

        float restante = Mathf.Max(0f, duracionForcejeo - tiempoForcejeo);
        textoEstado.text = $"¡PICÓ! {pezEnAnzuelo.nombreEspecie} — {Mathf.CeilToInt(restante)} s";

        if (tiempoForcejeo < duracionForcejeo)
            return;

        float porcentajeControlado = tiempoForcejeo > 0f
            ? tiempoEnZonaSegura / tiempoForcejeo
            : 0f;
        CanaData cana = canaEquipada;
        if (cana == null)
        {
            Debug.LogWarning("No hay caña asignada; se usará una caña básica para resolver RF5.", this);
            cana = ScriptableObject.CreateInstance<CanaData>();
            cana.nombreCana = "Caña básica";
            ResolverIntentoCapturaConCanaTemporal(cana, porcentajeControlado);
            return;
        }

        ResolverIntentoCaptura(
            pezEnAnzuelo,
            cana,
            porcentajeControlado,
            out _);
    }

    private void ResolverIntentoCapturaConCanaTemporal(CanaData cana, float porcentajeControlado)
    {
        try
        {
            ResolverIntentoCaptura(
                pezEnAnzuelo,
                cana,
                porcentajeControlado,
                out _);
        }
        finally
        {
            Destroy(cana);
        }
    }

    /// <summary>
    /// Permite activar u ocultar el control inferior desde otros sistemas.
    /// </summary>
    public void SetZonaActiva(bool activa)
    {
        zonaActiva = activa;
        if (!activa)
        {
            capturaTouch = false;
            capturaMouse = false;
            CambiarEstadoPresion(false);
        }

        ActualizarVisualZona();
    }

    /// <summary>
    /// RF4 pasa su proporción de tiempo en zona segura. RF5 calcula el resultado
    /// usando la fuerza del pez y el poder/tolerancia de la caña equipada.
    /// </summary>
    public bool ResolverIntentoCaptura(
        PezData pez,
        CanaData cana,
        float porcentajeControlado,
        out CapturaEvaluacion evaluacion)
    {
        if (estado != EstadoPesca.EnForcejeo)
            throw new InvalidOperationException("Solo se puede resolver una captura después del pique.");
        if (interfazResultado == null)
            throw new InvalidOperationException("Falta CapturaResultUI en el mismo objeto que TensionSystem.");

        SetZonaActiva(false);
        bool capturada = interfazResultado.ResolverIntentoCaptura(
            pez,
            cana,
            porcentajeControlado,
            out evaluacion);

        if (capturada)
        {
            estado = EstadoPesca.EsperandoRecogida;
            textoEstado.text = "Captura conseguida. Recoge el pez para volver a pescar.";
        }
        else
        {
            VolverAListo();
            textoEstado.text = "El pez escapó. Pulsa para volver a pescar.";
        }

        return capturada;
    }

    private void LeerEntradaDeZona()
    {
        TouchControl touch = Touchscreen.current != null
            ? Touchscreen.current.primaryTouch
            : null;

        // Conserva la fuente que inició la pulsación hasta que se suelte.
        if (capturaTouch)
        {
            if (touch != null && touch.press.isPressed)
                return;

            capturaTouch = false;
            CambiarEstadoPresion(false);
        }

        if (capturaMouse)
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return;

            capturaMouse = false;
            CambiarEstadoPresion(false);
        }

        if (touch != null && touch.press.wasPressedThisFrame && EstaDentroDeZona(touch.position.ReadValue()))
        {
            capturaTouch = true;
            CambiarEstadoPresion(true);
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                 EstaDentroDeZona(Mouse.current.position.ReadValue()))
        {
            capturaMouse = true;
            CambiarEstadoPresion(true);
        }
    }

    private bool EstaDentroDeZona(Vector2 posicion)
    {
        return posicion.x >= 0f && posicion.x <= Screen.width &&
               posicion.y >= 0f && posicion.y <= Screen.height * 0.5f;
    }

    private void CambiarEstadoPresion(bool presionada)
    {
        if (estaPresionado == presionada)
            return;

        estaPresionado = presionada;
        PresionCambiada?.Invoke(estaPresionado);
        if (estaPresionado)
            alPresionar?.Invoke();
        else
            alSoltar?.Invoke();

        ActualizarVisualZona();
    }

    private void CrearInterfazPesca()
    {
        var canvasObject = new GameObject(
            "Canvas Controles de Pesca",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        CrearZonaInferior(canvasObject.transform);
        hudLanzamiento = new GameObject("Interfaz de lanzamiento", typeof(RectTransform));
        hudLanzamiento.transform.SetParent(canvasObject.transform, false);
        var hudRect = hudLanzamiento.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
        CrearBotonLanzar(hudLanzamiento.transform);
        CrearTextoEstado(canvasObject.transform);
    }

    private void CrearZonaInferior(Transform padre)
    {
        var panel = new GameObject(
            "Botón Mitad Inferior",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panel.transform.SetParent(padre, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        fondoZona = panel.GetComponent<Image>();
        fondoZona.raycastTarget = false;

        var label = CrearTexto("Instrucción de tensión", panel.transform);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.05f, 0.12f);
        labelRect.anchorMax = new Vector2(0.95f, 0.88f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        textoZona = label;
        textoZona.fontSize = 30;
        textoZona.resizeTextMinSize = 16;
        textoZona.resizeTextMaxSize = 30;
    }

    private void CrearBotonLanzar(Transform padre)
    {
        var botonObject = new GameObject(
            "Botón Lanzar Pescar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        botonObject.transform.SetParent(padre, false);

        var rect = botonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(240f, 240f);
        rect.anchoredPosition = new Vector2(-48f, 28f);

        var imagen = botonObject.GetComponent<Image>();
        spriteBotonCircular = CrearSpriteBotonCircular();
        imagen.sprite = spriteBotonCircular;
        imagen.preserveAspect = true;
        // Las esquinas transparentes no reciben toques; el centro translúcido
        // supera este umbral y sigue funcionando como botón.
        imagen.alphaHitTestMinimumThreshold = 0.45f;
        imagen.color = Color.white;
        var boton = botonObject.GetComponent<Button>();
        boton.targetGraphic = imagen;
        var colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = Hex("FFF2D8");
        colores.pressedColor = Hex("DDE8D8");
        colores.selectedColor = Hex("FFF2D8");
        colores.disabledColor = Hex("8A9188");
        colores.colorMultiplier = 1f;
        boton.colors = colores;
        boton.onClick.AddListener(IniciarLanzamiento);
        botonLanzar = boton;

        textoBotonLanzar = CrearTexto("Acción del botón", botonObject.transform);
        textoBotonLanzar.text = "LANZAR";
        textoBotonLanzar.fontSize = 21;
        textoBotonLanzar.resizeTextMinSize = 15;
        textoBotonLanzar.resizeTextMaxSize = 21;
        textoBotonLanzar.color = Hex("F4E9C8");
        var accionRect = textoBotonLanzar.GetComponent<RectTransform>();
        accionRect.anchorMin = new Vector2(0.12f, 0.45f);
        accionRect.anchorMax = new Vector2(0.88f, 0.66f);

        subtituloBotonLanzar = CrearTexto("Subtítulo de pesca", botonObject.transform);
        subtituloBotonLanzar.text = "PESCA";
        subtituloBotonLanzar.fontSize = 13;
        subtituloBotonLanzar.resizeTextMinSize = 10;
        subtituloBotonLanzar.resizeTextMaxSize = 13;
        subtituloBotonLanzar.color = Hex("E1C17A");
        var subtituloRect = subtituloBotonLanzar.GetComponent<RectTransform>();
        subtituloRect.anchorMin = new Vector2(0.15f, 0.3f);
        subtituloRect.anchorMax = new Vector2(0.85f, 0.45f);

        marcaBotonLanzar = CrearTexto("Marca Amazonas", botonObject.transform);
        marcaBotonLanzar.text = "AMAZONAS";
        marcaBotonLanzar.fontSize = 9;
        marcaBotonLanzar.resizeTextMinSize = 7;
        marcaBotonLanzar.resizeTextMaxSize = 9;
        marcaBotonLanzar.color = Hex("F4E9C8");
        var marcaRect = marcaBotonLanzar.GetComponent<RectTransform>();
        marcaRect.anchorMin = new Vector2(0.15f, 0.65f);
        marcaRect.anchorMax = new Vector2(0.85f, 0.78f);
    }

    private void CrearTextoEstado(Transform padre)
    {
        textoEstado = CrearTexto("Estado de pesca", padre);
        var rect = textoEstado.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.12f, 0.28f);
        rect.anchorMax = new Vector2(0.88f, 0.38f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        textoEstado.fontSize = 22;
        textoEstado.resizeTextMinSize = 12;
        textoEstado.resizeTextMaxSize = 22;
    }

    private void ColocarMensajeDePique()
    {
        var rect = textoEstado.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.12f, 0.84f);
        rect.anchorMax = new Vector2(0.88f, 0.94f);
    }

    private void ColocarMensajeListo()
    {
        var rect = textoEstado.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.12f, 0.28f);
        rect.anchorMax = new Vector2(0.88f, 0.38f);
    }

    private static Text CrearTexto(string nombre, Transform padre)
    {
        var label = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(padre, false);
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.raycastTarget = false;
        return text;
    }

    private static Color Hex(string valor)
    {
        ColorUtility.TryParseHtmlString("#" + valor, out Color color);
        return color;
    }

    private Sprite CrearSpriteBotonCircular()
    {
        const int resolucion = 256;
        var pixeles = new Color[resolucion * resolucion];
        Color verdeOscuro = Hex("244334");
        Color verdeHoja = Hex("456B48");
        Color dorado = Hex("D4AA5E");
        Color madera = Hex("3D281B");
        Color borde = Hex("17251F");

        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                float dx = (x + 0.5f - resolucion * 0.5f) / (resolucion * 0.5f);
                float dy = (y + 0.5f - resolucion * 0.5f) / (resolucion * 0.5f);
                float radio = Mathf.Sqrt(dx * dx + dy * dy);
                int indice = y * resolucion + x;

                // Deja un pequeño margen transparente dentro del rectángulo
                // cuadrado para que tanto el dibujo como el área táctil sean circulares.
                if (radio >= 0.92f)
                {
                    pixeles[indice] = Color.clear;
                    continue;
                }

                Color color;
                float opacidad;
                if (radio >= 0.87f)
                {
                    color = borde;
                    opacidad = 0.96f;
                }
                else if (radio >= 0.81f)
                {
                    color = dorado;
                    opacidad = 0.88f;
                }
                else if (radio >= 0.75f)
                {
                    color = madera;
                    opacidad = 0.76f;
                }
                else
                {
                    float luz = Mathf.Clamp01(0.25f + (y / (float)resolucion) * 0.45f + (0.8f - radio) * 0.45f);
                    color = Color.Lerp(verdeOscuro, verdeHoja, luz);
                    opacidad = 0.62f;
                }

                float suavizadoBorde = 1f - Mathf.SmoothStep(0.90f, 0.92f, radio);
                color.a = opacidad * suavizadoBorde;
                pixeles[indice] = color;
            }
        }

        texturaBotonCircular = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, false)
        {
            name = "Botón circular de pesca Amazonas",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texturaBotonCircular.SetPixels(pixeles);
        texturaBotonCircular.Apply(false, false);

        return Sprite.Create(
            texturaBotonCircular,
            new Rect(0f, 0f, resolucion, resolucion),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
    }

    private void AsegurarEventSystem()
    {
        var eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventos = new GameObject("EventSystem");
            eventos.transform.SetParent(transform, false);
            eventSystem = eventos.AddComponent<EventSystem>();
        }

        // Puede existir un EventSystem de otra escena o prefab sin módulo
        // compatible con el Input System configurado por el proyecto.
        var uiInput = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (uiInput == null)
            uiInput = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        var legacyInput = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyInput != null)
            legacyInput.enabled = false;
    }

    private void ActualizarVisualZona()
    {
        if (fondoZona == null || textoZona == null)
            return;

        bool visible = mostrarIndicador && zonaActiva;
        fondoZona.enabled = visible;
        textoZona.enabled = visible;
        if (!visible)
            return;

        fondoZona.color = estaPresionado
            ? new Color(0.10f, 0.48f, 0.32f, Mathf.Min(0.55f, opacidadZona + 0.18f))
            : new Color(0.02f, 0.08f, 0.08f, opacidadZona);
        textoZona.text = estaPresionado
            ? "TENSANDO...\nSUELTA PARA LIBERAR"
            : "MANTÉN PRESIONADO PARA TENSAR";
    }

    private void VolverAListo()
    {
        estado = EstadoPesca.Listo;
        SetZonaActiva(false);
        botonLanzar.interactable = true;
        hudLanzamiento.SetActive(true);
        textoBotonLanzar.text = "LANZAR";
        subtituloBotonLanzar.text = "PESCA";
        marcaBotonLanzar.text = "AMAZONAS";
        ColocarMensajeListo();
        textoEstado.text = "Pulsa para lanzar el anzuelo.";
    }

    private void OnDisable()
    {
        capturaTouch = false;
        capturaMouse = false;
        CambiarEstadoPresion(false);
    }

    private void OnDestroy()
    {
        if (interfazResultado != null)
            interfazResultado.alRecoger.RemoveListener(VolverAListo);

        if (spriteBotonCircular != null)
            Destroy(spriteBotonCircular);
        if (texturaBotonCircular != null)
            Destroy(texturaBotonCircular);
    }
}
