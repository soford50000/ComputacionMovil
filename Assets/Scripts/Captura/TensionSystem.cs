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
    [Header("RF4: tensión vinculada al toque")]
    [SerializeField, Min(0.05f)] private float velocidadSubidaTension = 0.38f;
    [SerializeField, Min(0.05f)] private float velocidadBajadaTension = 0.44f;
    [SerializeField] private Vector2 intervaloTirones = new Vector2(1.4f, 2.6f);
    [SerializeField, Min(0.1f)] private float tiempoMaximoEnRojo = 1.1f;
    [SerializeField] private CanaData canaEquipada;
    [SerializeField] private CeboData ceboEquipado;
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
    private CanaVisual visualCana;
    private PezData pezEnAnzuelo;
    private float tiempoForcejeo;
    private float tiempoEnZonaSegura;
    private float tensionActual = 0.5f;
    private float tiempoEnRojo;
    private float tiempoHastaTiron;
    private float tiempoDesdeTiron = 10f;
    private float tironPendiente;
    private float velocidadTironPendiente;
    private const float DuracionVisualTiron = 0.32f;
    private bool capturaTouch;
    private bool capturaMouse;
    private bool estaPresionado;
    private const float TensionSeguraMinima = 0.17f;
    private const float TensionSeguraMaxima = 0.83f;

    /// <summary>True mientras el jugador mantiene pulsada la zona inferior.</summary>
    public bool EstaPresionado => estaPresionado;
    /// <summary>Estado de pique para la presentación; no modifica el cálculo de captura.</summary>
    public bool EnForcejeo => estado == EstadoPesca.EnForcejeo && zonaActiva;
    public bool PuedeCambiarCana => estado == EstadoPesca.Listo;
    public CanaData CanaEquipada => canaEquipada;
    public CeboData CeboEquipado => ceboEquipado;
    public bool PuedeCambiarCebo => estado == EstadoPesca.Listo;
    public event Action CeboCambiado;
    private CeboSelectorUI selectorCebos;
    private FisheryManager gestorZonas;
    private ZonaSelectorUI selectorZonas;

    public bool EquiparCebo(CeboData cebo)
    {
        if (!PuedeCambiarCebo) return false;
        ceboEquipado = cebo; // null permite volver a las probabilidades base.
        CeboCambiado?.Invoke();
        return true;
    }

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
        visualCana = GetComponent<CanaVisual>();
        if (interfazResultado != null)
            interfazResultado.alRecoger.AddListener(VolverAListo);

        AsegurarEventSystem();
        CrearInterfazPesca();
        SetZonaActiva(false);
        VolverAListo();
        selectorCebos = GetComponent<CeboSelectorUI>();
        if (selectorCebos == null) selectorCebos = gameObject.AddComponent<CeboSelectorUI>();
    }

    private void Start()
    {
        gestorZonas = GetComponent<FisheryManager>();
        selectorZonas = GetComponent<ZonaSelectorUI>();
        if (gestorZonas != null)
        {
            gestorZonas.ZonaCambiada += AlCambiarZona;
            AlCambiarZona(gestorZonas.ZonaActual);
        }
    }

    private void AlCambiarZona(ZonaData zona) => VolverAListo();

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
        if (estado != EstadoPesca.Listo || (selectorCebos != null && selectorCebos.EstaAbierto) ||
            (selectorZonas != null && selectorZonas.EstaAbierto))
            return;

        var gestorPeces = FisheryManager.Instance;
        if (gestorPeces == null)
        {
            Debug.LogError("No se encontró FisheryManager en la escena.", this);
            textoEstado.text = "No se pudo iniciar: falta el gestor de peces.";
            return;
        }

        if (!gestorPeces.TieneEspecies)
        {
            textoEstado.text = "Las especies de esta zona estarán disponibles próximamente.";
            return;
        }

        pezEnAnzuelo = gestorPeces.GenerarPezAleatorio(cebo: ceboEquipado);
        if (pezEnAnzuelo == null)
        {
            textoEstado.text = "No se pudo iniciar: revisa los peces de esta zona.";
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
        tensionActual = 0.5f;
        tiempoEnRojo = 0f;
        tironPendiente = 0f;
        velocidadTironPendiente = 0f;
        tiempoHastaTiron = ObtenerIntervaloTironAleatorio();
        tiempoDesdeTiron = 10f;
        if (visualCana != null)
            visualCana.EstablecerTension(tensionActual);
        SetZonaActiva(true);
        hudLanzamiento.SetActive(false);
        ColocarMensajeDePique();
        textoEstado.text = "¡PICÓ!";
        PiqueDetectado?.Invoke();
        alPique.Invoke();
    }

    /// <summary>
    /// RF4: mantener pulsada la zona sube la tensión y soltarla la baja. Los
    /// tirones del pez también mueven el indicador del arco que dibuja CanaVisual.
    /// RF5 usa el tiempo que el indicador permanece en verde o amarillo.
    /// </summary>
    private void ActualizarForcejeo()
    {
        float delta = Time.deltaTime;
        tiempoForcejeo += delta;
        tiempoDesdeTiron += delta;

        float fuerzaEfectiva = ObtenerFuerzaEfectiva();
        float poderCana = canaEquipada != null ? Mathf.Clamp(canaEquipada.poderCana, 1f, 10f) : 1f;
        float factorPez = 0.75f + Mathf.Clamp01(fuerzaEfectiva / 10f) * 0.45f;
        float factorCana = 1f / (1f + Mathf.Max(0f, poderCana - 1f) * 0.055f);
        float cambioManual = estaPresionado ? velocidadSubidaTension : -velocidadBajadaTension;
        tensionActual += cambioManual * factorPez * factorCana * delta;

        // Distribuye el impulso del tirón en varios fotogramas para evitar un
        // salto visual del marcador, manteniendo la misma fuerza total.
        if (tironPendiente > 0f)
        {
            float aplicado = Mathf.Min(tironPendiente, velocidadTironPendiente * delta);
            tensionActual += aplicado;
            tironPendiente -= aplicado;
            if (tironPendiente <= 0.0001f)
            {
                tironPendiente = 0f;
                velocidadTironPendiente = 0f;
            }
        }

        tiempoHastaTiron -= delta;
        if (tiempoHastaTiron <= 0f)
        {
            float intensidadTiron = Mathf.Lerp(0.07f, 0.19f, Mathf.Clamp01(fuerzaEfectiva / 10f));
            tironPendiente = UnityEngine.Random.Range(intensidadTiron * 0.65f, intensidadTiron) * factorCana;
            velocidadTironPendiente = tironPendiente / DuracionVisualTiron;
            tiempoHastaTiron = ObtenerIntervaloTironAleatorio();
            tiempoDesdeTiron = 0f;
        }

        tensionActual = Mathf.Clamp01(tensionActual);
        if (visualCana != null)
            visualCana.EstablecerTension(tensionActual);

        bool enZonaSegura = tensionActual >= TensionSeguraMinima && tensionActual <= TensionSeguraMaxima;
        if (enZonaSegura)
            tiempoEnZonaSegura += delta;
        tiempoEnRojo = enZonaSegura ? 0f : tiempoEnRojo + delta;

        ActualizarTextoTension();

        if (tiempoEnRojo >= tiempoMaximoEnRojo)
        {
            ResolverIntentoConControl(0f);
            if (estado == EstadoPesca.Listo)
                textoEstado.text = "La línea se rompió. Pulsa para volver a pescar.";
            return;
        }

        if (tiempoForcejeo < duracionForcejeo)
            return;

        float porcentajeControlado = tiempoForcejeo > 0f
            ? tiempoEnZonaSegura / tiempoForcejeo
            : 0f;
        ResolverIntentoConControl(porcentajeControlado);
    }

    private float ObtenerFuerzaEfectiva()
    {
        float fuerza = pezEnAnzuelo != null ? Mathf.Clamp(pezEnAnzuelo.fuerzaPez, 1f, 10f) : 1f;
        float poder = canaEquipada != null ? Mathf.Clamp(canaEquipada.poderCana, 1f, 10f) : 1f;
        return Mathf.Max(1f, fuerza - (poder - 1f) * 0.65f);
    }

    private float ObtenerIntervaloTironAleatorio()
    {
        float minimo = Mathf.Max(0.25f, Mathf.Min(intervaloTirones.x, intervaloTirones.y));
        float maximo = Mathf.Max(minimo, Mathf.Max(intervaloTirones.x, intervaloTirones.y));
        return UnityEngine.Random.Range(minimo, maximo);
    }

    private void ResolverIntentoConControl(float porcentajeControlado)
    {
        CanaData cana = canaEquipada;
        if (cana != null)
        {
            ResolverIntentoCaptura(pezEnAnzuelo, cana, porcentajeControlado, out _);
            return;
        }

        Debug.LogWarning("No hay caña asignada; se usará una caña básica para resolver RF5.", this);
        cana = ScriptableObject.CreateInstance<CanaData>();
        cana.nombreCana = "Caña básica";
        try
        {
            ResolverIntentoCaptura(pezEnAnzuelo, cana, porcentajeControlado, out _);
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
        marcaBotonLanzar.text = FisheryManager.Instance != null && FisheryManager.Instance.ZonaActual != null
            ? FisheryManager.Instance.ZonaActual.nombreZona.ToUpperInvariant() : "AMAZONAS";
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
        text.font = EstiloExpedicion.Fuente;
        text.fontStyle = FontStyle.Normal;
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
        var sistemas = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        var eventSystem = EventSystem.current;
        if (eventSystem == null && sistemas.Length > 0)
            eventSystem = sistemas[0];
        if (eventSystem == null)
        {
            var eventos = new GameObject("EventSystem");
            eventos.transform.SetParent(transform, false);
            eventSystem = eventos.AddComponent<EventSystem>();
        }
        eventSystem.enabled = true;

        // Mantiene un único EventSystem activo para que los raycasters de los
        // Canvas no compitan entre módulos creados por escenas anteriores.
        foreach (var sistema in sistemas)
        {
            if (sistema == null || sistema == eventSystem) continue;
            foreach (var moduloDuplicado in sistema.GetComponents<BaseInputModule>())
                moduloDuplicado.enabled = false;
            sistema.enabled = false;
        }

        var uiInput = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (uiInput == null)
            uiInput = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        uiInput.enabled = true;
        if (uiInput.actionsAsset == null || uiInput.point == null || uiInput.point.action == null ||
            uiInput.leftClick == null || uiInput.leftClick.action == null)
            uiInput.AssignDefaultActions();

        foreach (var modulo in eventSystem.GetComponents<BaseInputModule>())
            if (modulo != uiInput) modulo.enabled = false;
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
        ActualizarTextoTension();
    }

    private void ActualizarTextoTension()
    {
        if (textoZona == null || !zonaActiva)
            return;

        int porcentaje = Mathf.RoundToInt(tensionActual * 100f);
        string instruccion;
        if (tiempoDesdeTiron < 0.65f)
            instruccion = "¡TIRÓN! AJUSTA LA TENSIÓN";
        else if (tensionActual > TensionSeguraMaxima)
            instruccion = "TENSIÓN ALTA · SUELTA PARA BAJAR";
        else if (tensionActual < TensionSeguraMinima)
            instruccion = "LÍNEA FLOJA · MANTÉN PARA SUBIR";
        else if (estaPresionado)
            instruccion = "TENSANDO · SUELTA PARA BAJAR";
        else
            instruccion = "MANTÉN PARA SUBIR · SUELTA PARA BAJAR";

        textoZona.text = $"TENSIÓN {porcentaje}%\n{instruccion}";
    }

    private void VolverAListo()
    {
        estado = EstadoPesca.Listo;
        SetZonaActiva(false);
        bool disponible = gestorZonas == null || gestorZonas.TieneEspecies;
        botonLanzar.interactable = disponible;
        hudLanzamiento.SetActive(true);
        textoBotonLanzar.text = disponible ? "LANZAR" : "PRONTO";
        subtituloBotonLanzar.text = "PESCA";
        marcaBotonLanzar.text = gestorZonas != null && gestorZonas.ZonaActual != null
            ? gestorZonas.ZonaActual.nombreZona.ToUpperInvariant() : "AMAZONAS";
        ColocarMensajeListo();
        textoEstado.text = disponible ? "Pulsa para lanzar el anzuelo."
            : "Las especies de esta zona estarán disponibles próximamente.";
    }

    private void OnDisable()
    {
        capturaTouch = false;
        capturaMouse = false;
        CambiarEstadoPresion(false);
    }

    private void OnDestroy()
    {
        if (gestorZonas != null) gestorZonas.ZonaCambiada -= AlCambiarZona;
        if (interfazResultado != null)
            interfazResultado.alRecoger.RemoveListener(VolverAListo);

        if (spriteBotonCircular != null)
            Destroy(spriteBotonCircular);
        if (texturaBotonCircular != null)
            Destroy(texturaBotonCircular);
    }
}
