using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// Control de tensión por pulsación: la zona interactiva ocupa la mitad
/// inferior de la pantalla y funciona tanto con toque como con mouse.
/// </summary>
public class TensionSystem : MonoBehaviour
{
    [Header("Zona táctil")]
    [SerializeField] private bool zonaActiva = true;
    [SerializeField] private bool mostrarIndicador = true;
    [SerializeField, Range(0f, 1f)] private float opacidadZona = 0.24f;
    [SerializeField] private UnityEvent alPresionar = new UnityEvent();
    [SerializeField] private UnityEvent alSoltar = new UnityEvent();

    private Image fondoZona;
    private Text textoZona;
    private bool capturaTouch;
    private bool capturaMouse;
    private bool estaPresionado;

    /// <summary>True mientras el jugador mantiene la zona inferior presionada.</summary>
    public bool EstaPresionado => estaPresionado;

    /// <summary>Notifica true al presionar y false al soltar.</summary>
    public event Action<bool> PresionCambiada;

    private void Awake()
    {
        CrearZonaVisual();
        ActualizarVisual();
    }

    private void Update()
    {
        if (!zonaActiva)
        {
            capturaTouch = false;
            capturaMouse = false;
            CambiarEstadoPresion(false);
            ActualizarVisual();
            return;
        }

        TouchControl touch = Touchscreen.current != null
            ? Touchscreen.current.primaryTouch
            : null;

        // Mantén capturado el dedo o botón que inició la acción hasta soltarlo.
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

    /// <summary>
    /// Permite activar el control cuando el pez pique y ocultarlo al terminar.
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

        ActualizarVisual();
    }

    /// <summary>
    /// Punto de integración para RF4: pasa su proporción de tiempo en zona
    /// segura y RF5 decide si mostrar la captura o emitir el escape.
    /// </summary>
    public bool ResolverIntentoCaptura(
        PezData pez,
        CanaData cana,
        float porcentajeControlado,
        int oro,
        out CapturaEvaluacion evaluacion)
    {
        CapturaResultUI interfazResultado = GetComponent<CapturaResultUI>();
        if (interfazResultado == null)
            throw new InvalidOperationException("Falta CapturaResultUI en el mismo objeto que TensionSystem.");

        SetZonaActiva(false);
        return interfazResultado.ResolverIntentoCaptura(
            pez,
            cana,
            porcentajeControlado,
            oro,
            out evaluacion);
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

        ActualizarVisual();
    }

    private void CrearZonaVisual()
    {
        var canvasObject = new GameObject(
            "Canvas Zona de Tensión",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Mantiene la zona por debajo de ventanas modales como el resultado de captura.
        canvas.sortingOrder = 20;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject(
            "Botón Mitad Inferior",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        fondoZona = panel.GetComponent<Image>();
        fondoZona.raycastTarget = false;

        var label = new GameObject(
            "Instrucción de tensión",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        label.transform.SetParent(panel.transform, false);

        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.05f, 0.12f);
        labelRect.anchorMax = new Vector2(0.95f, 0.88f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        textoZona = label.GetComponent<Text>();
        textoZona.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoZona.fontSize = 30;
        textoZona.fontStyle = FontStyle.Bold;
        textoZona.alignment = TextAnchor.MiddleCenter;
        textoZona.color = Color.white;
        textoZona.resizeTextForBestFit = true;
        textoZona.resizeTextMinSize = 16;
        textoZona.resizeTextMaxSize = 30;
        textoZona.raycastTarget = false;
    }

    private void ActualizarVisual()
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

    private void OnDisable()
    {
        capturaTouch = false;
        capturaMouse = false;
        CambiarEstadoPresion(false);
    }
}
