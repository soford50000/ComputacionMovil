using UnityEngine;
using UnityEngine.Events;

/// <summary>Lenguaje visual compartido por los cuadernos y el equipo de la expedición.</summary>
public static class EstiloExpedicion
{
    public static readonly Color Madera = Hex("30251F");
    public static readonly Color Papel = Hex("F2E8D3");
    public static readonly Color PapelOscuro = Hex("DFD2B6");
    public static readonly Color Tinta = Hex("283D33");
    public static readonly Color Verde = Hex("294C3E");
    public static readonly Color Dorado = Hex("D6B36A");
    public static readonly Color Secundario = Hex("647164");
    public static readonly Color Velo = new Color(.025f, .055f, .04f, .78f);
    private static Font fuente;

    public static Font Fuente
    {
        get
        {
            if (fuente == null) fuente = Resources.Load<Font>("Fonts/Rye-Regular");
            return fuente ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    public static Color Hex(string valor)
    {
        ColorUtility.TryParseHtmlString("#" + valor, out var color);
        return color;
    }

    public static RectTransform Rect(Transform padre, string nombre, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        Ubicar(rect, x0, y0, x1, y1);
        return rect;
    }

    public static void Ubicar(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static UnityEngine.UI.Image Panel(Transform padre, string nombre, Color color,
        float x0, float y0, float x1, float y1)
    {
        var imagen = Rect(padre, nombre, x0, y0, x1, y1).gameObject.AddComponent<UnityEngine.UI.Image>();
        TemaZonaUI.Colorear(imagen, color);
        imagen.raycastTarget = false;
        return imagen;
    }

    public static void Tipografia(UnityEngine.UI.Text texto, int tamano, Color color, bool esTitulo = false)
    {
        texto.font = Fuente;
        texto.fontSize = tamano;
        texto.fontStyle = FontStyle.Normal;
        TemaZonaUI.Colorear(texto, color);
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = Mathf.Min(tamano, 16);
        texto.resizeTextMaxSize = tamano;
        texto.raycastTarget = false;
    }

    public static UnityEngine.UI.Text Texto(Transform padre, string nombre, string contenido, int tamano,
        Color color, float x0, float y0, float x1, float y1, bool esTitulo = false,
        TextAnchor alineacion = TextAnchor.MiddleLeft)
    {
        var texto = Rect(padre, nombre, x0, y0, x1, y1).gameObject.AddComponent<UnityEngine.UI.Text>();
        Tipografia(texto, tamano, color, esTitulo);
        texto.text = contenido;
        texto.alignment = alineacion;
        return texto;
    }

    public static void Borde(UnityEngine.UI.Image imagen, Color color, float grosor = 1)
    {
        var borde = imagen.GetComponent<UnityEngine.UI.Outline>();
        if (borde == null) borde = imagen.gameObject.AddComponent<UnityEngine.UI.Outline>();
        TemaZonaUI.Borde(borde, color);
        borde.effectDistance = new Vector2(grosor, -grosor);
        borde.useGraphicAlpha = true;
    }

    public static void Boton(UnityEngine.UI.Button boton, bool primario = false)
    {
        // Se aplica también a botones cuyo estilo se actualiza al equipar/comprar.
        boton.onClick.RemoveListener(SonidosJuego.PulsarBoton);
        boton.onClick.AddListener(SonidosJuego.PulsarBoton);
        var imagen = boton.targetGraphic as UnityEngine.UI.Image;
        if (imagen != null)
        {
            imagen.raycastTarget = true;
            if (primario) TemaZonaUI.Colorear(imagen, Dorado);
            Borde(imagen, primario ? Dorado : new Color(.65f, .57f, .39f, .55f));
        }
        boton.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colores = TemaZonaUI.EstadosOriginales(boton);
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1.12f, 1.10f, 1.02f);
        colores.selectedColor = colores.highlightedColor;
        colores.pressedColor = new Color(.78f, .84f, .76f);
        colores.disabledColor = new Color(.75f, .75f, .70f, .72f);
        colores.fadeDuration = .12f;
        TemaZonaUI.Estados(boton, colores);
    }

    public static UnityEngine.UI.Button Accion(Transform padre, string nombre, string etiqueta,
        float x0, float y0, float x1, float y1, UnityAction accion, bool primario = false)
    {
        var imagen = Panel(padre, nombre, primario ? Dorado : Verde, x0, y0, x1, y1);
        var boton = imagen.gameObject.AddComponent<UnityEngine.UI.Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);
        Boton(boton, primario);
        Texto(boton.transform, "Etiqueta", etiqueta, 18, primario ? Tinta : Papel,
            .04f, .05f, .96f, .95f, false, TextAnchor.MiddleCenter);
        return boton;
    }

    public static void Enmarcar(RectTransform panel)
    {
        var imagen = panel.GetComponent<UnityEngine.UI.Image>();
        TemaZonaUI.Colorear(imagen, Papel);
        Borde(imagen, Dorado, 2);
        var sombra = panel.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        sombra.effectColor = new Color(0, 0, 0, .28f);
        sombra.effectDistance = new Vector2(5, -7);
        Panel(panel, "Línea de cabecera", PapelOscuro, .035f, .795f, .965f, .798f);
        var marca = Texto(panel, "Marca de expedición", "", 12, Secundario,
            .04f, .932f, .75f, .976f);
        TemaZonaUI.Rotular(marca, "R O A D   T O   A M A Z O N A S", "S A N   A N D R É S   ·   C A R I B E");
    }

    public static void Cabecera(RectTransform panel, string tituloTexto, string subtitulo, UnityAction cerrar)
    {
        Enmarcar(panel);
        Texto(panel, "Título", tituloTexto, 36, Tinta, .04f, .84f, .78f, .932f, true);
        Texto(panel, "Ayuda", subtitulo, 17, Secundario, .04f, .802f, .79f, .845f);
        Cierre(panel, cerrar, Madera);
    }

    public static UnityEngine.UI.Button Cierre(Transform padre, UnityAction cerrar, Color tinta)
    {
        var cruz = Texto(padre, "Cerrar", "×", 36, tinta, 1, 1, 1, 1,
            false, TextAnchor.MiddleCenter);
        var rect = cruz.rectTransform;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-16, -16);
        // Área completa de pulsación, aunque solo se dibuja la X.
        rect.sizeDelta = new Vector2(56, 56);
        cruz.raycastTarget = true;
        var boton = cruz.gameObject.AddComponent<UnityEngine.UI.Button>();
        boton.targetGraphic = cruz;
        boton.onClick.AddListener(cerrar);
        Boton(boton);
        return boton;
    }

    public static void PrepararModal(GameObject modal, RectTransform panel)
    {
        var fondo = modal.GetComponent<UnityEngine.UI.Image>();
        if (fondo != null) { TemaZonaUI.Colorear(fondo, Velo); fondo.raycastTarget = true; }
        // Elevar sólo la ventana: los accesos del HUD mantienen su orden habitual.
        var lienzo = modal.GetComponent<Canvas>();
        if (lienzo == null)
        {
            lienzo = modal.AddComponent<Canvas>();
            lienzo.overrideSorting = true;
            lienzo.sortingOrder = modal.transform.parent.GetComponentInParent<EntradaExpedicion>() != null ? 111 : 110;
            modal.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        else lienzo.sortingOrder = 120;
        var animacion = modal.AddComponent<EntradaExpedicion>();
        animacion.Panel = panel;
    }
}
