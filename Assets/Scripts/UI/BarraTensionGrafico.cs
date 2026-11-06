using UnityEngine;
using UnityEngine.UI;

/// <summary>Arco de tensión: rojo, amarillo, verde, amarillo y rojo, siempre visibles.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BarraTensionGrafico : MaskableGraphic
{
    private const int Tramos = 96;
    private float valor = 0.5f;
    private static readonly Color Rojo = new Color32(242, 45, 42, 255);
    private static readonly Color Amarillo = new Color32(255, 211, 42, 255);
    private static readonly Color Verde = new Color32(65, 215, 68, 255);

    public void EstablecerValor(float tension)
    {
        float nuevo = float.IsNaN(tension) || float.IsInfinity(tension) ? 0.5f : Mathf.Clamp01(tension);
        if (Mathf.Abs(valor - nuevo) < 0.0001f) return;
        valor = nuevo;
        SetVerticesDirty();
    }

    private static Color ColorZona(float t)
    {
        float distancia = Mathf.Abs(t - 0.5f) * 2f;
        if (distancia <= 0.24f) return Verde;
        if (distancia <= 0.52f) return Color.Lerp(Verde, Amarillo, (distancia - 0.24f) / 0.28f);
        if (distancia <= 0.66f) return Amarillo;
        return Color.Lerp(Amarillo, Rojo, Mathf.Clamp01((distancia - 0.66f) / 0.22f));
    }

    private Vector2 Punto(float t)
    {
        Rect r = rectTransform.rect;
        float margen = r.height * 0.12f;
        float angulo = Mathf.Lerp(160f, 20f, t) * Mathf.Deg2Rad;
        return new Vector2(r.center.x + Mathf.Cos(angulo) / Mathf.Cos(20f * Mathf.Deg2Rad) * (r.width * 0.5f - margen),
            r.yMin + margen + (Mathf.Sin(angulo) - Mathf.Sin(20f * Mathf.Deg2Rad)) /
            (1f - Mathf.Sin(20f * Mathf.Deg2Rad)) * (r.height - margen * 2f));
    }

    private Vector2 Normal(float t)
    {
        Vector2 direccion = (Punto(t + 0.0001f) - Punto(t - 0.0001f)).normalized;
        return new Vector2(-direccion.y, direccion.x);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (rectTransform.rect.width <= 0f || rectTransform.rect.height <= 0f) return;
        float unidad = rectTransform.rect.height / 132f;
        Franja(vh, 26f * unidad, new Vector2(0f, -2f * unidad), new Color32(5, 19, 23, 120), false);
        Franja(vh, 23f * unidad, Vector2.zero, new Color32(15, 42, 48, 255), false);
        Franja(vh, 19f * unidad, Vector2.zero, new Color32(99, 152, 157, 255), false);
        Franja(vh, 15f * unidad, Vector2.zero, Color.white, true);
        Franja(vh, 3f * unidad, new Vector2(0f, 4f * unidad), new Color(1f, 1f, 1f, 0.23f), false);

        // El marcador recorre el arco sin ocultar las zonas restantes.
        Vector2 indicador = Punto(valor);
        Disco(vh, indicador + Vector2.down * 1.5f * unidad, 10f * unidad, new Color(0f, 0f, 0f, 0.35f));
        Disco(vh, indicador, 8.5f * unidad, new Color32(245, 250, 233, 255));
        Disco(vh, indicador, 5.5f * unidad, new Color32(22, 44, 47, 255));
        Disco(vh, indicador, 3f * unidad, ColorZona(valor));
    }

    private void Franja(VertexHelper vh, float ancho, Vector2 desplazamiento, Color tinte, bool zonas)
    {
        float radio = ancho * 0.5f;
        for (int i = 0; i <= Tramos; i++)
        {
            float t = i / (float)Tramos;
            Vector2 punto = Punto(t) + desplazamiento;
            Vector2 normal = Normal(t) * radio;
            Color c = zonas ? ColorZona(t) : tinte;
            int indice = vh.currentVertCount;
            vh.AddVert(punto - normal, c, Vector2.zero);
            vh.AddVert(punto + normal, c, Vector2.zero);
            if (i == 0) continue;
            vh.AddTriangle(indice - 2, indice - 1, indice + 1);
            vh.AddTriangle(indice - 2, indice + 1, indice);
        }
        Disco(vh, Punto(0f) + desplazamiento, radio, zonas ? ColorZona(0f) : tinte);
        Disco(vh, Punto(1f) + desplazamiento, radio, zonas ? ColorZona(1f) : tinte);
    }

    private static void Disco(VertexHelper vh, Vector2 centro, float radio, Color c)
    {
        int inicio = vh.currentVertCount;
        vh.AddVert(centro, c, Vector2.zero);
        for (int i = 0; i <= 24; i++)
        {
            float angulo = i * Mathf.PI * 2f / 24f;
            vh.AddVert(centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio, c, Vector2.zero);
            if (i > 0) vh.AddTriangle(inicio, inicio + i, inicio + i + 1);
        }
    }
}
