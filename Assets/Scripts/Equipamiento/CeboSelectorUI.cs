using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Selector de una sola carnada, disponible antes de lanzar.</summary>
public sealed class CeboSelectorUI : MonoBehaviour
{
    private const string ClaveCompra = "RoadToAmazonas.CebosComprados.v1.";
    private TensionSystem pesca;
    private CeboData[] cebos;
    private CeboData seleccionado;
    private GameObject ventana;
    private Text resumen, titulo, detalle, afinidades, aviso, textoEquipar;
    private Image foto;
    private Button abrir, equipar;
    private Image[] tarjetas;
    public bool EstaAbierto => ventana != null && ventana.activeSelf;

    private static readonly Color Crema = new Color32(244, 233, 200, 255);
    private static readonly Color Verde = new Color32(41, 71, 53, 255);
    private static readonly Color Madera = new Color32(44, 24, 16, 255);

    private void Awake()
    {
        pesca = GetComponent<TensionSystem>();
        if (pesca == null) { enabled = false; return; }
        var catalogo = Resources.Load<CatalogoCebos>("CatalogoCebos");
        cebos = catalogo != null && catalogo.cebos != null ? catalogo.cebos : new CeboData[0];
        CrearInterfaz();
        pesca.CeboCambiado += ActualizarResumen;
        ActualizarResumen();
    }

    private void Update()
    {
        abrir.interactable = pesca.PuedeCambiarCebo;
        if (!pesca.PuedeCambiarCebo && EstaAbierto) Cerrar();
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
        acceso.anchoredPosition = new Vector2(24, -90);
        acceso.sizeDelta = new Vector2(250, 58);
        resumen = Texto(abrir.transform, "Carnada equipada", "", 17, .04f, .05f, .96f, .95f);

        ventana = Panel(lienzo.transform, "Menú de carnadas", new Color(0, 0, 0, .86f), 0, 0, 1, 1).gameObject;
        var panel = Panel(ventana.transform, "Panel de carnadas", Madera, .04f, .05f, .96f, .95f);
        Texto(panel, "Título", "CARNADAS", 30, .025f, .88f, .72f, .98f);
        Texto(panel, "Ayuda", "Elige qué peces quieres encontrar con más frecuencia.", 17, .025f, .81f, .75f, .89f);
        Boton(panel, "Cerrar carnadas", "CERRAR", .84f, .9f, .975f, .97f, Cerrar);

        tarjetas = new Image[cebos.Length];
        int filas = Mathf.Max(1, Mathf.CeilToInt(cebos.Length / 2f));
        float alto = .67f / filas;
        for (int i = 0; i < cebos.Length; i++)
        {
            var cebo = cebos[i];
            float x = .025f + i % 2 * .305f;
            float y = .80f - i / 2 * alto;
            var boton = Boton(panel, "Carnada " + (cebo != null ? cebo.nombre : "Sin datos"), "",
                x, y - alto + .012f, x + .292f, y, () => Mostrar(cebo));
            tarjetas[i] = boton.targetGraphic as Image;
            var imagen = Panel(boton.transform, "Imagen", new Color32(64, 53, 38, 255), .025f, .08f, .35f, .92f).GetComponent<Image>();
            imagen.raycastTarget = false;
            if (cebo != null && cebo.icono != null)
            {
                imagen.sprite = cebo.icono;
                imagen.preserveAspect = true;
                imagen.color = Color.white;
            }
            else Texto(imagen.transform, "Imagen provisional", "CEBO", 15, 0, 0, 1, 1);
            string precio = cebo == null ? "Sin datos" : EstaComprado(cebo) ? "COMPRADO" : $"{cebo.precio:N0} ORO";
            Texto(boton.transform, "Nombre", cebo != null ? cebo.nombre + "\n" + precio : "Sin datos",
                17, .38f, .08f, .97f, .92f);
        }
        Boton(panel, "Sin carnada", "SIN CARNADA", .025f, .025f, .315f, .1f, () => Mostrar(null));

        var ficha = Panel(panel, "Ficha de carnada", Verde, .66f, .025f, .975f, .80f);
        titulo = Texto(ficha, "Nombre de carnada", "", 23, .05f, .88f, .95f, .98f);
        foto = Panel(ficha, "Foto de carnada", Color.clear, .32f, .66f, .68f, .87f).GetComponent<Image>();
        foto.preserveAspect = true;
        foto.raycastTarget = false;
        detalle = Texto(ficha, "Descripción", "", 15, .05f, .54f, .95f, .66f);
        afinidades = Texto(ficha, "Peces favorecidos", "", 17, .06f, .18f, .94f, .54f);
        afinidades.alignment = TextAnchor.UpperLeft;
        aviso = Texto(ficha, "Aviso", "", 13, .05f, .105f, .95f, .18f);
        equipar = Boton(ficha, "Equipar carnada", "", .07f, .02f, .93f, .10f, Equipar);
        textoEquipar = Texto(equipar.transform, "Texto equipar", "EQUIPAR", 19, 0, 0, 1, 1);
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
            lista.AppendLine("FAVORECE LA APARICIÓN DE:");
            foreach (var pez in cebo.especiesFavorecidas)
                if (pez != null) lista.AppendLine("• " + pez.nombreEspecie.Split('(')[0].Trim());
        }
        afinidades.text = lista.ToString();
        aviso.text = cebo != null
            ? $"Precio: {cebo.precio:N0} oro · Atracción ×{cebo.multiplicadorAparicion:0.#}."
            : "Sin bonificación de aparición.";
        bool comprado = cebo == null || EstaComprado(cebo);
        textoEquipar.text = cebo == pesca.CeboEquipado ? "EQUIPADA"
            : cebo == null ? "QUITAR CARNADA"
            : comprado ? "EQUIPAR" : $"COMPRAR · {cebo.precio:N0} ORO";
        equipar.interactable = cebo != pesca.CeboEquipado;
        for (int i = 0; i < tarjetas.Length; i++)
            tarjetas[i].color = cebos[i] == cebo ? new Color32(98, 115, 62, 255) : Verde;
    }

    private void Equipar()
    {
        if (!pesca.PuedeCambiarCebo) { aviso.text = "Termina el intento antes de cambiar de carnada."; return; }

        if (seleccionado != null && !EstaComprado(seleccionado))
        {
            if (EconomiaManager.Instance == null)
            {
                aviso.text = "No se encontró el saldo del jugador.";
                return;
            }
            if (!EconomiaManager.Instance.IntentarGastarOro(seleccionado.precio))
            {
                aviso.text = $"Saldo insuficiente. Necesitas {seleccionado.precio:N0} monedas.";
                return;
            }
            MarcarComoComprado(seleccionado);
        }

        if (!pesca.EquiparCebo(seleccionado)) { aviso.text = "No se pudo equipar esta carnada."; return; }
        ActualizarResumen();
        Cerrar();
    }

    private static string ClaveDe(CeboData cebo) => ClaveCompra + cebo.name;

    private static bool EstaComprado(CeboData cebo)
    {
        return cebo != null && PlayerPrefs.GetInt(ClaveDe(cebo), cebo.precio <= 0 ? 1 : 0) == 1;
    }

    private static void MarcarComoComprado(CeboData cebo)
    {
        if (cebo == null) return;
        PlayerPrefs.SetInt(ClaveDe(cebo), 1);
        PlayerPrefs.Save();
    }

    private void ActualizarResumen()
    {
        resumen.text = "CARNADA · " + (pesca.CeboEquipado != null ? pesca.CeboEquipado.nombre : "Sin carnada");
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
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.text = contenido;
        texto.color = Crema;
        texto.fontSize = tamano;
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 10;
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
        if (!string.IsNullOrEmpty(etiqueta)) Texto(rect, "Etiqueta", etiqueta, 17, 0, 0, 1, 1);
        return boton;
    }
}
