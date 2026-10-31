using System.Collections.Generic;
using UnityEngine;

/// <summary>Alterna las anclas del cuaderno al pasar de una pantalla apaisada a vertical.</summary>
public sealed class DisposicionExpedicion : MonoBehaviour
{
    private struct Elemento
    {
        public RectTransform rect;
        public Vector4 horizontal, vertical;
    }
    private readonly List<Elemento> elementos = new List<Elemento>();

    public void Agregar(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        elementos.Add(new Elemento { rect = rect,
            horizontal = new Vector4(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y),
            vertical = new Vector4(x0, y0, x1, y1) });
        Actualizar();
    }

    private void OnEnable() { Actualizar(); }
    private void OnRectTransformDimensionsChange() { Actualizar(); }

    private void Actualizar()
    {
        var espacio = ((RectTransform)transform).rect;
        if (espacio.width <= 0 || espacio.height <= 0) return;
        bool vertical = espacio.height > espacio.width;
        foreach (var elemento in elementos)
        {
            if (elemento.rect == null) continue;
            var anclas = vertical ? elemento.vertical : elemento.horizontal;
            EstiloExpedicion.Ubicar(elemento.rect, anclas.x, anclas.y, anclas.z, anclas.w);
        }
    }
}
