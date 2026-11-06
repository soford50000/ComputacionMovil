using System.Collections.Generic;
using UnityEngine;

/// <summary>Conserva el estilo original de cada control y aplica la paleta de la zona por Canvas.</summary>
[DisallowMultipleComponent]
public sealed class TemaZonaUI : MonoBehaviour
{
    private readonly Dictionary<UnityEngine.UI.Graphic, Color> graficos = new Dictionary<UnityEngine.UI.Graphic, Color>();
    private readonly Dictionary<UnityEngine.UI.Outline, Color> bordes = new Dictionary<UnityEngine.UI.Outline, Color>();
    private readonly Dictionary<UnityEngine.UI.Button, UnityEngine.UI.ColorBlock> botones = new Dictionary<UnityEngine.UI.Button, UnityEngine.UI.ColorBlock>();
    private readonly Dictionary<UnityEngine.UI.Text, string[]> textos = new Dictionary<UnityEngine.UI.Text, string[]>();
    private static readonly Dictionary<Color32, Color> paleta = CrearPaleta();
    private FisheryManager gestor;

    public static bool EsMarino => FisheryManager.Instance != null &&
        FisheryManager.Instance.ZonaSanAndres != null &&
        FisheryManager.Instance.ZonaActual == FisheryManager.Instance.ZonaSanAndres;

    private static TemaZonaUI Obtener(Component elemento)
    {
        var canvas = elemento.GetComponentInParent<Canvas>(true);
        if (canvas == null) return null;
        var raiz = canvas.rootCanvas;
        var tema = raiz.GetComponent<TemaZonaUI>();
        return tema != null ? tema : raiz.gameObject.AddComponent<TemaZonaUI>();
    }

    public static void Colorear(UnityEngine.UI.Graphic elemento, Color original)
    {
        var tema = Obtener(elemento);
        if (tema != null) tema.graficos[elemento] = original;
        elemento.color = ColorLocal(original);
    }

    public static void Excluir(UnityEngine.UI.Graphic elemento)
    {
        var tema = Obtener(elemento);
        if (tema != null) tema.graficos.Remove(elemento);
    }

    public static void Borde(UnityEngine.UI.Outline elemento, Color original)
    {
        var tema = Obtener(elemento);
        if (tema != null) tema.bordes[elemento] = original;
        elemento.effectColor = ColorLocal(original);
    }

    public static UnityEngine.UI.ColorBlock EstadosOriginales(UnityEngine.UI.Button boton)
    {
        var tema = Obtener(boton);
        return tema != null && tema.botones.TryGetValue(boton, out var originales) ? originales : boton.colors;
    }

    public static void Estados(UnityEngine.UI.Button boton, UnityEngine.UI.ColorBlock originales)
    {
        var tema = Obtener(boton);
        if (tema != null) tema.botones[boton] = originales;
        boton.colors = EstadosLocales(originales);
    }

    public static void Rotular(UnityEngine.UI.Text texto, string amazonas, string mar)
    {
        var tema = Obtener(texto);
        if (tema != null) tema.textos[texto] = new[] { amazonas, mar };
        texto.text = EsMarino ? mar : amazonas;
    }

    private void OnEnable() { Conectar(); Actualizar(null); }
    private void Start() { Conectar(); Actualizar(null); }
    private void Conectar()
    {
        if (gestor != null) return;
        gestor = GetComponentInParent<FisheryManager>();
        if (gestor != null) gestor.ZonaCambiada += Actualizar;
    }
    private void OnDisable()
    {
        if (gestor != null) gestor.ZonaCambiada -= Actualizar;
        gestor = null;
    }

    public void Actualizar(ZonaData zona)
    {
        Limpiar(graficos); Limpiar(bordes); Limpiar(botones); Limpiar(textos);
        foreach (var par in graficos) par.Key.color = ColorLocal(par.Value);
        foreach (var par in bordes) par.Key.effectColor = ColorLocal(par.Value);
        foreach (var par in botones) par.Key.colors = EstadosLocales(par.Value);
        foreach (var par in textos) par.Key.text = par.Value[EsMarino ? 1 : 0];
        foreach (var fondo in GetComponentsInChildren<FondoNogalCatalogo>(true)) fondo.ActualizarTema();
        foreach (var decoracion in GetComponentsInChildren<VegetacionCatalogo>(true)) decoracion.ActualizarTema();
    }

    private static void Limpiar<T, V>(Dictionary<T, V> enlaces) where T : Object
    {
        var eliminados = new List<T>();
        foreach (var par in enlaces) if (par.Key == null) eliminados.Add(par.Key);
        foreach (var clave in eliminados) enlaces.Remove(clave);
    }

    private static UnityEngine.UI.ColorBlock EstadosLocales(UnityEngine.UI.ColorBlock originales)
    {
        if (!EsMarino) return originales;
        originales.highlightedColor = new Color(.90f, 1f, 1f);
        originales.selectedColor = originales.highlightedColor;
        originales.pressedColor = new Color(.70f, .86f, .94f);
        // El blanco identifica las tarjetas bloqueadas que conservan su contraste.
        if (originales.disabledColor != Color.white)
            originales.disabledColor = new Color(.80f, .88f, .93f, .85f);
        return originales;
    }

    public static Color ColorLocal(Color original)
    {
        if (!EsMarino) return original;
        if (paleta.TryGetValue((Color32)original, out var marino)) return marino;
        if (original.r < .03f && original.g < .06f && original.b < .05f && original.a > .7f)
            return new Color(.015f, .06f, .14f, original.a);
        if (original == new Color(.65f, .57f, .39f, .55f))
            return new Color(.20f, .53f, .67f, .7f);
        return original; // Fotografías, transparencias y colores ajenos a la paleta se conservan.
    }

    private static Dictionary<Color32, Color> CrearPaleta()
    {
        var colores = new Dictionary<Color32, Color>();
        Agregar(colores, "103D59", "30251F", "39251B", "2C1810", "2A1D10", "283D33");
        Agregar(colores, "E9F7FC", "F2E8D3", "F2E8CF", "F4E9C8", "F5E9D0");
        Agregar(colores, "C6E4EF", "DFD2B6");
        Agregar(colores, "B0D6E5", "B38F5C");
        Agregar(colores, "52869D", "8F6F42");
        Agregar(colores, "0A596E", "294C3E", "294735", "456B48");
        Agregar(colores, "A5E2EB", "D6B36A", "D4AA5E", "F2B632");
        Agregar(colores, "315A70", "647164", "5A4326", "332411");
        Agregar(colores, "8EBACD", "64765A");
        Agregar(colores, "07546B", "886119");
        Agregar(colores, "7A4C00", "B37D00");
        Agregar(colores, "35546B", "526358");
        Agregar(colores, "225F4A", "307539");
        Agregar(colores, "205589", "2864A6");
        Agregar(colores, "6B2E91", "8541B0");
        return colores;
    }

    private static void Agregar(Dictionary<Color32, Color> colores, string destino, params string[] origenes)
    {
        foreach (var origen in origenes) colores[(Color32)EstiloExpedicion.Hex(origen)] = EstiloExpedicion.Hex(destino);
    }
}
