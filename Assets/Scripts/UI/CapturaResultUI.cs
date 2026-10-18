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
    public bool NuevaEspecie { get; }

    public CapturaResultado(PezData pez, bool nuevaEspecie = false)
    {
        if (pez == null) throw new ArgumentNullException(nameof(pez));
        ValidarRango(pez.pesoMinimo, pez.pesoMaximo);
        ValidarRango(pez.longitudMinima, pez.longitudMaxima);
        Pez = pez;
        NuevaEspecie = nuevaEspecie;
        PesoKg = UnityEngine.Random.Range(pez.pesoMinimo, pez.pesoMaximo);
        LongitudCm = UnityEngine.Random.Range(pez.longitudMinima, pez.longitudMaxima);
        Oro = RecompensaCaptura.Calcular(pez, PesoKg, LongitudCm);
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
    public UnityEvent alRecoger = new UnityEvent();
    public UnityEvent alPescarDeNuevo = new UnityEvent();
    public UnityEvent alEscapar = new UnityEvent();
    public event Action<CapturaResultado> CapturaRecogida;
    public event Action<CapturaEvaluacion> IntentoEvaluado;
    public CapturaResultado ResultadoActual { get; private set; }

    private GameObject ventana;
    private Text nombre, rareza, peso, largo, oro, notaCaptura;
    private GameObject avisoNuevaEspecie;
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
        oro.text = "+$ " + resultado.Oro.ToString(cultura);
        var zona = FisheryManager.Instance != null ? FisheryManager.Instance.ZonaActual : null;
        notaCaptura.text = "CAPTURA REGISTRADA · " + (zona != null ? zona.nombreZona.ToUpperInvariant() : "AMAZONAS");
        avisoNuevaEspecie.SetActive(resultado.NuevaEspecie);
        ventana.SetActive(true);
    }

    /// <summary>
    /// Resuelve RF5 con los datos del pez, la caña equipada y el rendimiento
    /// de zona segura que entrega RF4. Muestra el popup solo si la captura tuvo éxito.
    /// </summary>
    /// <param name="porcentajeControlado">Proporción en zona segura, entre 0 y 1.</param>
    public bool ResolverIntentoCaptura(
        PezData pez,
        CanaData cana,
        float porcentajeControlado,
        out CapturaEvaluacion evaluacion)
    {
        evaluacion = CapturaCalculoRF5.Evaluar(pez, cana, porcentajeControlado);
        IntentoEvaluado?.Invoke(evaluacion);

        if (!evaluacion.Capturada)
        {
            alEscapar.Invoke();
            return false;
        }

        bool nuevaEspecie = false;
        if (CatalogoManager.Instance == null)
            Debug.LogError("Falta CatalogoManager: no se pudo registrar la especie capturada.", this);
        else
            nuevaEspecie = CatalogoManager.Instance.RegistrarCaptura(pez);

        Mostrar(new CapturaResultado(pez, nuevaEspecie));
        return true;
    }

    private void Confirmar(bool volverAPescar)
    {
        if (ResultadoActual == null) return;
        var resultado = ResultadoActual;
        if (EconomiaManager.Instance != null)
            EconomiaManager.Instance.AgregarOro(resultado.Oro);
        else
            Debug.LogError("Falta EconomiaManager: no se pudo acreditar la recompensa de la captura.", this);
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
        if (pez != null) Mostrar(new CapturaResultado(pez));
    }

    private void Construir()
    {
        if (ventana != null) return;
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
        var sombra = Panel("Fondo modal", ventana.transform, EstiloExpedicion.Velo, 0, 0, 1, 1);
        var fondo = Panel("Cuaderno de captura", sombra, EstiloExpedicion.Papel, .04f, .08f, .96f, .92f);
        EstiloExpedicion.Enmarcar(fondo);
        EstiloExpedicion.PrepararModal(ventana, fondo);
        Etiqueta("Titulo", fondo, "Una historia más del río", 36, EstiloExpedicion.Tinta, .04f, .83f, .73f, .93f, TextAnchor.MiddleLeft);
        avisoNuevaEspecie = EstiloExpedicion.Texto(fondo, "Nueva especie", "¡Nueva especie!", 20,
            EstiloExpedicion.Verde, .75f, .85f, .96f, .93f, false, TextAnchor.MiddleCenter).gameObject;
        var imagen = Panel("Pez", fondo, Color.white, .025f, .20f, .49f, .77f);
        foto = imagen.GetComponent<Image>();
        foto.preserveAspect = true;
        foto.raycastTarget = false;
        var tarjeta = Panel("Ficha", fondo, EstiloExpedicion.Papel, .51f, .20f, .96f, .77f);
        nombre = Etiqueta("Nombre especie", tarjeta, "", 32, EstiloExpedicion.Tinta, .02f, .73f, .98f, .98f, TextAnchor.MiddleLeft);
        rareza = Campo(tarjeta, "Rareza", .02f, .43f, .48f, .70f);
        largo = Campo(tarjeta, "Longitud", .52f, .43f, .98f, .70f);
        peso = Campo(tarjeta, "Peso", .02f, .08f, .48f, .36f);
        oro = Campo(tarjeta, "Ganancia", .52f, .08f, .98f, .36f);
        EstiloExpedicion.Tipografia(oro, 34, Hex("886119"));
        var nota = EstiloExpedicion.Texto(fondo, "Nota de captura", "CAPTURA REGISTRADA · AMAZONAS", 13, EstiloExpedicion.Secundario, .04f, .14f, .47f, .20f);
        notaCaptura = nota;
        var recoger = Boton(fondo, "Recoger", EstiloExpedicion.Verde, EstiloExpedicion.Papel, .51f, .05f, .70f, .15f, () => Confirmar(false));
        var repetir = Boton(fondo, "Pescar de nuevo", EstiloExpedicion.Dorado, EstiloExpedicion.Tinta, .72f, .05f, .96f, .15f, () => Confirmar(true));
        var disposicion = fondo.gameObject.AddComponent<DisposicionExpedicion>();
        disposicion.Agregar(imagen, .08f, .48f, .92f, .78f);
        disposicion.Agregar(tarjeta, .05f, .17f, .95f, .47f);
        disposicion.Agregar(nota.rectTransform, .04f, .14f, .96f, .17f);
        disposicion.Agregar(recoger, .04f, .05f, .48f, .13f);
        disposicion.Agregar(repetir, .52f, .05f, .96f, .13f);
        ventana.SetActive(false);
    }

    private Text Campo(Transform padre, string titulo, float x0, float y0, float x1, float y1)
    {
        var interior = Panel(titulo, padre, EstiloExpedicion.PapelOscuro, x0, y0, x1, y1);
        Etiqueta("Etiqueta", interior, titulo, 16, EstiloExpedicion.Secundario, .055f, .58f, .95f, .95f, TextAnchor.MiddleLeft);
        return Etiqueta("Valor", interior, "", 25, EstiloExpedicion.Tinta, .055f, .04f, .95f, .60f, TextAnchor.MiddleLeft);
    }

    private RectTransform Boton(Transform padre, string texto, Color color, Color tinta, float x0, float y0, float x1, float y1, UnityAction accion)
    {
        var borde = Panel(texto, padre, Madera, x0, y0, x1, y1);
        var interior = Panel("Boton", borde, color, .015f, .08f, .985f, .94f);
        var boton = interior.gameObject.AddComponent<Button>();
        boton.targetGraphic = interior.GetComponent<Image>();
        boton.onClick.AddListener(accion);
        EstiloExpedicion.Boton(boton);
        Etiqueta("Texto", interior, texto, 19, tinta, .02f, .05f, .98f, .95f);
        return borde;
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
        EstiloExpedicion.Tipografia(texto, tamano, color, nombre == "Titulo" || nombre == "Nombre especie");
        texto.text = contenido; texto.alignment = alineacion;
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
