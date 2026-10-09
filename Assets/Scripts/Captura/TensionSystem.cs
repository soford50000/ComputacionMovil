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
    private Image fondoZona;
    private Text textoZona;
    private Button botonLanzar;
    private Text textoBotonLanzar;
    private Text subtituloBotonLanzar;
    private Text textoEstado;
    private CapturaResultUI interfazResultado;
    private bool capturaTouch;
    private bool capturaMouse;
    private bool estaPresionado;

    /// <summary>True mientras el jugador mantiene pulsada la zona inferior.</summary>
    public bool EstaPresionado => estaPresionado;

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
    }

    /// <summary>Callback del botón táctil «Lanzar / Pescar».</summary>
    public void IniciarLanzamiento()
    {
        if (estado != EstadoPesca.Listo)
            return;

        estado = EstadoPesca.EsperandoPique;
        SetZonaActiva(false);
        botonLanzar.interactable = false;
        textoBotonLanzar.text = "ESPERANDO...";
        subtituloBotonLanzar.text = "EL ANZUELO YA ESTÁ EN EL AGUA";
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
        SetZonaActiva(true);
        botonLanzar.gameObject.SetActive(false);
        textoEstado.text = "¡PICÓ! Mantén pulsada la mitad inferior para tensar.";
        PiqueDetectado?.Invoke();
        alPique.Invoke();
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
        int oro,
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
            oro,
            out evaluacion);

        if (capturada)
        {
            estado = EstadoPesca.EsperandoRecogida;
            textoEstado.text = "Captura conseguida. Recoge el pez para volver a pescar.";
        }
        else
        {
            VolverAListo();
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
        CrearBotonLanzar(canvasObject.transform);
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
        CrearBloqueDecorativo(
            "Sombra del botón",
            padre,
            Hex("17251F"),
            new Vector2(0.165f, 0.045f),
            new Vector2(0.835f, 0.235f));
        CrearBloqueDecorativo(
            "Marco de madera y latón",
            padre,
            Hex("C79B52"),
            new Vector2(0.17f, 0.055f),
            new Vector2(0.83f, 0.235f));

        var botonObject = new GameObject(
            "Botón Lanzar Pescar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        botonObject.transform.SetParent(padre, false);

        var rect = botonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.185f, 0.07f);
        rect.anchorMax = new Vector2(0.815f, 0.22f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var imagen = botonObject.GetComponent<Image>();
        imagen.color = Hex("315441");
        var boton = botonObject.GetComponent<Button>();
        boton.targetGraphic = imagen;
        var colores = boton.colors;
        colores.normalColor = Hex("315441");
        colores.highlightedColor = Hex("426C4C");
        colores.pressedColor = Hex("243F35");
        colores.selectedColor = Hex("426C4C");
        colores.disabledColor = Hex("5A6257");
        colores.colorMultiplier = 1f;
        boton.colors = colores;
        boton.onClick.AddListener(IniciarLanzamiento);
        botonLanzar = boton;

        textoBotonLanzar = CrearTexto("Acción del botón", botonObject.transform);
        textoBotonLanzar.text = "LANZAR / PESCAR";
        textoBotonLanzar.fontSize = 25;
        textoBotonLanzar.resizeTextMinSize = 16;
        textoBotonLanzar.resizeTextMaxSize = 25;
        textoBotonLanzar.color = Hex("F4E9C8");
        var accionRect = textoBotonLanzar.GetComponent<RectTransform>();
        accionRect.anchorMin = new Vector2(0.08f, 0.39f);
        accionRect.anchorMax = new Vector2(0.92f, 0.88f);

        subtituloBotonLanzar = CrearTexto("Subtítulo de pesca", botonObject.transform);
        subtituloBotonLanzar.text = "TOCA PARA LANZAR EL ANZUELO";
        subtituloBotonLanzar.fontSize = 12;
        subtituloBotonLanzar.resizeTextMinSize = 9;
        subtituloBotonLanzar.resizeTextMaxSize = 12;
        subtituloBotonLanzar.color = Hex("E1C17A");
        var subtituloRect = subtituloBotonLanzar.GetComponent<RectTransform>();
        subtituloRect.anchorMin = new Vector2(0.08f, 0.1f);
        subtituloRect.anchorMax = new Vector2(0.92f, 0.4f);

        CrearAcento("Ribete superior", botonObject.transform,
            new Vector2(0.08f, 0.91f), new Vector2(0.92f, 0.935f));
        CrearAcento("Detalle izquierdo", botonObject.transform,
            new Vector2(0.035f, 0.24f), new Vector2(0.05f, 0.76f));
        CrearAcento("Detalle derecho", botonObject.transform,
            new Vector2(0.95f, 0.24f), new Vector2(0.965f, 0.76f));
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

    private static void CrearBloqueDecorativo(string nombre, Transform padre, Color color, Vector2 minimo, Vector2 maximo)
    {
        var bloque = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bloque.transform.SetParent(padre, false);
        var rect = bloque.GetComponent<RectTransform>();
        rect.anchorMin = minimo;
        rect.anchorMax = maximo;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var imagen = bloque.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
    }

    private static void CrearAcento(string nombre, Transform padre, Vector2 minimo, Vector2 maximo)
    {
        var acento = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        acento.transform.SetParent(padre, false);
        var rect = acento.GetComponent<RectTransform>();
        rect.anchorMin = minimo;
        rect.anchorMax = maximo;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var imagen = acento.GetComponent<Image>();
        imagen.color = Hex("D7AE62");
        imagen.raycastTarget = false;
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

    private void AsegurarEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var eventos = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventos.transform.SetParent(transform, false);
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
        botonLanzar.gameObject.SetActive(true);
        textoBotonLanzar.text = "LANZAR / PESCAR";
        subtituloBotonLanzar.text = "TOCA PARA LANZAR EL ANZUELO";
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
    }
}
