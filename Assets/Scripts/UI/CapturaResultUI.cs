using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Valores inmutables de una captura. Las longitudes de PezData están en cm.</summary>
public sealed class CapturaResultado
{
    public PezData Pez { get; }
    public float PesoKg { get; }
    public float LongitudCm { get; }
    public int Oro { get; }

    public CapturaResultado(PezData pez, int oro)
    {
        if (pez == null) throw new ArgumentNullException(nameof(pez));
        ValidarRango(pez.pesoMinimo, pez.pesoMaximo);
        ValidarRango(pez.longitudMinima, pez.longitudMaxima);
        if (oro < 0) throw new ArgumentOutOfRangeException(nameof(oro));
        Pez = pez;
        PesoKg = UnityEngine.Random.Range(pez.pesoMinimo, pez.pesoMaximo);
        LongitudCm = UnityEngine.Random.Range(pez.longitudMinima, pez.longitudMaxima);
        Oro = oro;
    }

    private static void ValidarRango(float min, float max)
    {
        if (float.IsNaN(min) || float.IsNaN(max) || float.IsInfinity(min) ||
            float.IsInfinity(max) || min < 0 || max < min)
            throw new ArgumentException("El pez tiene un rango de medidas inválido.");
    }
}

/// <summary>RF6. Construye el Canvas al jugar; Mostrar no vuelve a sortear las medidas.</summary>
public class CapturaResultUI : MonoBehaviour
{
    [Tooltip("Fuente opcional para reproducir el estilo pixel del diseño.")]
    [SerializeField] private Font fuentePixel;
    public UnityEvent alRecoger = new UnityEvent();
    public UnityEvent alPescarDeNuevo = new UnityEvent();
    public UnityEvent alEscapar = new UnityEvent();
    public event Action<CapturaResultado> CapturaRecogida;
    public event Action<CapturaEvaluacion> IntentoEvaluado;
    public CapturaResultado ResultadoActual { get; private set; }

    private GameObject ventana;
    private Text nombre, rareza, peso, largo, oro;
    private Image foto;
    private CapturaResultado ultimoRecogido;
    private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly Color Madera = Hex("2C1810"), Borde = Hex("8B5A2B"), Crema = Hex("F2E8CF"), Dorado = Hex("F2B632");

    private void Awake() { Construir(); }

    public void Mostrar(CapturaResultado resultado)
    {
        if (resultado == null) throw new ArgumentNullException(nameof(resultado));
        if (ReferenceEquals(resultado, ultimoRecogido)) return;
        // No reemplazar una captura pendiente de recoger.
        if (ResultadoActual != null && !ReferenceEquals(ResultadoActual, resultado)) return;
        Construir();
        ResultadoActual = resultado;
        nombre.text = resultado.Pez.nombreEspecie;
        foto.sprite = resultado.Pez.foto;
        foto.enabled = foto.sprite != null;
        rareza.text = NombreRareza(resultado.Pez.rareza);
        rareza.color = ColorRareza(resultado.Pez.rareza);
        peso.text = resultado.PesoKg.ToString("0.##", cultura) + " kg";
        largo.text = (resultado.LongitudCm / 100f).ToString("0.###", cultura) + " m";
        oro.text = "+" + resultado.Oro.ToString(cultura);
        ventana.SetActive(true);
    }

    /// <summary>
    /// Resuelve RF5 con los datos del pez, la caña equipada y el rendimiento
    /// de zona segura que entrega RF4. Muestra el popup solo si la captura tuvo éxito.
    /// </summary>
    /// <param name="porcentajeControlado">Proporción en zona segura, entre 0 y 1.</param>
    /// <param name="oro">Recompensa que se mostrará si la captura resulta exitosa.</param>
    public bool ResolverIntentoCaptura(
        PezData pez,
        CanaData cana,
        float porcentajeControlado,
        int oro,
        out CapturaEvaluacion evaluacion)
    {
        evaluacion = CapturaCalculoRF5.Evaluar(pez, cana, porcentajeControlado);
        IntentoEvaluado?.Invoke(evaluacion);

        if (!evaluacion.Capturada)
        {
            alEscapar.Invoke();
            return false;
        }

        Mostrar(new CapturaResultado(pez, oro));
        return true;
    }

    private void Confirmar(bool volverAPescar)
    {
        if (ResultadoActual == null) return;
        var resultado = ResultadoActual;
        ultimoRecogido = resultado;
        ResultadoActual = null;
        ventana.SetActive(false);
        CapturaRecogida?.Invoke(resultado);
        alRecoger.Invoke();
        if (volverAPescar) alPescarDeNuevo.Invoke();
    }

    [ContextMenu("Probar ventana de captura (en Play)")]
    private void ProbarCaptura()
    {
        if (!Application.isPlaying) { Debug.Log("Activa Play para probar la ventana.", this); return; }
        if (ResultadoActual != null) return;
        var gestor = FisheryManager.Instance;
        if (gestor == null) return;
        var pez = gestor.GenerarPezAleatorio();
        // Recompensa de prueba: la recompensa real la proporciona el sistema de pesca.
        if (pez != null) Mostrar(new CapturaResultado(pez, 500));
    }

    private void Construir()
    {
        if (ventana != null) return;
        if (fuentePixel == null) fuentePixel = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ventana = new GameObject("Canvas Resultado Captura", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ventana.transform.SetParent(transform, false);
        var canvas = ventana.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = ventana.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var eventos = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventos.transform.SetParent(transform, false);
        }
        var sombra = Panel("Fondo modal", ventana.transform, new Color(0, 0, 0, 0.75f), 0, 0, 1, 1);
        var marco = Panel("Marco madera", sombra, Borde, .025f, .16f, .975f, .84f);
        var fondo = Panel("Interior", marco, Madera, .005f, .012f, .995f, .988f);
        Panel("Linea superior", fondo, Dorado, .27f, .91f, .73f, .916f);
        Etiqueta("Titulo", fondo, "¡ATRAPADO!", 36, Crema, .2f, .8f, .8f, .91f);
        Panel("Linea inferior", fondo, Dorado, .27f, .79f, .73f, .796f);
        var imagen = Panel("Pez", fondo, Color.white, .03f, .12f, .4f, .74f);
        foto = imagen.GetComponent<Image>();
        foto.preserveAspect = true;
        foto.raycastTarget = false;
        var tarjetaBorde = Panel("Borde ficha", fondo, Borde, .43f, .06f, .98f, .74f);
        var tarjeta = Panel("Ficha", tarjetaBorde, Crema, .009f, .018f, .991f, .982f);
        nombre = Etiqueta("Nombre especie", tarjeta, "", 29, Madera, .03f, .77f, .97f, .97f);
        rareza = Campo(tarjeta, "RAREZA", .04f, .51f, .49f, .75f);
        largo = Campo(tarjeta, "LARGO", .51f, .51f, .96f, .75f);
        peso = Campo(tarjeta, "PESO", .04f, .24f, .49f, .48f);
        oro = Campo(tarjeta, "ORO", .51f, .24f, .96f, .48f);
        oro.color = Hex("B37D00");
        Boton(tarjeta, "RECOGER", Dorado, Madera, .04f, .04f, .49f, .19f, () => Confirmar(false));
        Boton(tarjeta, "PESCAR DE NUEVO", Hex("E8743B"), Color.white, .51f, .04f, .96f, .19f, () => Confirmar(true));
        ventana.SetActive(false);
    }

    private Text Campo(Transform padre, string titulo, float x0, float y0, float x1, float y1)
    {
        var borde = Panel(titulo, padre, Borde, x0, y0, x1, y1);
        var interior = Panel("Interior", borde, Color.white, .008f, .025f, .992f, .975f);
        Etiqueta("Etiqueta", interior, titulo, 13, Color.gray, .055f, .52f, .95f, .93f, TextAnchor.MiddleLeft);
        return Etiqueta("Valor", interior, "", 22, Madera, .055f, .05f, .95f, .57f, TextAnchor.MiddleLeft);
    }

    private void Boton(Transform padre, string texto, Color color, Color tinta, float x0, float y0, float x1, float y1, UnityAction accion)
    {
        var borde = Panel(texto, padre, Madera, x0, y0, x1, y1);
        var interior = Panel("Boton", borde, color, .015f, .08f, .985f, .94f);
        var boton = interior.gameObject.AddComponent<Button>();
        boton.targetGraphic = interior.GetComponent<Image>();
        boton.onClick.AddListener(accion);
        Etiqueta("Texto", interior, texto, 19, tinta, .02f, .05f, .98f, .95f);
    }

    private static RectTransform Rect(string nombre, Transform padre, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform Panel(string nombre, Transform padre, Color color, float x0, float y0, float x1, float y1)
    {
        var rect = Rect(nombre, padre, x0, y0, x1, y1);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private Text Etiqueta(string nombre, Transform padre, string contenido, int tamano, Color color, float x0, float y0, float x1, float y1, TextAnchor alineacion = TextAnchor.MiddleCenter)
    {
        var texto = Rect(nombre, padre, x0, y0, x1, y1).gameObject.AddComponent<Text>();
        texto.font = fuentePixel; texto.text = contenido; texto.fontSize = tamano;
        texto.fontStyle = FontStyle.Bold; texto.color = color; texto.alignment = alineacion;
        texto.resizeTextForBestFit = true; texto.resizeTextMinSize = 10; texto.resizeTextMaxSize = tamano;
        texto.raycastTarget = false;
        return texto;
    }

    private static Color Hex(string valor) { ColorUtility.TryParseHtmlString("#" + valor, out var color); return color; }
    private static string NombreRareza(Rareza valor)
    {
        switch (valor) {
            case Rareza.Comun: return "Común";
            case Rareza.PocoComun: return "Poco común";
            case Rareza.Rara: return "Rara";
            case Rareza.Epica: return "Épica";
            case Rareza.Legendaria: return "Legendaria";
            default: return valor.ToString();
        }
    }
    private static Color ColorRareza(Rareza valor)
    {
        switch (valor) {
            case Rareza.Comun: return Hex("526358");
            case Rareza.PocoComun: return Hex("307539");
            case Rareza.Rara: return Hex("2864A6");
            case Rareza.Epica: return Hex("8541B0");
            case Rareza.Legendaria: return Hex("B37D00");
            default: return Madera;
        }
    }
}
