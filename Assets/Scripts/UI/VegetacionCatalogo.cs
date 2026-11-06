using System.Collections.Generic;
using UnityEngine;

/// <summary>Fondo ornamental fijo que cubre el panel completo de colección o carnadas.</summary>
public sealed class VegetacionCatalogo : MonoBehaviour
{
    private readonly List<UnityEngine.UI.RawImage> ramas = new List<UnityEngine.UI.RawImage>();
    private readonly Texture2D[] imagenes = new Texture2D[4];
    private readonly Texture2D[] corales = new Texture2D[2];
    private RectTransform panel, capa;
    private Vector2 tamanoAnterior;
    private bool permitirCorales, temaMarino;

    public void Preparar(RectTransform fondo, bool mostrarCoralesEnSanAndres = false)
    {
        panel = fondo;
        permitirCorales = mostrarCoralesEnSanAndres;
        capa = EstiloExpedicion.Rect(panel, "Vegetación decorativa", 0, 0, 1, 1);
        // Cubre cabecera, márgenes y pie; el recorte pertenece al panel, no al scroll.
        capa.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        capa.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        var grupo = capa.gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        capa.SetAsFirstSibling();
        for (int i = 0; i < imagenes.Length; i++)
            imagenes[i] = Resources.Load<Texture2D>("DecoracionCatalogo/Rama" + (i + 1));
        if (permitirCorales)
            for (int i = 0; i < corales.Length; i++)
                corales[i] = Resources.Load<Texture2D>("DecoracionCatalogo/Coral" + (i + 1));
        ActualizarTema();
    }

    private void OnEnable() { Canvas.willRenderCanvases += Actualizar; ActualizarTema(); }
    private void OnDisable() { Canvas.willRenderCanvases -= Actualizar; }

    public void ActualizarTema()
    {
        if (capa == null) return;
        bool marino = TemaZonaUI.EsMarino;
        if (temaMarino != marino)
        {
            temaMarino = marino;
            // Viajar también debe sustituir las texturas si el panel conserva su tamaño.
            tamanoAnterior = Vector2.zero;
        }
        capa.name = marino && permitirCorales ? "Corales decorativos" : "Vegetación decorativa";
        capa.gameObject.SetActive(!marino || permitirCorales);
        Actualizar();
    }

    private void Actualizar()
    {
        if (panel == null || capa == null || !capa.gameObject.activeSelf) return;
        Vector2 tamano = panel.rect.size;
        if (tamano.x <= 0 || tamano.y <= 0 || tamano == tamanoAnterior) return;
        tamanoAnterior = tamano;

        int columnas = Mathf.Max(2, Mathf.CeilToInt(tamano.x / 260f));
        int filas = Mathf.Max(2, Mathf.CeilToInt(tamano.y / 220f));
        Vector2 celda = new Vector2(tamano.x / columnas, tamano.y / filas);
        var texturas = temaMarino ? corales : imagenes;
        int necesarias = (filas + 1) * (columnas + 1);
        while (ramas.Count < necesarias)
        {
            var rect = EstiloExpedicion.Rect(capa, "Rama decorativa " + (ramas.Count + 1), 0, 1, 0, 1);
            var imagen = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            // Sin texto, control seleccionable ni nodo de accesibilidad: solo decoración.
            imagen.raycastTarget = false;
            imagen.color = new Color(.91f, .95f, .86f, .42f);
            ramas.Add(imagen);
        }

        for (int i = 0; i < ramas.Count; i++)
        {
            var imagen = ramas[i];
            bool visible = i < necesarias;
            imagen.gameObject.SetActive(visible);
            if (!visible) continue;
            int fila = i / (columnas + 1), columna = i % (columnas + 1);
            int variante = fila * 7 + columna * 3;
            var textura = texturas[variante % texturas.Length];
            imagen.name = (temaMarino ? "Coral decorativo " : "Rama decorativa ") + (i + 1);
            imagen.color = temaMarino ? new Color(1f, 1f, 1f, .28f) : new Color(.91f, .95f, .86f, .42f);
            imagen.texture = textura;
            imagen.enabled = textura != null;
            if (textura == null) continue;

            float x = columna * celda.x;
            float y = fila * celda.y;
            float ancho = Mathf.Clamp(celda.x * (1.3f + variante % 5 * .12f), 260, 440);
            var rect = imagen.rectTransform;
            rect.anchoredPosition = new Vector2(x + (variante % 3 - 1) * 18, -y + (variante % 5 - 2) * 13);
            rect.sizeDelta = new Vector2(ancho, ancho * textura.height / textura.width);
            float giro = temaMarino ? (variante * 17 % 36) - 18 : (variante * 37 % 100) - 50;
            rect.localRotation = Quaternion.Euler(0, 0, giro);
            rect.localScale = new Vector3(variante % 2 == 0 ? 1 : -1, 1, 1);
        }
    }
}
