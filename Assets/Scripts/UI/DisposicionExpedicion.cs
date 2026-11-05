using System.Collections.Generic;
using UnityEngine;

/// <summary>Conserva únicamente las anclas horizontales de los cuadernos.</summary>
public sealed class DisposicionExpedicion : MonoBehaviour
{
    private struct Elemento
    {
        public RectTransform rect;
        public Vector4 horizontal;
    }
    private readonly List<Elemento> elementos = new List<Elemento>();

    // Firma conservada para los menús existentes; las antiguas coordenadas verticales se ignoran.
    public void Agregar(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        elementos.Add(new Elemento { rect = rect,
            horizontal = new Vector4(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y) });
        Actualizar();
    }

    private void OnEnable() { Actualizar(); }
    private void OnRectTransformDimensionsChange() { Actualizar(); }

    private void Actualizar()
    {
        var espacio = ((RectTransform)transform).rect;
        if (espacio.width <= 0 || espacio.height <= 0) return;
        foreach (var elemento in elementos)
        {
            if (elemento.rect == null) continue;
            var anclas = elemento.horizontal;
            EstiloExpedicion.Ubicar(elemento.rect, anclas.x, anclas.y, anclas.z, anclas.w);
        }
    }
}
