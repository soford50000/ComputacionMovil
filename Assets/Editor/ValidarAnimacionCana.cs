using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pruebas deterministas sin abrir escenas, entrar en Play ni modificar progreso.</summary>
public static class ValidarAnimacionCana
{
    private delegate void PasoResorte(ref float posicion, ref float velocidad, float objetivo,
        float respuesta, float amortiguacion, float delta);

    [MenuItem("Pesca/Validar animacion procedural de cana")]
    public static void Ejecutar()
    {
        var paso = (PasoResorte)Delegate.CreateDelegate(typeof(PasoResorte),
            typeof(CanaVisual).GetMethod("IntegrarResorte", BindingFlags.Static | BindingFlags.NonPublic));
        float referencia = 0f;
        foreach (int fps in new[] { 30, 60, 120 })
        {
            float posicion = 0f, velocidad = 0f, delta = 1f / fps;
            for (int i = 0; i < fps; i++) paso(ref posicion, ref velocidad, 1f, 0.2f, 0.55f, delta);
            Exigir(Mathf.Abs(posicion - 1f) < 0.01f, "El resorte no alcanza la carga.");
            if (fps == 30) referencia = posicion;
            else Exigir(Mathf.Abs(posicion - referencia) < 0.0001f, "Respuesta dependiente de FPS.");
            float minimo = posicion;
            for (int i = 0; i < fps * 2; i++)
            {
                paso(ref posicion, ref velocidad, 0f, 0.2f, 0.55f, delta);
                minimo = Mathf.Min(minimo, posicion);
            }
            Exigir(minimo < -0.05f && minimo > -0.25f, "Falta el pequeño rebote de descarga.");
            Exigir(Mathf.Abs(posicion) < 0.001f, "El resorte no se estabiliza.");
            posicion = 0.5f;
            velocidad = 4f;
            float maximo = posicion;
            for (int i = 0; i < fps * 2; i++)
            {
                paso(ref posicion, ref velocidad, 0.5f, 0.2f, 0.55f, delta);
                maximo = Mathf.Max(maximo, posicion);
            }
            Exigir(maximo > 0.65f && Mathf.Abs(posicion - 0.5f) < 0.001f, "El impulso no produce latigazo y recuperación.");
            float antes = posicion, velocidadAntes = velocidad;
            paso(ref posicion, ref velocidad, 0f, 0.2f, 0.55f, 0f);
            Exigir(posicion == antes && velocidad == velocidadAntes, "El resorte avanza en pausa.");
            paso(ref posicion, ref velocidad, 1f, 0.2f, 0.55f, 2f);
            Exigir(Mathf.Abs(posicion - 1f) < 0.001f, "El resorte falla tras un frame largo.");
        }

        var objeto = new GameObject("Validación temporal caña", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanaGrafico));
        try
        {
            var grafico = objeto.GetComponent<CanaGrafico>();
            grafico.rectTransform.sizeDelta = new Vector2(1920, 1080);
            grafico.ConfigurarFlexion(0.25f, 2f, 0.045f, false);
            Vector2 inicio = new Vector2(0.74f, -0.12f), fin = new Vector2(0.5f, 0.81f);
            grafico.Configurar(inicio, fin, 1f, 1.35f, 0f);
            Vector2 baseReposo = grafico.Punto(0.2f), puntaReposo = grafico.Punto(1f);
            float longitudEsperada = Vector2.Scale(fin - inicio, grafico.rectTransform.rect.size).magnitude;
            var poblar = (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), grafico,
                typeof(CanaGrafico).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic));
            using (var vertices = new VertexHelper())
            {
                foreach (float carga in new[] { -0.2f, 0f, 0.5f, 1f, 1.4f })
                {
                    grafico.Configurar(inicio, fin, 1f, 1.35f, carga);
                    Exigir(Vector2.Distance(baseReposo, grafico.Punto(0.2f)) < 0.001f, "La base se flexiona.");
                    float longitud = 0f;
                    for (int i = 1; i <= 96; i++) longitud += Vector2.Distance(grafico.Punto((i - 1f) / 96), grafico.Punto(i / 96f));
                    Exigir(Mathf.Abs(longitud - longitudEsperada) < 0.01f, "La caña cambia de longitud.");
                    if (carga == 1f) Exigir(Vector2.Distance(puntaReposo, grafico.Punto(1f)) > 200f, "Flexión demasiado rígida a tensión alta.");
                    float holgura = 1f - Mathf.Clamp01(carga);
                    grafico.ConfigurarSedal(null, 1f, 0.09f * holgura * holgura, 0.85f, 0.6f, 0f, 1f);
                    poblar(vertices);
                    Exigir(vertices.currentVertCount < 65000, "La malla supera el límite de índices de 16 bits.");
                    UIVertex a = default, b = default;
                    // Los primeros siete quads unen anillas; después vienen los 24 del sedal exterior.
                    vertices.PopulateUIVertex(ref a, 7 * 4);
                    vertices.PopulateUIVertex(ref b, 7 * 4 + 3);
                    Exigir(Vector2.Distance((a.position + b.position) * 0.5f, grafico.SalidaSedal) < 0.001f, "El sedal no sigue la punta.");
                    vertices.PopulateUIVertex(ref a, 30 * 4 + 1);
                    vertices.PopulateUIVertex(ref b, 30 * 4 + 2);
                    Exigir(Vector2.Distance((a.position + b.position) * 0.5f, grafico.EntradaAgua) < 0.001f, "El sedal pierde su ancla.");
                    vertices.PopulateUIVertex(ref a, 18 * 4 + 1);
                    vertices.PopulateUIVertex(ref b, 18 * 4 + 2);
                    Vector2 medio = (a.position + b.position) * 0.5f;
                    Vector2 recta = (grafico.SalidaSedal + grafico.EntradaAgua) * 0.5f;
                    Exigir(Mathf.Abs(medio.y - (recta.y - 1080f * 0.045f * holgura * holgura)) < 0.01f,
                        "La holgura no disminuye con la tensión.");
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(objeto); }
        ValidarAnclaSedal.Ejecutar();
        Debug.Log("ANIMACION_CANA_OK: resorte a 30/60/120 FPS, pausa, frame largo, rebote, impulso, longitud y sedal a 1920x1080; regresión de ancla superada.");
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }
}
