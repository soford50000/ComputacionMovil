using UnityEngine;

/// <summary>Registro local del nombre en el primer inicio, sin alterar la partida guardada.</summary>
[DisallowMultipleComponent]
public sealed class PerfilJugador : MonoBehaviour
{
    private const string ClaveNombre = "RoadToAmazonas.NombreJugador.v1";
    public const int LongitudMaximaNombre = 20;
    public static string NombreJugador => PlayerPrefs.GetString(ClaveNombre, string.Empty);

    private TensionSystem pesca;
    private bool pendiente, pescaBloqueada, pescaEstabaHabilitada;
    private GameObject ventana;
    private UnityEngine.UI.InputField entradaNombre;
    private UnityEngine.UI.Button botonContinuar;
    private UnityEngine.UI.Text aviso;

    private void Awake()
    {
        pesca = GetComponent<TensionSystem>();
        pendiente = !IntentarNormalizarNombre(NombreJugador, out _);
        if (pendiente) BloquearPesca();
    }

    private void Start()
    {
        if (pendiente) Construir();
    }

    public static bool IntentarNormalizarNombre(string entrada, out string nombre)
    {
        nombre = (entrada ?? string.Empty).Trim();
        if (nombre.Length == 0 || nombre.Length > LongitudMaximaNombre) return false;
        for (int i = 0; i < nombre.Length; i++)
            if (char.IsControl(nombre[i])) return false;
        return true;
    }

    private void Construir()
    {
        ventana = new GameObject("Canvas Bienvenida del Pescador", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        ventana.transform.SetParent(transform, false);
        var canvas = ventana.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var escala = ventana.GetComponent<UnityEngine.UI.CanvasScaler>();
        escala.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280, 720);
        escala.matchWidthOrHeight = 0.5f;

        var velo = EstiloExpedicion.Panel(ventana.transform, "Fondo de bienvenida", EstiloExpedicion.Velo, 0, 0, 1, 1);
        velo.raycastTarget = true;
        var tarjeta = EstiloExpedicion.Panel(velo.transform, "Tu primera expedición", EstiloExpedicion.Papel, .15f, .12f, .85f, .88f);
        EstiloExpedicion.Enmarcar(tarjeta.rectTransform);
        EstiloExpedicion.Texto(tarjeta.transform, "Bienvenida", "¡Bienvenido a la expedición!", 32,
            EstiloExpedicion.Tinta, .06f, .80f, .94f, .92f, true, TextAnchor.MiddleCenter);
        EstiloExpedicion.Texto(tarjeta.transform, "Pregunta", "¿Cómo te llamas, pescador?", 25,
            EstiloExpedicion.Tinta, .07f, .65f, .93f, .76f, false, TextAnchor.MiddleCenter);

        var campo = EstiloExpedicion.Panel(tarjeta.transform, "Nombre del jugador", Color.white, .1f, .44f, .9f, .60f);
        campo.raycastTarget = true;
        EstiloExpedicion.Borde(campo, EstiloExpedicion.Dorado, 2f);
        entradaNombre = campo.gameObject.AddComponent<UnityEngine.UI.InputField>();
        entradaNombre.targetGraphic = campo;
        entradaNombre.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
        entradaNombre.characterLimit = LongitudMaximaNombre;
        entradaNombre.contentType = UnityEngine.UI.InputField.ContentType.Standard;
        entradaNombre.keyboardType = TouchScreenKeyboardType.Default;
        var texto = EstiloExpedicion.Texto(campo.transform, "Texto", "", 26, EstiloExpedicion.Tinta,
            .035f, .08f, .965f, .92f);
        // Fuente legible para escribir nombres, incluidos acentos; no interpreta etiquetas.
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.supportRichText = false;
        texto.resizeTextForBestFit = false;
        entradaNombre.textComponent = texto;
        entradaNombre.placeholder = EstiloExpedicion.Texto(campo.transform, "Ejemplo", "Escribe tu nombre", 22,
            EstiloExpedicion.Secundario, .035f, .08f, .965f, .92f);
        aviso = EstiloExpedicion.Texto(tarjeta.transform, "Ayuda del nombre", "De 1 a 20 caracteres.", 16,
            EstiloExpedicion.Secundario, .1f, .33f, .9f, .42f, false, TextAnchor.MiddleCenter);
        botonContinuar = EstiloExpedicion.Accion(tarjeta.transform, "Guardar nombre", "COMENZAR A PESCAR",
            .2f, .12f, .8f, .28f, ConfirmarNombre, true);
        botonContinuar.interactable = false;
        entradaNombre.onValueChanged.AddListener(ActualizarNombre);
    }

    private void ActualizarNombre(string valor)
    {
        bool valido = IntentarNormalizarNombre(valor, out _);
        botonContinuar.interactable = valido;
        aviso.text = valido ? "¡Tu expedición está a punto de comenzar!" : "Escribe un nombre de 1 a 20 caracteres.";
    }

    private void ConfirmarNombre()
    {
        if (!pendiente || !IntentarNormalizarNombre(entradaNombre.text, out string nombre)) return;
        PlayerPrefs.SetString(ClaveNombre, nombre);
        PlayerPrefs.Save();
        pendiente = false;
        entradaNombre.DeactivateInputField();
        ventana.SetActive(false);
        RestaurarPesca();
    }

    private void BloquearPesca()
    {
        // El lanzamiento lee input directamente: un panel con raycast no basta.
        if (pesca == null || pescaBloqueada) return;
        pescaEstabaHabilitada = pesca.enabled;
        pesca.enabled = false;
        pescaBloqueada = true;
    }

    private void RestaurarPesca()
    {
        if (pesca != null && pescaBloqueada) pesca.enabled = pescaEstabaHabilitada;
        pescaBloqueada = false;
    }

    private void OnEnable()
    {
        if (!pendiente) return;
        BloquearPesca();
        if (ventana != null) ventana.SetActive(true);
    }

    private void OnDisable()
    {
        if (ventana != null) ventana.SetActive(false);
        RestaurarPesca();
    }

    private void OnDestroy()
    {
        RestaurarPesca();
        if (ventana != null) Destroy(ventana);
    }
}
