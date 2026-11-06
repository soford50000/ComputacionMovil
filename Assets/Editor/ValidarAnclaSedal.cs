using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Regresión del ancla: no abre ni guarda escenas del proyecto.</summary>
public static class ValidarAnclaSedal
{
    [MenuItem("Pesca/Validar ancla del sedal")]
    public static void Ejecutar()
    {
        var raiz = new GameObject("Prueba temporal del ancla");
        raiz.SetActive(false);
        try
        {
            var fondo = raiz.AddComponent<FondoAguaUI>();
            var imagenObjeto = new GameObject("Fondo de prueba", typeof(RectTransform), typeof(RawImage));
            imagenObjeto.transform.SetParent(raiz.transform, false);
            var imagen = imagenObjeto.GetComponent<RawImage>();
            imagen.rectTransform.sizeDelta = new Vector2(1280, 720);
            imagen.rectTransform.localScale = Vector3.one * 1.5f;
            typeof(FondoAguaUI).GetField("imagen", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fondo, imagen);
            var objeto = new GameObject("Caña de prueba", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanaGrafico));
            objeto.transform.SetParent(raiz.transform, false);
            var grafico = objeto.GetComponent<CanaGrafico>();
            grafico.rectTransform.sizeDelta = new Vector2(1280, 720);
            grafico.rectTransform.localScale = Vector3.one * 0.75f;
            grafico.ConfigurarAnclaAgua(fondo, 0.3f, 0.035f);

            // Recorte vertical y horizontal, con escalas distintas entre ambos Canvas.
            foreach (Rect uv in new[] { new Rect(0, 0, 1, 1), new Rect(0, 0.2f, 1, 0.6f), new Rect(0.1f, 0, 0.8f, 1) })
            {
                imagen.uvRect = uv;
                Vector2 ancla = grafico.EntradaAgua;
                Vector3 enFondo = imagen.rectTransform.InverseTransformPoint(grafico.rectTransform.TransformPoint(ancla));
                float yVisible = (enFondo.y - imagen.rectTransform.rect.yMin) / imagen.rectTransform.rect.height;
                Exigir(Mathf.Abs(uv.y + (yVisible + 0.035f) * uv.height - 0.433f) < 0.0001f,
                    "El ancla no respeta el límite UV y la profundidad.");
                for (int i = 0; i <= 10; i++)
                {
                    float carga = i / 10f;
                    grafico.Configurar(new Vector2(0.74f, -0.12f), new Vector2(0.5f, 0.81f), 1, 0.32f, carga, 0.003f * carga, 1);
                    grafico.ConfigurarSedal(null, 1, 0.09f * (1 - carga) * (1 - carga), 0.85f, 0.6f, 0, 1);
                    Exigir(Vector2.Distance(ancla, grafico.EntradaAgua) < 0.001f, "El ancla se mueve con la caña.");
                    using (var vertices = new VertexHelper())
                    {
                        typeof(CanaGrafico).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(grafico, new object[] { vertices });
                        // Siete tramos entre anillas y 24 tramos de Bézier; centro del extremo final.
                        UIVertex a = default, b = default;
                        vertices.PopulateUIVertex(ref a, (7 + 23) * 4 + 1);
                        vertices.PopulateUIVertex(ref b, (7 + 23) * 4 + 2);
                        Exigir(Vector2.Distance((a.position + b.position) * 0.5f, ancla) < 0.001f,
                            "La malla del sedal no termina en el ancla.");
                    }
                }
            }
            // Nuevo modo: fracciones del propio lienzo, sin depender de la escala del fondo.
            foreach (Vector2 tamano in new[] { new Vector2(1280, 720), new Vector2(1920, 1080), new Vector2(1600, 1000) })
            {
                grafico.rectTransform.sizeDelta = tamano;
                imagen.uvRect = new Rect(0, 0, 1, 1);
                grafico.ConfigurarAnclaAgua(fondo, 0.3f, 0.08f, 0.24f, true);
                for (int i = 0; i <= 10; i++)
                {
                    grafico.Configurar(new Vector2(0.74f, -0.12f), new Vector2(0.5f, 0.81f), 1, 0.32f, i / 10f);
                    Vector2 punto = grafico.EntradaAgua;
                    Rect rect = grafico.rectTransform.rect;
                    Exigir(Mathf.Abs((punto.x - rect.xMin) / rect.width - 0.3f) < 0.0001f,
                        "X del ancla no coincide con la fracción configurada.");
                    Exigir(Mathf.Abs((punto.y - rect.yMin) / rect.height - 0.24f) < 0.0001f,
                        "Y del ancla no coincide con la fracción configurada.");
                }
                grafico.ConfigurarAnclaAgua(fondo, 0.3f, 0.08f, 0.9f);
                float altura = (grafico.EntradaAgua.y - grafico.rectTransform.rect.yMin) / tamano.y;
                Exigir(altura <= 0.433f - 0.08f + 0.0001f, "El ancla configurada sobre la orilla no se limita bajo el agua.");
            }
            grafico.puntoObjetivoSedal = new Vector2(-80, -90);
            Exigir(grafico.EntradaAgua == new Vector2(-80, -90), "El objetivo externo no se respeta.");
            grafico.puntoObjetivoSedal = null;
            Exigir(grafico.EntradaAgua != new Vector2(-80, -90), "null no restaura el ancla del fondo.");
            Debug.Log("ANCLA_SEDAL_OK: 66 casos de tensión/recorte/resolución, límite bajo el agua y objetivo nullable.");
        }
        finally { UnityEngine.Object.DestroyImmediate(raiz); }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }
}
