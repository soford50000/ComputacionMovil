using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RF9: mantiene y guarda el saldo local del jugador.</summary>
[DisallowMultipleComponent]
public sealed class EconomiaManager : MonoBehaviour
{
    private const string ClaveSaldo = "RoadToAmazonas.SaldoOro.v1";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly Color Madera = ColorHex("#39251B");
    private static readonly Color Dorado = ColorHex("#F2B632");

    [SerializeField, Min(0)] private int saldoInicial;
    public static EconomiaManager Instance { get; private set; }
    public int Saldo { get; private set; }
    public event Action<int> SaldoCambiado;

    private Text textoSaldo;
    private GameObject ventana;
    private Text textoSaldoModal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Saldo = PlayerPrefs.HasKey(ClaveSaldo)
            ? Mathf.Max(0, PlayerPrefs.GetInt(ClaveSaldo))
            : Mathf.Max(0, saldoInicial);
        GuardarSaldo();
        ConstruirInterfaz();
        ActualizarInterfaz();
    }

    public void AgregarOro(int cantidad)
    {
        if (cantidad <= 0) return;
        Saldo = (int)Math.Min(int.MaxValue, (long)Saldo + cantidad);
        GuardarSaldo();
        ActualizarInterfaz();
        SaldoCambiado?.Invoke(Saldo);
    }

    public bool IntentarGastarOro(int cantidad)
    {
        if (cantidad < 0 || Saldo < cantidad) return false;
        if (cantidad == 0) return true;
        Saldo -= cantidad;
        GuardarSaldo();
        ActualizarInterfaz();
        SaldoCambiado?.Invoke(Saldo);
        return true;
    }

    private void GuardarSaldo()
    {
        PlayerPrefs.SetInt(ClaveSaldo, Saldo);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pausado)
    {
        if (pausado) GuardarSaldo();
    }

    private void OnApplicationQuit() => GuardarSaldo();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ConstruirInterfaz()
    {
        var canvasObject = new GameObject("Canvas Dinero", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var wallet = Rect("Saldo de oro", canvasObject.transform, 0.81f, 0.91f, 0.985f, 0.985f);
        var walletImage = wallet.gameObject.AddComponent<Image>();
        walletImage.color = Madera;
        var button = wallet.gameObject.AddComponent<Button>();
        button.targetGraphic = walletImage;
        button.onClick.AddListener(AbrirVentana);
        textoSaldo = Etiqueta("Cantidad de oro", wallet, "", 22, Dorado, 0.03f, 0.05f, 0.97f, 0.95f);

        ventana = new GameObject("Ventana de dinero", typeof(RectTransform), typeof(Image));
        ventana.transform.SetParent(canvasObject.transform, false);
        var fondo = ventana.GetComponent<Image>();
        fondo.color = new Color(0.015f, 0.035f, 0.025f, 0.78f);
        Estirar(ventana.GetComponent<RectTransform>());

        var tarjeta = Rect("Tarjeta de saldo", ventana.transform, 0.25f, 0.28f, 0.75f, 0.72f);
        tarjeta.gameObject.AddComponent<Image>().color = Madera;
        Etiqueta("Titulo", tarjeta, "ORO DEL AMAZONAS", 30, Dorado, 0.04f, 0.69f, 0.96f, 0.94f);
        textoSaldoModal = Etiqueta("Saldo guardado", tarjeta, "", 42, Color.white, 0.05f, 0.36f, 0.95f, 0.68f);
        Etiqueta("Guardado local", tarjeta, "Tu saldo se guarda automáticamente en este dispositivo.",
            17, new Color(0.89f, 0.85f, 0.74f), 0.06f, 0.18f, 0.94f, 0.36f);
        var cerrar = Rect("Cerrar", tarjeta, 0.35f, 0.04f, 0.65f, 0.16f);
        var cerrarImagen = cerrar.gameObject.AddComponent<Image>();
        cerrarImagen.color = Dorado;
        var cerrarBoton = cerrar.gameObject.AddComponent<Button>();
        cerrarBoton.targetGraphic = cerrarImagen;
        cerrarBoton.onClick.AddListener(CerrarVentana);
        Etiqueta("Cerrar texto", cerrar, "VOLVER", 19, Madera, 0f, 0f, 1f, 1f);
        ventana.SetActive(false);
    }

    private void AbrirVentana()
    {
        ActualizarInterfaz();
        ventana.SetActive(true);
    }

    private void CerrarVentana() => ventana.SetActive(false);

    private void ActualizarInterfaz()
    {
        if (textoSaldo != null) textoSaldo.text = "ORO  " + Saldo.ToString("N0", Cultura);
        if (textoSaldoModal != null) textoSaldoModal.text = Saldo.ToString("N0", Cultura) + " monedas";
    }

    private static RectTransform Rect(string nombre, Transform padre, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Text Etiqueta(string nombre, Transform padre, string contenido, int tamano,
        Color color, float x0, float y0, float x1, float y1)
    {
        var texto = Rect(nombre, padre, x0, y0, x1, y1).gameObject.AddComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.fontStyle = FontStyle.Bold;
        texto.color = color;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 10;
        texto.resizeTextMaxSize = tamano;
        texto.raycastTarget = false;
        return texto;
    }

    private static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Color ColorHex(string valor)
    {
        ColorUtility.TryParseHtmlString(valor, out var color);
        return color;
    }
}
