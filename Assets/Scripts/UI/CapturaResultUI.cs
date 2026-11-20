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
    public int RecompensaBase { get; }
    public int Oro { get; }
    public int RecompensaEspecialInvasora { get; }
    public bool EspecieInvasora => Pez.esInvasora;
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
        RecompensaEspecialInvasora = pez.esInvasora ? Math.Max(0, pez.bonoMonetarioInvasora) : 0;
        RecompensaBase = RecompensaCaptura.Calcular(pez, PesoKg, LongitudCm);
        Oro = RecompensaBase + RecompensaEspecialInvasora;
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
    private Text nombre, rareza, peso, largo, oro, recompensaBase, bonoInvasora, notaCaptura;
    private GameObject avisoNuevaEspecie;
    private GameObject avisoInvasora;
    private Text mensajeAvisoInvasora;
    private CapturaResultado capturaPendienteAvisoInvasora;
    private Coroutine secuenciaAvisoInvasora;
    private GameObject insigniaInvasora;
    private CanvasGroup grupoInsigniaInvasora;
    private RectTransform rectInsigniaInvasora;
    private Coroutine animacionInvasora;
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
        // Evitar que una segunda llamada a Mostrar reemplace el aviso previo a la ficha.
        if (capturaPendienteAvisoInvasora != null) return;
        Construir();
        if (resultado.EspecieInvasora)
        {
            capturaPendienteAvisoInvasora = resultado;
            mensajeAvisoInvasora.text = "¡Esta especie es invasora!\nTienes un bonus de +$ " +
                resultado.RecompensaEspecialInvasora.ToString(cultura) + " por la captura.";
            avisoInvasora.SetActive(true);
            ventana.SetActive(true);
            secuenciaAvisoInvasora = StartCoroutine(MostrarFichaDespuesDelAviso(resultado));
            return;
        }

        MostrarFichaCaptura(resultado);
    }

    private void MostrarFichaCaptura(CapturaResultado resultado)
    {
        ResultadoActual = resultado;
        nombre.text = resultado.Pez.nombreEspecie;
        // Restablecer el estado visual evita que una captura previa o el tema
        // de la zona deje el retrato transparente/tintado en el siguiente popup.
        foto.overrideSprite = null;
        foto.sprite = resultado.Pez.foto;
        foto.type = Image.Type.Simple;
        foto.preserveAspect = true;
        foto.color = Color.white;
        foto.canvasRenderer.SetAlpha(1f);
        foto.enabled = foto.sprite != null;
        foto.SetAllDirty();
        if (foto.sprite == null)
            Debug.LogError($"La especie '{resultado.Pez.nombreEspecie}' no tiene una imagen asignada.", resultado.Pez);
        rareza.text = NombreRareza(resultado.Pez.rareza);
        TemaZonaUI.Colorear(rareza, ColorRareza(resultado.Pez.rareza));
        peso.text = resultado.PesoKg.ToString("0.##", cultura) + " kg";
        largo.text = (resultado.LongitudCm / 100f).ToString("0.###", cultura) + " m";
        oro.text = "+$ " + resultado.Oro.ToString(cultura);
        var zona = FisheryManager.Instance != null ? FisheryManager.Instance.ZonaActual : null;
        notaCaptura.text = "CAPTURA REGISTRADA · " + (zona != null ? zona.nombreZona.ToUpperInvariant() : "AMAZONAS") +
            (resultado.EspecieInvasora ? " · CONTROL DE INVASORA" : "");
        bool invasora = resultado.EspecieInvasora;
        recompensaBase.gameObject.SetActive(invasora);
        bonoInvasora.gameObject.SetActive(invasora);
        insigniaInvasora.SetActive(invasora);
        if (animacionInvasora != null) StopCoroutine(animacionInvasora);
        if (invasora)
        {
            recompensaBase.text = "PESCA +$ " + resultado.RecompensaBase.ToString(cultura);
            bonoInvasora.text = "BONO DE CONTROL +$ " + resultado.RecompensaEspecialInvasora.ToString(cultura);
            animacionInvasora = StartCoroutine(RevelarBonoInvasora(resultado.RecompensaEspecialInvasora));
        }
        avisoNuevaEspecie.SetActive(resultado.NuevaEspecie);
        ventana.SetActive(true);
    }

    private System.Collections.IEnumerator MostrarFichaDespuesDelAviso(CapturaResultado resultado)
    {
        // Mantener el aviso el tiempo suficiente para leerlo antes de abrir la ficha habitual.
        yield return new WaitForSecondsRealtime(3f);
        secuenciaAvisoInvasora = null;
        if (capturaPendienteAvisoInvasora != resultado) yield break;

        capturaPendienteAvisoInvasora = null;
        avisoInvasora.SetActive(false);
        MostrarFichaCaptura(resultado);
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
        var titulo = Etiqueta("Titulo", fondo, "", 36, EstiloExpedicion.Tinta, .04f, .83f, .73f, .93f, TextAnchor.MiddleLeft);
        TemaZonaUI.Rotular(titulo, "Una historia más del río", "Una historia más del mar");
        avisoNuevaEspecie = EstiloExpedicion.Texto(fondo, "Nueva especie", "¡Nueva especie!", 20,
            EstiloExpedicion.Verde, .75f, .85f, .96f, .93f, false, TextAnchor.MiddleCenter).gameObject;
        var insignia = Panel("Distintivo de conservación", fondo, Hex("963C2B"), .70f, .75f, .96f, .835f);
        var imagenInsignia = insignia.GetComponent<Image>();
        TemaZonaUI.Excluir(imagenInsignia);
        EstiloExpedicion.Borde(imagenInsignia, Hex("DDAA5D"), 1.5f);
        var textoInsignia = Etiqueta("Etiqueta de conservación", insignia, "CONTROL DE INVASORA", 14,
            Hex("FFF2D8"), .04f, .04f, .96f, .96f, TextAnchor.MiddleCenter);
        TemaZonaUI.Excluir(textoInsignia);
        textoInsignia.color = Hex("FFF2D8");
        insigniaInvasora = insignia.gameObject;
        grupoInsigniaInvasora = insignia.gameObject.AddComponent<CanvasGroup>();
        rectInsigniaInvasora = insignia;
        insigniaInvasora.SetActive(false);
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
        oro.rectTransform.anchorMin = new Vector2(.055f, .31f);
        oro.rectTransform.anchorMax = new Vector2(.95f, .60f);
        var panelGanancia = oro.transform.parent;
        recompensaBase = EstiloExpedicion.Texto(panelGanancia, "Recompensa de pesca", "", 12,
            Hex("526358"), .055f, .16f, .95f, .31f, false, TextAnchor.MiddleLeft);
        bonoInvasora = EstiloExpedicion.Texto(panelGanancia, "Bono invasora", "", 13,
            Hex("886119"), .055f, .015f, .95f, .17f, false, TextAnchor.MiddleLeft);
        EstiloExpedicion.Tipografia(bonoInvasora, 12, Hex("886119"));
        EstiloExpedicion.Tipografia(recompensaBase, 12, Hex("526358"));
        recompensaBase.gameObject.SetActive(false);
        bonoInvasora.gameObject.SetActive(false);
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

        var fondoAviso = EstiloExpedicion.Panel(ventana.transform, "Aviso de especie invasora",
            EstiloExpedicion.Velo, 0, 0, 1, 1);
        fondoAviso.raycastTarget = true;
        var bloqueoAviso = fondoAviso.gameObject.AddComponent<CanvasGroup>();
        bloqueoAviso.blocksRaycasts = true;
        bloqueoAviso.interactable = false;
        var panelAviso = EstiloExpedicion.Panel(fondoAviso.transform, "Tarjeta de aviso invasora",
            EstiloExpedicion.Papel, .08f, .32f, .92f, .68f);
        EstiloExpedicion.Borde(panelAviso, Hex("963C2B"), 2.5f);
        var acentoAviso = EstiloExpedicion.Panel(panelAviso.transform, "Acento de control",
            Hex("963C2B"), .035f, .15f, .052f, .85f);
        TemaZonaUI.Excluir(acentoAviso);
        EstiloExpedicion.Texto(panelAviso.transform, "Titulo aviso invasora", "¡ESPECIE INVASORA!", 28,
            Hex("963C2B"), .09f, .67f, .94f, .91f, true, TextAnchor.MiddleCenter);
        mensajeAvisoInvasora = EstiloExpedicion.Texto(panelAviso.transform, "Mensaje aviso invasora", "", 25,
            EstiloExpedicion.Tinta, .08f, .30f, .94f, .68f, false, TextAnchor.MiddleCenter);
        EstiloExpedicion.Texto(panelAviso.transform, "Siguiente paso aviso invasora",
            "A continuación verás los datos de tu captura.", 16, EstiloExpedicion.Secundario,
            .08f, .10f, .94f, .27f, false, TextAnchor.MiddleCenter);
        avisoInvasora = fondoAviso.gameObject;
        avisoInvasora.SetActive(false);
        ventana.SetActive(false);
    }

    private System.Collections.IEnumerator RevelarBonoInvasora(int bono)
    {
        const float entrada = .30f;
        grupoInsigniaInvasora.alpha = 0;
        rectInsigniaInvasora.localScale = Vector3.one * .78f;
        float tiempo = 0;
        while (tiempo < entrada)
        {
            tiempo += Time.unscaledDeltaTime;
            float avance = Mathf.Clamp01(tiempo / entrada);
            float suave = 1f - Mathf.Pow(1f - avance, 3f);
            grupoInsigniaInvasora.alpha = suave;
            rectInsigniaInvasora.localScale = Vector3.one * Mathf.Lerp(.78f, 1f, suave);
            int mostrado = Mathf.RoundToInt(bono * suave);
            bonoInvasora.text = "BONO DE CONTROL +$ " + mostrado.ToString(cultura);
            yield return null;
        }
        grupoInsigniaInvasora.alpha = 1;
        rectInsigniaInvasora.localScale = Vector3.one;
        bonoInvasora.text = "BONO DE CONTROL +$ " + bono.ToString(cultura);
        animacionInvasora = null;
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
        TemaZonaUI.Colorear(rect.gameObject.AddComponent<Image>(), color);
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
