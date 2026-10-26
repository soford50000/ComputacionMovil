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
    private static readonly Color NogalCasilla = ColorHex("#B38F5C");
    private static readonly Color SiluetaNogal = ColorHex("#8F6F42");
    private static readonly Color CremaCasilla = ColorHex("#F5E9D0");
    private static readonly Color TintaCatalogo = ColorHex("#2A1D10");
    private static readonly Color SecundarioCrema = ColorHex("#5A4326");
    // #5A4326 no alcanza 4.5:1 sobre nogal; conservarlo solo sobre crema.
    private static readonly Color SecundarioNogal = ColorHex("#332411");

    public static CatalogoManager Instance { get; private set; }
    public event Action<PezData> EspecieDescubierta;

    private readonly List<PezData> especies = new List<PezData>();
    private readonly List<GameObject> tarjetas = new List<GameObject>();
    private GameObject menu;
    private GameObject detalle;
    private Text progreso;
    private RectTransform rellenoProgreso;
    private Image imagenDetalle;
    private Text nombreDetalle;
    private Text rarezaDetalle;
    private Text rangoDetalle;
    private Text habitatDetalle;
    private Button botonCatalogo;
    private TensionSystem sistemaPesca;
    private Material materialSilueta;
    private FisheryManager gestorZonas;
    private Text tituloCatalogo, ayudaFicha, avisoSinEspecies;

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
        gestorZonas = GetComponent<FisheryManager>();
        if (gestorZonas != null) gestorZonas.ZonaCambiada += CambiarZona;
        CambiarZona(gestorZonas != null ? gestorZonas.ZonaActual : null);
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
        if (gestorZonas != null) gestorZonas.ZonaCambiada -= CambiarZona;
        if (sistemaPesca != null)
            sistemaPesca.LanzamientoIniciado -= Cerrar;
        if (Instance == this) Instance = null;
        if (materialSilueta != null) Destroy(materialSilueta);
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
        especies.Clear();
        FisheryManager gestor = FisheryManager.Instance;
        if (gestor == null) gestor = FindAnyObjectByType<FisheryManager>();
        if (gestor != null && gestor.ZonaActual != null && gestor.ZonaActual.especiesDeEstaZona != null)
        {
            foreach (var pez in gestor.ZonaActual.especiesDeEstaZona)
                if (pez != null && !especies.Contains(pez)) especies.Add(pez);
        }

        especies.Sort((a, b) => string.Compare(a.nombreEspecie, b.nombreEspecie, StringComparison.CurrentCulture));
    }

    private string NombreZona => gestorZonas != null && gestorZonas.ZonaActual != null
        ? gestorZonas.ZonaActual.nombreZona : "Amazonas";

    private void CambiarZona(ZonaData zona)
    {
        Cerrar();
        CerrarDetalle();
        CargarEspecies();
        tituloCatalogo.text = NombreZona == "Amazonas" ? "Peces del Amazonas" : "Peces de " + NombreZona;
        ayudaFicha.text = "Notas de campo · " + NombreZona;
        avisoSinEspecies.gameObject.SetActive(especies.Count == 0);
        CrearTarjetas();
        var scroll = menu.GetComponentInChildren<ScrollRect>(true);
        scroll.verticalNormalizedPosition = 1;
    }

    private void CrearInterfaz()
    {
        materialSilueta = new Material(Resources.Load<Shader>("DecoracionCatalogo/SiluetaNogal"));
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
            Vector2.up, Vector2.up, Vector2.up,
            new Vector2(24f, -24f), new Vector2(250f, 58f), Abrir);
        EstiloExpedicion.Borde(botonCatalogo.targetGraphic as Image, EstiloExpedicion.Dorado, 2);
        CrearIconoMenu(botonCatalogo.transform);
        EstiloExpedicion.Texto(botonCatalogo.transform, "Acceso colección", "Colección de peces", 19,
            EstiloExpedicion.Papel, .23f, .08f, .96f, .92f);

        menu = new GameObject("Menú catálogo", typeof(RectTransform), typeof(Image));
        menu.transform.SetParent(lienzo.transform, false);
        Estirar(menu.GetComponent<RectTransform>());
        TemaZonaUI.Colorear(menu.GetComponent<Image>(), new Color(0.015f, 0.035f, 0.03f, 0.88f));

        var panel = CrearPanel("Panel de catálogo", menu.transform, EstiloExpedicion.Papel,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
        EstiloExpedicion.Cabecera(panel.rectTransform, "Peces del Amazonas", "Tu cuaderno de descubrimientos. Cada captura cuenta una historia.", Cerrar);
        panel.gameObject.AddComponent<FondoNogalCatalogo>().MostrarVetas = false;
        panel.gameObject.AddComponent<VegetacionCatalogo>().Preparar(panel.rectTransform, mostrarCoralesEnSanAndres: true);
        TemaZonaUI.Colorear(panel.transform.Find("Título").GetComponent<Text>(), TintaCatalogo);
        tituloCatalogo = panel.transform.Find("Título").GetComponent<Text>();
        TemaZonaUI.Colorear(panel.transform.Find("Ayuda").GetComponent<Text>(), SecundarioNogal);
        TemaZonaUI.Colorear(panel.transform.Find("Marca de expedición").GetComponent<Text>(), SecundarioNogal);
        panel.transform.Find("Línea de cabecera").gameObject.SetActive(false);
        EstiloExpedicion.PrepararModal(menu, panel.rectTransform);
        progreso = CrearTexto("Progreso catálogo", panel.transform, "", 17, TintaCatalogo,
            new Vector2(0.04f, 0.733f), new Vector2(0.46f, 0.786f));
        var pista = EstiloExpedicion.Panel(panel.transform, "Progreso de exploración", ColorHex("#64765A"), .49f, .75f, .96f, .765f);
        rellenoProgreso = EstiloExpedicion.Panel(pista.transform, "Descubiertos", EstiloExpedicion.Verde, 0, 0, 0, 1).rectTransform;

        CrearCuadricula(panel.transform);
        avisoSinEspecies = EstiloExpedicion.Texto(panel.transform, "Especies próximamente",
            "El catálogo de esta zona estará disponible próximamente.", 22, TintaCatalogo,
            .08f, .32f, .92f, .58f, false, TextAnchor.MiddleCenter);
        avisoSinEspecies.gameObject.SetActive(false);
        EstiloExpedicion.Texto(panel.transform, "Ayuda colección", "Desliza para explorar la colección", 13,
            SecundarioNogal, .04f, .009f, .96f, .04f, false, TextAnchor.MiddleCenter);
        CrearPanelDetalle(menu.transform);
        menu.SetActive(false);
    }

    private void CrearCuadricula(Transform panel)
    {
        var scrollRectTransform = Rect("Desplazamiento catálogo", panel,
            new Vector2(0.035f, 0.045f), new Vector2(0.965f, 0.72f));
        var scroll = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        var viewport = Rect("Viewport catálogo", scrollRectTransform,
            Vector2.zero, Vector2.one);
        var mascara = viewport.gameObject.AddComponent<Mask>();
        mascara.showMaskGraphic = false;
        TemaZonaUI.Colorear(viewport.gameObject.AddComponent<Image>(), Color.white);

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
        contenido.gameObject.AddComponent<CuadriculaExpedicion>();
        scroll.viewport = viewport;
        scroll.content = contenido;
    }

    private void CrearPanelDetalle(Transform padre)
    {
        detalle = CrearPanel("Ficha del pez", padre, EstiloExpedicion.Velo, Vector2.zero, Vector2.one).gameObject;
        var ficha = CrearPanel("Cuaderno de especie", detalle.transform, EstiloExpedicion.Papel,
            new Vector2(.08f, .08f), new Vector2(.92f, .92f));
        EstiloExpedicion.Cabecera(ficha.rectTransform, "Ficha de especie", "Notas de campo · Cuenca del Amazonas", CerrarDetalle);
        ayudaFicha = ficha.transform.Find("Ayuda").GetComponent<Text>();
        EstiloExpedicion.PrepararModal(detalle, ficha.rectTransform);
        imagenDetalle = CrearPanel("Imagen de especie", ficha.transform, Color.white,
            new Vector2(0.04f, 0.24f), new Vector2(0.48f, 0.75f)).GetComponent<Image>();
        imagenDetalle.preserveAspect = true;
        imagenDetalle.raycastTarget = false;
        nombreDetalle = CrearTexto("Nombre especie", ficha.transform, "", 30, EstiloExpedicion.Tinta,
            new Vector2(0.53f, 0.60f), new Vector2(0.95f, 0.75f));
        rarezaDetalle = CrearTexto("Rareza especie", ficha.transform, "", 19, EstiloExpedicion.Verde,
            new Vector2(0.53f, 0.52f), new Vector2(0.95f, 0.60f));
        rangoDetalle = CrearTexto("Rangos especie", ficha.transform, "", 20, EstiloExpedicion.Tinta,
            new Vector2(0.53f, 0.35f), new Vector2(0.95f, 0.51f));
        habitatDetalle = CrearTexto("Hábitat especie", ficha.transform, "", 18, EstiloExpedicion.Secundario,
            new Vector2(0.53f, 0.16f), new Vector2(0.95f, 0.34f));
        var volver = EstiloExpedicion.Accion(ficha.transform, "Volver a cuadrícula", "Volver a la colección", .32f, .04f, .68f, .13f, CerrarDetalle);
        var disposicion = ficha.gameObject.AddComponent<DisposicionExpedicion>();
        disposicion.Agregar(imagenDetalle.rectTransform, .08f, .48f, .92f, .77f);
        disposicion.Agregar(nombreDetalle.rectTransform, .06f, .39f, .94f, .48f);
        disposicion.Agregar(rarezaDetalle.rectTransform, .06f, .33f, .94f, .39f);
        disposicion.Agregar(rangoDetalle.rectTransform, .06f, .24f, .94f, .33f);
        disposicion.Agregar(habitatDetalle.rectTransform, .06f, .13f, .94f, .24f);
        disposicion.Agregar((RectTransform)volver.transform, .22f, .035f, .78f, .11f);
        detalle.SetActive(false);
    }

    private void CrearTarjetas()
    {
        if (menu == null) return;
        // Retira enlaces de tarjetas destruidas en aperturas anteriores.
        menu.GetComponentInParent<TemaZonaUI>(true)?.Actualizar(null);
        var contenido = menu.transform.Find("Panel de catálogo/Desplazamiento catálogo/Viewport catálogo/Cuadrícula de especies");
        if (contenido == null) return;
        // Conservar la capa ornamental al reconstruir las tarjetas.
        foreach (var tarjeta in tarjetas) { tarjeta.SetActive(false); Destroy(tarjeta); }
        tarjetas.Clear();

        for (int i = 0; i < especies.Count; i++)
        {
            PezData pez = especies[i];
            bool descubierto = EstaDescubierto(pez);
            var tarjeta = new GameObject("Especie " + pez.name, typeof(RectTransform), typeof(Image), typeof(Button));
            tarjeta.transform.SetParent(contenido, false);
            TemaZonaUI.Colorear(tarjeta.GetComponent<Image>(), descubierto ? CremaCasilla : NogalCasilla);
            var boton = tarjeta.GetComponent<Button>();
            boton.targetGraphic = tarjeta.GetComponent<Image>();
            boton.interactable = descubierto;
            boton.onClick.AddListener(() => MostrarDetalle(pez));
            EstiloExpedicion.Boton(boton);
            var colores = TemaZonaUI.EstadosOriginales(boton);
            colores.disabledColor = Color.white;
            TemaZonaUI.Estados(boton, colores);
            EstiloExpedicion.Borde(tarjeta.GetComponent<Image>(), descubierto ? HexRareza(pez.rareza) : NogalCasilla);

            var imagen = CrearPanel("Retrato", tarjeta.transform,
                Color.white, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.87f));
            var foto = imagen.GetComponent<Image>();
            foto.sprite = pez.foto;
            TemaZonaUI.Colorear(foto, descubierto ? Color.white : SiluetaNogal);
            foto.material = descubierto ? null : materialSilueta;
            foto.preserveAspect = true;
            foto.raycastTarget = false;
            string etiqueta = descubierto ? pez.nombreEspecie : "Especie por descubrir";
            CrearTexto("Nombre", tarjeta.transform, etiqueta, 19, TintaCatalogo,
                new Vector2(0.06f, 0.11f), new Vector2(0.94f, 0.32f));
            EstiloExpedicion.Texto(tarjeta.transform, "Rareza", pez.caracteristicasPendientes ? "Próximamente" :
                descubierto ? NombreRareza(pez.rareza) : NombreZona == "Amazonas" ? "?  Explora el río" : "?  Explora el mar", 14,
                descubierto ? SecundarioCrema : SecundarioNogal, .07f, .02f, .93f, .12f);
            EstiloExpedicion.Texto(tarjeta.transform, "Número de ficha", (i + 1).ToString("D2") + "  /  " + NombreZona.ToUpperInvariant(), 12,
                descubierto ? SecundarioCrema : SecundarioNogal, .07f, .88f, .93f, .97f);
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
        TemaZonaUI.Colorear(rarezaDetalle, HexRareza(pez.rareza));
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
        if (progreso != null) progreso.text = $"{CantidadDescubierta} de {especies.Count} especies descubiertas";
        if (rellenoProgreso != null)
            rellenoProgreso.anchorMax = new Vector2(especies.Count > 0 ? (float)CantidadDescubierta / especies.Count : 0, 1);
    }

    private static void CrearIconoMenu(Transform padre)
    {
        EstiloExpedicion.Panel(padre, "Tapa del cuaderno", EstiloExpedicion.Dorado, .045f, .23f, .18f, .77f);
        EstiloExpedicion.Panel(padre, "Página", EstiloExpedicion.Papel, .068f, .27f, .17f, .73f);
        EstiloExpedicion.Panel(padre, "Lomo", EstiloExpedicion.Madera, .057f, .23f, .066f, .77f);
        EstiloExpedicion.Panel(padre, "Apunte", EstiloExpedicion.Verde, .088f, .53f, .15f, .55f);
        EstiloExpedicion.Panel(padre, "Apunte inferior", EstiloExpedicion.Verde, .088f, .43f, .14f, .45f);
    }

    private static Button CrearBoton(string nombre, Transform padre, Color color,
        Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 posicion, Vector2 tamano, UnityEngine.Events.UnityAction accion)
    {
        var rect = Rect(nombre, padre, anchorMin, anchorMax);
        rect.pivot = pivot;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        var imagen = rect.gameObject.AddComponent<Image>();
        TemaZonaUI.Colorear(imagen, color);
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);
        EstiloExpedicion.Boton(boton);
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
        TemaZonaUI.Colorear(imagen, color);
        return imagen;
    }

    private static Text CrearTexto(string nombre, Transform padre, string contenido, int tamano, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var texto = Rect(nombre, padre, anchorMin, anchorMax).gameObject.AddComponent<Text>();
        EstiloExpedicion.Tipografia(texto, tamano, color, nombre == "Nombre especie");
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.fontStyle = FontStyle.Normal;
        TemaZonaUI.Colorear(texto, color);
        texto.alignment = TextAnchor.MiddleCenter;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = Mathf.Min(tamano, 16);
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
