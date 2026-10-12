using UnityEngine;
using UnityEngine.UI;

/// <summary>Menú RF7 para elegir y equipar una caña antes de pescar.</summary>
public sealed class CanaSelectorUI : MonoBehaviour
{
    [SerializeField] private CanaData[] canasDisponibles;
    [SerializeField] private CanaData canaInicial;

    private TensionSystem sistemaPesca;
    private Button botonEquipo;
    private Text textoBotonEquipo;
    private GameObject ventana;
    private Text textoAviso;
    private Image[] fondosOpciones;
    private Text[] marcasOpciones;

    private static readonly Color Madera = Hex("2C1810");
    private static readonly Color Verde = Hex("294735");
    private static readonly Color VerdeClaro = Hex("456B48");
    private static readonly Color Dorado = Hex("D4AA5E");
    private static readonly Color Crema = Hex("F4E9C8");

    private void Awake()
    {
        sistemaPesca = GetComponent<TensionSystem>();
        if (sistemaPesca == null)
        {
            Debug.LogError("CanaSelectorUI necesita TensionSystem en el mismo objeto.", this);
            enabled = false;
            return;
        }

        // La escena abierta puede conservar una lista anterior en memoria.
        // El catálogo incluido en el juego es la fuente de las opciones actuales.
        var catalogo = Resources.Load<CatalogoCanas>("CatalogoCanas");
        if (catalogo != null)
            canasDisponibles = catalogo.canas;

        CrearInterfaz();
        EquiparCanaInicial();
    }

    private void CrearInterfaz()
    {
        var canvasObject = new GameObject(
            "Canvas Equipo de Pesca",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        CrearBotonDeEquipo(canvasObject.transform);
        CrearVentana(canvasObject.transform);
    }

    private void CrearBotonDeEquipo(Transform padre)
    {
        botonEquipo = CrearBoton("Abrir equipo", padre, Verde, new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f),
            new Vector2(250f, 58f), AlternarVentana);
        textoBotonEquipo = CrearTexto("Caña equipada", botonEquipo.transform,
            "EQUIPO", 19, Crema, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
    }

    private void CrearVentana(Transform padre)
    {
        ventana = new GameObject("Menú de cañas", typeof(RectTransform), typeof(Image));
        ventana.transform.SetParent(padre, false);
        var ventanaRect = ventana.GetComponent<RectTransform>();
        ventanaRect.anchorMin = Vector2.zero;
        ventanaRect.anchorMax = Vector2.one;
        ventanaRect.offsetMin = Vector2.zero;
        ventanaRect.offsetMax = Vector2.zero;
        ventana.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.03f, 0.82f);

        var panel = CrearPanel("Panel de cañas", ventana.transform, Madera,
            new Vector2(0.07f, 0.09f), new Vector2(0.93f, 0.91f));
        CrearTexto("Título", panel, "EQUIPO DE PESCA", 34, Crema,
            new Vector2(0.06f, 0.82f), new Vector2(0.78f, 0.96f));
        CrearTexto("Instrucción", panel,
            "Elige una caña. Abu Garcia Vengeance está equipada por defecto.",
            17, Hex("DCC89B"), new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.83f));

        CrearBoton("Cerrar menú de cañas", panel, Hex("6B3B25"),
            new Vector2(0.84f, 0.84f), new Vector2(0.84f, 0.84f),
            new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero, CerrarVentana);
        CrearTexto("Texto cerrar", panel, "CERRAR", 14, Crema,
            new Vector2(0.84f, 0.84f), new Vector2(0.98f, 0.98f));

        int cantidad = canasDisponibles == null ? 0 : canasDisponibles.Length;
        if (cantidad == 0)
        {
            CrearTexto("Sin cañas", panel, "No hay cañas configuradas.", 20, Crema,
                new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.65f));
            ventana.SetActive(false);
            return;
        }

        fondosOpciones = new Image[cantidad];
        marcasOpciones = new Text[cantidad];
        float separacion = 0.025f;
        int columnas = cantidad > 3 ? 2 : cantidad;
        float ancho = (0.88f - separacion * (columnas - 1)) / columnas;
        for (int i = 0; i < cantidad; i++)
            CrearOpcionCana(panel, i, cantidad, ancho, separacion);

        textoAviso = CrearTexto("Aviso de equipo", panel, "", 15, Hex("E1C17A"),
            new Vector2(0.08f, 0.025f), new Vector2(0.92f, 0.11f));
        ventana.SetActive(false);
    }

    private void CrearOpcionCana(Transform padre, int indice, int cantidad, float ancho, float separacion)
    {
        CanaData cana = canasDisponibles[indice];
        int columnas = cantidad > 3 ? 2 : cantidad;
        float xMin = 0.06f + (indice % columnas) * (ancho + separacion);
        float xMax = xMin + ancho;
        int filas = Mathf.CeilToInt(cantidad / (float)columnas);
        float alto = (0.54f - separacion * (filas - 1)) / filas;
        float yMax = 0.70f - (indice / columnas) * (alto + separacion);
        Color colorFondo = Hex("3C2A1D");
        var boton = CrearBoton(
            "Opción " + (cana != null ? cana.nombreCana : "caña inválida"),
            padre,
            colorFondo,
            new Vector2(xMin, yMax - alto),
            new Vector2(xMin, yMax - alto),
            new Vector2(xMax, yMax),
            Vector2.zero,
            Vector2.zero,
            () => SeleccionarCana(cana));

        fondosOpciones[indice] = boton.targetGraphic as Image;
        CrearIconoCana(boton.transform, indice, cana);

        string nombre = cana != null ? cana.nombreCana : "Caña sin datos";
        float poder = cana != null ? cana.poderCana : 0f;
        float tolerancia = cana != null ? cana.bonificacionTolerancia : 0f;
        int precio = cana != null ? cana.precio : 0;
        CrearTexto("Nombre", boton.transform, nombre, 21, Crema,
            new Vector2(0.28f, 0.69f), new Vector2(0.96f, 0.91f));
        bool pendientes = cana != null && cana.estadisticasPendientes;
        CrearTexto("Estadísticas", boton.transform,
            pendientes ? "ESTADÍSTICAS POR DEFINIR" : $"PODER {poder:0.#}  ·  TOLERANCIA +{tolerancia:0.#}",
            14, Hex("E1C17A"), new Vector2(0.28f, 0.46f), new Vector2(0.96f, 0.69f));
        string detalle = cana != null && cana.modeloVisual == ModeloCana.ShimanoFx ? "LA MÁS BÁSICA" : pendientes && cana.puestoPotenciaPrevisto > 0
            ? (cana.puestoPotenciaPrevisto == 1 ? "LA MÁS POTENTE" : $"{cana.puestoPotenciaPrevisto}.ª MÁS POTENTE")
            : (precio == 0 ? "GRATIS" : $"{precio} ORO");
        CrearTexto("Precio o posición prevista", boton.transform, detalle,
            17, Hex("F2B632"), new Vector2(0.28f, 0.30f), new Vector2(0.96f, 0.46f));
        marcasOpciones[indice] = CrearTexto("Estado de caña", boton.transform, "",
            13, Crema, new Vector2(0.28f, 0.08f), new Vector2(0.96f, 0.27f));
    }

    private void CrearIconoCana(Transform padre, int indice, CanaData cana)
    {
        var zona = CrearPanel("Silueta de caña", padre, Color.clear,
            new Vector2(0.035f, 0.19f), new Vector2(0.27f, 0.88f));
        zona.GetComponent<Image>().raycastTarget = false;

        if (cana != null && cana.iconoCana != null)
        {
            var icono = zona.GetComponent<Image>();
            icono.sprite = cana.iconoCana;
            icono.color = Color.white;
            icono.preserveAspect = true;
            icono.raycastTarget = false;
            return;
        }

        if (cana != null && cana.puestoPotenciaPrevisto > 0)
        {
            var modelo = new GameObject("Modelo de " + cana.nombreCana,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(CanaGrafico));
            modelo.transform.SetParent(zona, false);
            var grafico = modelo.GetComponent<CanaGrafico>();
            grafico.rectTransform.anchorMin = Vector2.zero;
            grafico.rectTransform.anchorMax = Vector2.one;
            grafico.rectTransform.offsetMin = grafico.rectTransform.offsetMax = Vector2.zero;
            grafico.raycastTarget = false;
            grafico.EstablecerModelo(cana.modeloVisual);
            grafico.Configurar(new Vector2(0.62f, 0.02f), new Vector2(0.38f, 0.98f), 4f, 0.32f, 0f);
            return;
        }

        Color[] colores = { Hex("C7924C"), Hex("5A8A69"), Hex("AAB7B1") };
        Color color = colores[indice % colores.Length];
        float grosor = indice == 0 ? 10f : indice == 1 ? 7f : 4f;
        float alto = indice == 0 ? 0.78f : indice == 1 ? 0.86f : 0.94f;

        var asta = CrearPanel("Asta", zona, color,
            new Vector2(0.5f, 0.12f + (1f - alto) * 0.5f),
            new Vector2(0.5f, 0.12f + (1f + alto) * 0.5f));
        asta.anchorMin = asta.anchorMax = new Vector2(0.5f, 0.5f);
        asta.sizeDelta = new Vector2(grosor, 128f * alto);
        asta.anchoredPosition = Vector2.zero;
        asta.localRotation = Quaternion.Euler(0f, 0f, indice == 1 ? -7f : indice == 2 ? 7f : 0f);
        asta.GetComponent<Image>().raycastTarget = false;

        var mango = CrearPanel("Mango", zona, Hex("38251B"),
            new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f));
        mango.sizeDelta = new Vector2(grosor + 8f, 26f);
        mango.anchoredPosition = new Vector2(0f, 18f);
        mango.GetComponent<Image>().raycastTarget = false;

        var anilla = CrearPanel("Anilla", zona, Dorado,
            new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f));
        anilla.sizeDelta = new Vector2(grosor + 10f, 4f);
        anilla.GetComponent<Image>().raycastTarget = false;
    }

    private void EquiparCanaInicial()
    {
        CanaData inicial = Contiene(canaInicial) ? canaInicial : null;
        if (inicial == null && canasDisponibles != null)
        {
            for (int i = 0; i < canasDisponibles.Length; i++)
            {
                if (canasDisponibles[i] != null && canasDisponibles[i].precio == 0)
                {
                    inicial = canasDisponibles[i];
                    break;
                }
            }
        }

        if (inicial == null && canasDisponibles != null && canasDisponibles.Length > 0)
            inicial = canasDisponibles[0];

        if (inicial != null)
            sistemaPesca.EquiparCana(inicial);
        ActualizarEstadoOpciones();
    }

    private void SeleccionarCana(CanaData cana)
    {
        if (cana == null)
            return;

        if (!sistemaPesca.EquiparCana(cana))
        {
            textoAviso.text = "Termina el intento actual antes de cambiar de caña.";
            return;
        }

        textoAviso.text = "Caña equipada: " + cana.nombreCana;
        ActualizarEstadoOpciones();
        CerrarVentana();
    }

    private void ActualizarEstadoOpciones()
    {
        CanaData equipada = sistemaPesca.CanaEquipada;
        if (textoBotonEquipo != null)
            textoBotonEquipo.text = equipada != null
                ? "EQUIPO  ·  " + equipada.nombreCana
                : "EQUIPO DE PESCA";

        if (marcasOpciones == null)
            return;

        for (int i = 0; i < marcasOpciones.Length; i++)
        {
            bool seleccionada = canasDisponibles[i] == equipada;
            marcasOpciones[i].text = seleccionada ? "EQUIPADA" : "TOCA PARA EQUIPAR";
            fondosOpciones[i].color = seleccionada ? VerdeClaro : Hex("3C2A1D");
        }
    }

    private bool Contiene(CanaData cana)
    {
        if (cana == null || canasDisponibles == null)
            return false;

        for (int i = 0; i < canasDisponibles.Length; i++)
        {
            if (canasDisponibles[i] == cana)
                return true;
        }
        return false;
    }

    private void AlternarVentana()
    {
        ventana.SetActive(!ventana.activeSelf);
    }

    private void CerrarVentana()
    {
        if (ventana != null)
            ventana.SetActive(false);
    }

    private static Button CrearBoton(
        string nombre,
        Transform padre,
        Color color,
        Vector2 anchorMin,
        Vector2 pivot,
        Vector2 anchorMax,
        Vector2 posicion,
        Vector2 tamano,
        UnityEngine.Events.UnityAction accion)
    {
        var objeto = new GameObject(nombre,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        objeto.transform.SetParent(padre, false);
        var rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        var imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = true;
        var boton = objeto.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);
        var colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = Hex("FFF2D8");
        colores.pressedColor = Hex("DDE8D8");
        colores.colorMultiplier = 1f;
        boton.colors = colores;
        return boton;
    }

    private static RectTransform CrearPanel(string nombre, Transform padre, Color color, Vector2 minimo, Vector2 maximo)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.transform.SetParent(padre, false);
        var rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = minimo;
        rect.anchorMax = maximo;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        objeto.GetComponent<Image>().color = color;
        return rect;
    }

    private static Text CrearTexto(
        string nombre, Transform padre, string contenido, int tamano, Color color,
        Vector2 minimo, Vector2 maximo)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        objeto.transform.SetParent(padre, false);
        var rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = minimo;
        rect.anchorMax = maximo;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var texto = objeto.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 10;
        texto.resizeTextMaxSize = tamano;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = color;
        texto.raycastTarget = false;
        return texto;
    }

    private static Color Hex(string valor)
    {
        ColorUtility.TryParseHtmlString("#" + valor, out Color color);
        return color;
    }
}
