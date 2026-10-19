using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Selector de una sola carnada, disponible antes de lanzar.</summary>
public sealed class CeboSelectorUI : MonoBehaviour
{
    private const string ClaveUsos = "RoadToAmazonas.CebosUsos.v2.";
    private const string ClaveCompraAnterior = "RoadToAmazonas.CebosComprados.v1.";
    private TensionSystem pesca;
    private CeboData[] cebos;
    private CeboData seleccionado;
    private GameObject ventana;
    private Text resumen, titulo, detalle, afinidades, aviso, textoEquipar;
    private Image foto;
    private Button abrir, equipar;
    private Image[] tarjetas;
    private Text[] estados;
    private Image[] retratosAfinidad;
    private ScrollRect desplazamientoAfinidades;
    public bool EstaAbierto => ventana != null && ventana.activeSelf;

    private static readonly Color Crema = EstiloExpedicion.Hex("F5E9D0");
    private static readonly Color Verde = EstiloExpedicion.Verde;
    private static readonly Color Nogal = EstiloExpedicion.Hex("B38F5C");
    private static readonly Color Tinta = EstiloExpedicion.Hex("2A1D10");
    private static readonly Color Secundario = EstiloExpedicion.Hex("332411");

    private void Awake()
    {
        pesca = GetComponent<TensionSystem>();
        if (pesca == null) { enabled = false; return; }
        var catalogo = Resources.Load<CatalogoCebos>("CatalogoCebos");
        cebos = catalogo != null && catalogo.cebos != null ? catalogo.cebos : new CeboData[0];
        CrearInterfaz();
        if (pesca.CeboEquipado != null && !TieneUsos(pesca.CeboEquipado))
            pesca.EquiparCebo(null);
        pesca.CeboCambiado += ActualizarResumen;
        ActualizarResumen();
    }

    private void LateUpdate()
    {
        bool puedeCambiar = pesca.PuedeCambiarCebo;
        abrir.interactable = puedeCambiar;
        if (abrir.gameObject.activeSelf != puedeCambiar)
            abrir.gameObject.SetActive(puedeCambiar);
        if (!puedeCambiar && EstaAbierto) Cerrar();
    }

    public void Abrir()
    {
        if (!pesca.PuedeCambiarCebo) return;
        ventana.SetActive(true);
        Mostrar(pesca.CeboEquipado);
    }

    public void Cerrar() => ventana.SetActive(false);

    private void CrearInterfaz()
    {
        var lienzo = new GameObject("Canvas Carnadas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31;
        var scaler = lienzo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;

        abrir = Boton(lienzo.transform, "Abrir carnadas", "", 0, 1, 0, 1, Abrir);
        var acceso = abrir.GetComponent<RectTransform>();
        acceso.pivot = new Vector2(0, 1);
        acceso.anchoredPosition = new Vector2(24, -156);
        acceso.sizeDelta = new Vector2(250, 58);
        resumen = Texto(abrir.transform, "Carnada equipada", "", 17, .04f, .05f, .96f, .95f);

        ventana = Panel(lienzo.transform, "Menú de carnadas", new Color(0, 0, 0, .86f), 0, 0, 1, 1).gameObject;
        var panel = Panel(ventana.transform, "Panel de carnadas", EstiloExpedicion.Papel, .04f, .05f, .96f, .95f);
        EstiloExpedicion.Cabecera(panel, "Carnadas del río", "Prepara tu próxima captura. Cada carnada atrae especies diferentes.", Cerrar);
        panel.gameObject.AddComponent<FondoNogalCatalogo>();
        panel.Find("Título").GetComponent<Text>().color = Tinta;
        panel.Find("Ayuda").GetComponent<Text>().color = Secundario;
        panel.Find("Marca de expedición").GetComponent<Text>().color = Secundario;
        panel.Find("Línea de cabecera").GetComponent<Image>().color = Nogal;
        EstiloExpedicion.PrepararModal(ventana, panel);
        var disposicion = panel.gameObject.AddComponent<DisposicionExpedicion>();
        panel.gameObject.AddComponent<VegetacionCatalogo>().Preparar(panel);

        tarjetas = new Image[cebos.Length];
        estados = new Text[cebos.Length];
        int filas = Mathf.Max(1, Mathf.CeilToInt(cebos.Length / 2f));
        float alto = .67f / filas;
        for (int i = 0; i < cebos.Length; i++)
        {
            var cebo = cebos[i];
            float x = .025f + i % 2 * .305f;
            float y = .77f - i / 2 * alto;
            var boton = Boton(panel, "Carnada " + (cebo != null ? cebo.nombre : "Sin datos"), "",
                x, y - alto + .012f, x + .292f, y, () => Mostrar(cebo));
            tarjetas[i] = boton.targetGraphic as Image;
            float yVertical = .77f - i / 2 * .075f;
            disposicion.Agregar((RectTransform)boton.transform, .025f + i % 2 * .48f, yVertical - .066f,
                .49f + i % 2 * .48f, yVertical);
            tarjetas[i].color = Crema;
            var imagen = Panel(boton.transform, "Imagen", Color.clear, .025f, .10f, .37f, .90f).GetComponent<Image>();
            imagen.raycastTarget = false;
            if (cebo != null && cebo.icono != null)
            {
                imagen.sprite = cebo.icono;
                imagen.preserveAspect = true;
                imagen.color = Color.white;
            }
            else Texto(imagen.transform, "Imagen provisional", "CEBO", 15, 0, 0, 1, 1);
            var nombre = Texto(boton.transform, "Nombre", cebo != null ? cebo.nombre : "Sin datos",
                19, .40f, .39f, .96f, .94f);
            nombre.alignment = TextAnchor.MiddleLeft;
            estados[i] = Texto(boton.transform, "Estado", "", 16, .40f, .07f, .96f, .37f);
            estados[i].alignment = TextAnchor.MiddleLeft;
            estados[i].color = Secundario;
        }
        var sinCarnada = Boton(panel, "Sin carnada", "Sin carnada", .025f, .025f, .315f, .1f, () => Mostrar(null));
        disposicion.Agregar((RectTransform)sinCarnada.transform, .025f, .41f, .49f, .46f);

        var ficha = Panel(panel, "Ficha de carnada", Crema, .66f, .025f, .975f, .77f);
        EstiloExpedicion.Borde(ficha.GetComponent<Image>(), Nogal);
        disposicion.Agregar(ficha, .025f, .025f, .975f, .395f);
        titulo = Texto(ficha, "Nombre de carnada", "", 25, .06f, .85f, .94f, .98f);
        EstiloExpedicion.Tipografia(titulo, 25, Tinta, true);
        foto = Panel(ficha, "Foto de carnada", Color.clear, .22f, .60f, .78f, .85f).GetComponent<Image>();
        foto.preserveAspect = true;
        foto.raycastTarget = false;
        detalle = Texto(ficha, "Descripción", "", 16, .06f, .48f, .94f, .60f);
        retratosAfinidad = new Image[3];
        for (int i = 0; i < retratosAfinidad.Length; i++)
        {
            retratosAfinidad[i] = Panel(ficha, "Especie favorecida " + i, Color.white,
                .08f + i * .29f, .37f, .34f + i * .29f, .47f).GetComponent<Image>();
            retratosAfinidad[i].preserveAspect = true;
            retratosAfinidad[i].raycastTarget = false;
        }
        var zonaAfinidades = Rect(ficha, "Lista de especies favorecidas", .07f, .21f, .93f, .37f);
        desplazamientoAfinidades = zonaAfinidades.gameObject.AddComponent<ScrollRect>();
        desplazamientoAfinidades.horizontal = false;
        desplazamientoAfinidades.scrollSensitivity = 25;
        desplazamientoAfinidades.movementType = ScrollRect.MovementType.Clamped;
        var viewport = Panel(zonaAfinidades, "Viewport", Color.white, 0, 0, 1, 1);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        afinidades = Texto(viewport, "Peces favorecidos", "", 16, 0, 1, 1, 1);
        afinidades.rectTransform.pivot = new Vector2(.5f, 1);
        afinidades.resizeTextForBestFit = false;
        afinidades.verticalOverflow = VerticalWrapMode.Overflow;
        afinidades.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        desplazamientoAfinidades.viewport = viewport;
        desplazamientoAfinidades.content = afinidades.rectTransform;
        EstiloExpedicion.Texto(ficha, "Ayuda de desplazamiento", "Desliza para ver todas las especies", 12,
            Secundario, .07f, .17f, .93f, .21f);
        afinidades.alignment = TextAnchor.UpperLeft;
        aviso = Texto(ficha, "Aviso", "", 16, .05f, .105f, .95f, .16f);
        equipar = Boton(ficha, "Equipar carnada", "", .07f, .02f, .93f, .10f, Equipar);
        textoEquipar = Texto(equipar.transform, "Texto equipar", "EQUIPAR", 19, 0, 0, 1, 1);
        textoEquipar.color = Crema;
        EstiloExpedicion.Boton(equipar);
        ventana.SetActive(false);
    }

    private void Mostrar(CeboData cebo)
    {
        seleccionado = cebo;
        titulo.text = cebo != null ? cebo.nombre : "Sin carnada";
        foto.sprite = cebo != null ? cebo.icono : null;
        foto.color = foto.sprite != null ? Color.white : Color.clear;
        detalle.text = cebo != null ? cebo.descripcion : "Todos los peces conservan su probabilidad base.";
        var lista = new StringBuilder();
        if (cebo != null && cebo.especiesFavorecidas != null)
        {
            foreach (var pez in cebo.especiesFavorecidas)
                if (pez != null) lista.AppendLine("• " + pez.nombreEspecie.Split('(')[0].Trim());
        }
        afinidades.text = lista.ToString();
        Canvas.ForceUpdateCanvases();
        desplazamientoAfinidades.verticalNormalizedPosition = 1;
        for (int i = 0; i < retratosAfinidad.Length; i++)
        {
            var pez = cebo != null && cebo.especiesFavorecidas != null && i < cebo.especiesFavorecidas.Length
                ? cebo.especiesFavorecidas[i] : null;
            retratosAfinidad[i].sprite = pez != null ? pez.foto : null;
            retratosAfinidad[i].enabled = retratosAfinidad[i].sprite != null;
        }
        aviso.text = cebo != null
            ? $"Precio: $ {cebo.precio:N0} · {cebo.UsosPorCompra} usos por compra · Atracción ×{cebo.multiplicadorAparicion:0.#}."
            : "Sin bonificación de aparición.";
        int usosDisponibles = cebo != null ? CantidadUsos(cebo) : 0;
        textoEquipar.text = cebo == pesca.CeboEquipado ? "Equipada"
            : cebo == null ? "Quitar carnada"
            : usosDisponibles > 0 ? $"Equipar · {usosDisponibles} usos"
            : $"Comprar · $ {cebo.precio:N0} ({cebo.UsosPorCompra} usos)";
        equipar.interactable = cebo != pesca.CeboEquipado;
        for (int i = 0; i < tarjetas.Length; i++)
        {
            bool elegida = cebos[i] == cebo;
            bool equipada = cebos[i] != null && cebos[i] == pesca.CeboEquipado;
            int usos = cebos[i] != null ? CantidadUsos(cebos[i]) : 0;
            tarjetas[i].color = elegida || usos > 0 ? Crema : Nogal;
            EstiloExpedicion.Borde(tarjetas[i], elegida ? EstiloExpedicion.Dorado : Nogal, elegida ? 3 : 1);
            estados[i].text = equipada ? $"✓ Equipada · {usos} usos"
                : usos > 0 ? $"{usos} usos disponibles"
                : cebos[i] != null ? $"Comprar · $ {cebos[i].precio:N0}" : "Sin datos";
        }
    }

    private void Equipar()
    {
        if (!pesca.PuedeCambiarCebo) { aviso.text = "Termina el intento antes de cambiar de carnada."; return; }

        if (seleccionado != null && !TieneUsos(seleccionado))
        {
            if (EconomiaManager.Instance == null)
            {
                aviso.text = "No se encontró el saldo del jugador.";
                return;
            }
            if (!EconomiaManager.Instance.IntentarGastarOro(seleccionado.precio))
            {
                aviso.text = $"Saldo insuficiente. Necesitas $ {seleccionado.precio:N0}.";
                return;
            }
            AgregarUsos(seleccionado, seleccionado.UsosPorCompra);
        }

        if (!pesca.EquiparCebo(seleccionado)) { aviso.text = "No se pudo equipar esta carnada."; return; }
        ActualizarResumen();
        Cerrar();
    }

    public bool TieneUsos(CeboData cebo) => cebo != null && CantidadUsos(cebo) > 0;

    public bool ConsumirUso(CeboData cebo)
    {
        if (!TieneUsos(cebo)) return false;
        PlayerPrefs.SetInt(ClaveDe(cebo), CantidadUsos(cebo) - 1);
        PlayerPrefs.Save();
        return true;
    }

    private static int CantidadUsos(CeboData cebo)
    {
        if (cebo == null) return 0;
        string clave = ClaveDe(cebo);
        if (PlayerPrefs.HasKey(clave))
            return Mathf.Max(0, PlayerPrefs.GetInt(clave));

        // Migra las compras ilimitadas guardadas por la versión anterior a un paquete finito.
        bool compradoAntes = PlayerPrefs.GetInt(ClaveCompraAnterior + cebo.name, 0) == 1;
        if (compradoAntes || cebo.precio <= 0)
            AgregarUsos(cebo, cebo.UsosPorCompra);
        return PlayerPrefs.GetInt(clave, 0);
    }

    private static void AgregarUsos(CeboData cebo, int cantidad)
    {
        if (cebo == null || cantidad <= 0) return;
        PlayerPrefs.SetInt(ClaveDe(cebo), CantidadUsosSinMigrar(cebo) + cantidad);
        PlayerPrefs.Save();
    }

    private static int CantidadUsosSinMigrar(CeboData cebo) =>
        PlayerPrefs.GetInt(ClaveDe(cebo), 0);

    private static string ClaveDe(CeboData cebo) => ClaveUsos + cebo.name;

    private void ActualizarResumen()
    {
        var equipada = pesca.CeboEquipado;
        resumen.text = equipada != null
            ? $"CARNADA · {equipada.nombre} ({CantidadUsos(equipada)} usos)"
            : "CARNADA · Sin carnada";
        resumen.color = EstiloExpedicion.Papel;
    }

    private void OnDestroy()
    {
        if (pesca != null) pesca.CeboCambiado -= ActualizarResumen;
    }

    private static RectTransform Rect(Transform padre, string nombre, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform Panel(Transform padre, string nombre, Color color, float x0, float y0, float x1, float y1)
    {
        var rect = Rect(padre, nombre, x0, y0, x1, y1);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static Text Texto(Transform padre, string nombre, string contenido, int tamano, float x0, float y0, float x1, float y1)
    {
        var texto = Rect(padre, nombre, x0, y0, x1, y1).gameObject.AddComponent<Text>();
        EstiloExpedicion.Tipografia(texto, tamano, Tinta);
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = Mathf.Min(tamano, 16);
        texto.resizeTextMaxSize = tamano;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.raycastTarget = false;
        return texto;
    }

    private static Button Boton(Transform padre, string nombre, string etiqueta, float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction accion)
    {
        var rect = Panel(padre, nombre, Verde, x0, y0, x1, y1);
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = rect.GetComponent<Image>();
        boton.onClick.AddListener(accion);
        EstiloExpedicion.Boton(boton);
        if (!string.IsNullOrEmpty(etiqueta)) Texto(rect, "Etiqueta", etiqueta, 17, .04f, 0, .96f, 1).color = EstiloExpedicion.Papel;
        return boton;
    }
}
