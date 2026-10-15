using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RF8: registra los peces descubiertos y presenta el catálogo local.</summary>
[DisallowMultipleComponent]
public sealed class CatalogoManager : MonoBehaviour
{
    private const string ClaveDescubrimiento = "RoadToAmazonas.Catalogo.v1.";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly Color Madera = ColorHex("#39251B");
    private static readonly Color Crema = ColorHex("#F2E8CF");
    private static readonly Color Dorado = ColorHex("#F2B632");
    private static readonly Color Verde = ColorHex("#294735");

    public static CatalogoManager Instance { get; private set; }
    public event Action<PezData> EspecieDescubierta;

    private readonly List<PezData> especies = new List<PezData>();
    private readonly List<GameObject> tarjetas = new List<GameObject>();
    private GameObject menu;
    private GameObject detalle;
    private Text progreso;
    private Image imagenDetalle;
    private Text nombreDetalle;
    private Text rarezaDetalle;
    private Text rangoDetalle;
    private Text habitatDetalle;
    private Button botonCatalogo;
    private TensionSystem sistemaPesca;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        sistemaPesca = GetComponent<TensionSystem>();
        CrearInterfaz();
    }

    private void Start()
    {
        CargarEspecies();
        CrearTarjetas();
        ActualizarProgreso();
        if (sistemaPesca != null)
            sistemaPesca.LanzamientoIniciado += Cerrar;
    }

    private void Update()
    {
        // Oculta el acceso al catálogo durante todo el lanzamiento, la espera
        // del pique y la recogida. Se muestra de nuevo al volver al estado listo.
        bool disponible = sistemaPesca == null || sistemaPesca.PuedeCambiarCana;
        if (botonCatalogo != null && botonCatalogo.gameObject.activeSelf != disponible)
            botonCatalogo.gameObject.SetActive(disponible);
        if (!disponible && menu != null && menu.activeSelf)
            Cerrar();
    }

    private void OnDestroy()
    {
        if (sistemaPesca != null)
            sistemaPesca.LanzamientoIniciado -= Cerrar;
        if (Instance == this) Instance = null;
    }

    /// <summary>Persiste el primer descubrimiento, sin duplicar capturas posteriores.</summary>
    public bool RegistrarCaptura(PezData pez)
    {
        if (pez == null) return false;
        string clave = ClaveDe(pez);
        if (PlayerPrefs.GetInt(clave, 0) == 1) return false;

        PlayerPrefs.SetInt(clave, 1);
        PlayerPrefs.Save();
        if (!especies.Contains(pez)) especies.Add(pez);
        CrearTarjetas();
        ActualizarProgreso();
        EspecieDescubierta?.Invoke(pez);
        return true;
    }

    public bool EstaDescubierto(PezData pez)
    {
        return pez != null && PlayerPrefs.GetInt(ClaveDe(pez), 0) == 1;
    }

    public int CantidadDescubierta
    {
        get
        {
            int cantidad = 0;
            foreach (var pez in especies)
                if (EstaDescubierto(pez)) cantidad++;
            return cantidad;
        }
    }

    private static string ClaveDe(PezData pez)
    {
        // El nombre del asset es estable y distingue especies incluso si hay
        // nombres visibles similares o acentos en el nombre mostrado.
        return ClaveDescubrimiento + Uri.EscapeDataString(pez.name);
    }

    private void CargarEspecies()
    {
        FisheryManager gestor = FisheryManager.Instance;
        if (gestor == null) gestor = FindFirstObjectByType<FisheryManager>();
        if (gestor != null && gestor.ZonaActual != null && gestor.ZonaActual.especiesDeEstaZona != null)
        {
            foreach (var pez in gestor.ZonaActual.especiesDeEstaZona)
                if (pez != null && !especies.Contains(pez)) especies.Add(pez);
        }

        especies.Sort((a, b) => string.Compare(a.nombreEspecie, b.nombreEspecie, StringComparison.CurrentCulture));
    }

    private void CrearInterfaz()
    {
        var lienzo = new GameObject("Canvas Catálogo de peces", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        var scaler = lienzo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        botonCatalogo = CrearBoton("Abrir catálogo", lienzo.transform, Verde,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(230f, 54f), Abrir);
        CrearTexto("Etiqueta catálogo", botonCatalogo.transform, "PECES  0/0", 19, Crema,
            new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f));

        menu = new GameObject("Menú catálogo", typeof(RectTransform), typeof(Image));
        menu.transform.SetParent(lienzo.transform, false);
        Estirar(menu.GetComponent<RectTransform>());
        menu.GetComponent<Image>().color = new Color(0.015f, 0.035f, 0.03f, 0.88f);

        var panel = CrearPanel("Panel de catálogo", menu.transform, Madera,
            new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.955f));
        CrearTexto("Título catálogo", panel.transform, "PECES DEL AMAZONAS", 32, Crema,
            new Vector2(0.04f, 0.88f), new Vector2(0.66f, 0.98f));
        progreso = CrearTexto("Progreso catálogo", panel.transform, "", 16, Dorado,
            new Vector2(0.66f, 0.88f), new Vector2(0.82f, 0.98f));
        CrearBotonConTexto("Cerrar catálogo", panel.transform, "CERRAR", ColorHex("#6B3B25"), Crema,
            new Vector2(0.83f, 0.91f), new Vector2(0.96f, 0.91f),
            new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero, Cerrar);

        CrearCuadricula(panel.transform);
        CrearPanelDetalle(menu.transform);
        menu.SetActive(false);
    }

    private void CrearCuadricula(Transform panel)
    {
        var scrollRectTransform = Rect("Desplazamiento catálogo", panel,
            new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.86f));
        var scroll = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        var viewport = Rect("Viewport catálogo", scrollRectTransform,
            Vector2.zero, Vector2.one);
        var mascara = viewport.gameObject.AddComponent<Mask>();
        mascara.showMaskGraphic = false;
        viewport.gameObject.AddComponent<Image>().color = Color.white;

        var contenido = Rect("Cuadrícula de especies", viewport,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        contenido.pivot = new Vector2(0.5f, 1f);
        var grid = contenido.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(220f, 205f);
        grid.spacing = new Vector2(14f, 14f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Clamp(Mathf.FloorToInt(Screen.width / 270f), 2, 4);
        contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = contenido;
    }

    private void CrearPanelDetalle(Transform padre)
    {
        detalle = CrearPanel("Ficha del pez", padre, Madera,
            new Vector2(0.18f, 0.11f), new Vector2(0.82f, 0.89f)).gameObject;
        CrearTexto("Título ficha", detalle.transform, "FICHA DE ESPECIE", 24, Dorado,
            new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));
        imagenDetalle = CrearPanel("Imagen de especie", detalle.transform, Crema,
            new Vector2(0.08f, 0.37f), new Vector2(0.48f, 0.83f)).GetComponent<Image>();
        imagenDetalle.preserveAspect = true;
        imagenDetalle.raycastTarget = false;
        nombreDetalle = CrearTexto("Nombre especie", detalle.transform, "", 27, Crema,
            new Vector2(0.53f, 0.69f), new Vector2(0.95f, 0.83f));
        rarezaDetalle = CrearTexto("Rareza especie", detalle.transform, "", 21, Dorado,
            new Vector2(0.53f, 0.57f), new Vector2(0.95f, 0.69f));
        rangoDetalle = CrearTexto("Rangos especie", detalle.transform, "", 18, Crema,
            new Vector2(0.53f, 0.33f), new Vector2(0.95f, 0.57f));
        habitatDetalle = CrearTexto("Hábitat especie", detalle.transform, "", 16, Crema,
            new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.34f));
        CrearBotonConTexto("Volver a cuadrícula", detalle.transform, "VOLVER", Verde, Crema,
            new Vector2(0.35f, 0.045f), new Vector2(0.35f, 0.045f),
            new Vector2(0.65f, 0.16f), Vector2.zero, Vector2.zero, CerrarDetalle);
        detalle.SetActive(false);
    }

    private void CrearTarjetas()
    {
        if (menu == null) return;
        var contenido = menu.transform.Find("Panel de catálogo/Desplazamiento catálogo/Viewport catálogo/Cuadrícula de especies");
        if (contenido == null) return;
        foreach (Transform hijo in contenido) Destroy(hijo.gameObject);
        tarjetas.Clear();

        for (int i = 0; i < especies.Count; i++)
        {
            PezData pez = especies[i];
            bool descubierto = EstaDescubierto(pez);
            var tarjeta = new GameObject("Especie " + pez.name, typeof(RectTransform), typeof(Image), typeof(Button));
            tarjeta.transform.SetParent(contenido, false);
            tarjeta.GetComponent<Image>().color = descubierto ? HexRareza(pez.rareza) : ColorHex("#26342D");
            var boton = tarjeta.GetComponent<Button>();
            boton.targetGraphic = tarjeta.GetComponent<Image>();
            boton.interactable = descubierto;
            boton.onClick.AddListener(() => MostrarDetalle(pez));

            var imagen = CrearPanel("Retrato", tarjeta.transform,
                descubierto ? Crema : ColorHex("#18221D"), new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.94f));
            var foto = imagen.GetComponent<Image>();
            foto.sprite = descubierto ? pez.foto : null;
            foto.preserveAspect = true;
            foto.raycastTarget = false;
            string etiqueta = descubierto ? pez.nombreEspecie : "???";
            CrearTexto("Nombre", tarjeta.transform, etiqueta, 17, descubierto ? Color.white : Dorado,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.25f));
            tarjetas.Add(tarjeta);
        }

        ActualizarProgreso();
    }

    private void MostrarDetalle(PezData pez)
    {
        if (pez == null || !EstaDescubierto(pez)) return;
        imagenDetalle.sprite = pez.foto;
        nombreDetalle.text = pez.nombreEspecie;
        rarezaDetalle.text = "Rareza: " + NombreRareza(pez.rareza);
        rangoDetalle.text = string.Format(Cultura,
            "Peso: {0:0.##}–{1:0.##} kg\nLongitud: {2:0.##}–{3:0.##} cm",
            pez.pesoMinimo, pez.pesoMaximo, pez.longitudMinima, pez.longitudMaxima);
        habitatDetalle.text = string.IsNullOrWhiteSpace(pez.descripcion)
            ? "Hábitat: " + (string.IsNullOrWhiteSpace(pez.habitat) ? "Amazonas" : pez.habitat)
            : pez.descripcion;
        detalle.SetActive(true);
    }

    private void Abrir()
    {
        CrearTarjetas();
        menu.SetActive(true);
        detalle.SetActive(false);
    }

    private void Cerrar() => menu.SetActive(false);
    private void CerrarDetalle() => detalle.SetActive(false);

    private void ActualizarProgreso()
    {
        if (progreso != null) progreso.text = $"DESCUBIERTOS  {CantidadDescubierta}/{especies.Count}";
        if (botonCatalogo != null)
        {
            var texto = botonCatalogo.GetComponentInChildren<Text>();
            if (texto != null) texto.text = $"PECES  {CantidadDescubierta}/{especies.Count}";
        }
    }

    private static Button CrearBoton(string nombre, Transform padre, Color color,
        Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 posicion, Vector2 tamano, UnityEngine.Events.UnityAction accion)
    {
        var rect = Rect(nombre, padre, anchorMin, anchorMax);
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        var imagen = rect.gameObject.AddComponent<Image>();
        imagen.color = color;
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);
        return boton;
    }

    private static void CrearBotonConTexto(string nombre, Transform padre, string titulo, Color fondo,
        Color textoColor, Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 posicion,
        Vector2 tamano, UnityEngine.Events.UnityAction accion)
    {
        var boton = CrearBoton(nombre, padre, fondo, anchorMin, pivot, anchorMax, posicion, tamano, accion);
        CrearTexto("Texto", boton.transform, titulo, 16, textoColor, Vector2.zero, Vector2.one);
    }

    private static RectTransform Rect(string nombre, Transform padre, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image CrearPanel(string nombre, Transform padre, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var imagen = Rect(nombre, padre, anchorMin, anchorMax).gameObject.AddComponent<Image>();
        imagen.color = color;
        return imagen;
    }

    private static Text CrearTexto(string nombre, Transform padre, string contenido, int tamano, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var texto = Rect(nombre, padre, anchorMin, anchorMax).gameObject.AddComponent<Text>();
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

    private static string NombreRareza(Rareza rareza)
    {
        switch (rareza)
        {
            case Rareza.Comun: return "Común";
            case Rareza.PocoComun: return "Poco común";
            case Rareza.Rara: return "Rara";
            case Rareza.Epica: return "Épica";
            case Rareza.Legendaria: return "Legendaria";
            default: return rareza.ToString();
        }
    }

    private static Color HexRareza(Rareza rareza)
    {
        switch (rareza)
        {
            case Rareza.Comun: return ColorHex("#526358");
            case Rareza.PocoComun: return ColorHex("#307539");
            case Rareza.Rara: return ColorHex("#2864A6");
            case Rareza.Epica: return ColorHex("#8541B0");
            case Rareza.Legendaria: return ColorHex("#B37D00");
            default: return Verde;
        }
    }

    private static Color ColorHex(string valor)
    {
        ColorUtility.TryParseHtmlString(valor, out var color);
        return color;
    }
}
