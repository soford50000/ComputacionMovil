using UnityEngine;
using UnityEngine.UI;

/// <summary>Malla 2D sombreada, sin texturas ni objetos por segmento. Anillas e hilo siguen la misma curva.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CanaGrafico : MaskableGraphic
{
    private Vector2 origen = new Vector2(0.74f, -0.12f), extremo = new Vector2(0.50f, 0.81f);
    private float grosor = 1f, flexion = 0.32f, tension;
    private float escala;
    private const int Tramos = 96;
    private readonly Vector2[] curvaCache = new Vector2[Tramos + 1];
    private Rect rectCache;
    private bool curvaPendiente = true;
    private float vibracion;
    private float tiempoAgua = -1f;
    public ModeloCana ModeloActual { get; private set; }
    private bool EsShimano => ModeloActual == ModeloCana.ShimanoColtSniperLimited100H;
    private bool EsFx => ModeloActual == ModeloCana.ShimanoFx;
    private bool EsDaiwa => ModeloActual == ModeloCana.DaiwaDSK25BF662M;
    private static readonly float[] GuiasFx = { 0.37f, 0.58f, 0.75f, 0.87f, 0.95f, 1f };
    private float[] PosicionesGuias => EsFx ? GuiasFx : Guias;
    private static readonly float[] Guias = { 0.34f, 0.52f, 0.68f, 0.80f, 0.89f, 0.955f, 1f };
    private static readonly Color32 Negro = new Color32(18, 22, 24, 255);
    private static readonly Color32 Grafito = new Color32(83, 91, 96, 255);
    private static readonly Color32 Plata = new Color32(179, 187, 187, 255);
    private static readonly Color32 Cobre = new Color32(168, 100, 64, 255);
    private static readonly Color32 Espuma = new Color32(39, 43, 44, 255);

    public void EstablecerModelo(ModeloCana modelo)
    {
        if (ModeloActual == modelo) return;
        ModeloActual = modelo;
        SetVerticesDirty();
    }

    public void Configurar(Vector2 inicio, Vector2 fin, float ancho, float curva, float carga, float sacudida = 0f, float tiempo = -1f)
    {
        if (origen == inicio && extremo == fin && grosor == ancho && flexion == curva && tension == carga && vibracion == sacudida && tiempoAgua == tiempo) return;
        origen = inicio; extremo = fin; grosor = ancho; flexion = curva; tension = carga;
        vibracion = sacudida;
        tiempoAgua = tiempo;
        curvaPendiente = true;
        SetVerticesDirty();
    }

    // Integra la dirección de cada tramo conservando longitud, con el mango rígido.
    // El arco empieza justo al salir del mango y gira hacia la izquierda de pantalla.
    public Vector2 Punto(float t)
    {
        Rect r = rectTransform.rect;
        if (curvaPendiente || r != rectCache)
        {
            rectCache = r;
            curvaPendiente = false;
            Vector2 a = r.min + Vector2.Scale(r.size, origen);
            Vector2 b = r.min + Vector2.Scale(r.size, extremo);
            float anguloInicial = Mathf.Atan2(b.y - a.y, b.x - a.x);
            float largo = Vector2.Distance(a, b) / Tramos;
            curvaCache[0] = a;
            Vector2 punto = a;
            for (int i = 1; i <= Tramos; i++)
            {
                float u = Mathf.Clamp01(((i - 0.5f) / Tramos - 0.265f) / 0.735f);
                float angulo = anguloInicial + Mathf.Pow(u, 2.1f) * tension * flexion * 7.8f;
                punto += new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * largo;
                // Solo el último tercio oscila; valor y pendiente nulos en la unión.
                float punta = Mathf.Clamp01((i / (float)Tramos - 2f / 3f) * 3f);
                curvaCache[i] = punto + new Vector2(0.3f, 1f) * (r.height * vibracion * punta * punta);
            }
        }
        float indice = Mathf.Clamp01(t) * Tramos;
        int tramo = Mathf.Min(Tramos - 1, Mathf.FloorToInt(indice));
        return Vector2.LerpUnclamped(curvaCache[tramo], curvaCache[tramo + 1], indice - tramo);
    }

    private Vector2 Normal(float t)
    {
        Vector2 eje = (Punto(t + 0.001f) - Punto(t - 0.001f)).normalized;
        return new Vector2(eje.y, -eje.x);
    }

    private float Radio(float t) { return Mathf.Lerp(10f, 1.15f, Mathf.Pow(t, 0.72f)) * escala; }

    // Normal interior de la curva: soportes, aros y sedal están del mismo lado.
    private Vector2 LadoInferior(float t) { return -Normal(t); }

    public Vector2 SalidaSedal => CentroGuia(1f);
    public Vector2 EntradaAgua
    {
        get
        {
            Rect r = rectTransform.rect;
            // Anclaje bajo y a la izquierda de la punta base; no persigue sus vibraciones.
            Vector2 puntaBase = Punto(1f) - new Vector2(0.3f, 1f) * (r.height * vibracion);
            return puntaBase + new Vector2(-r.width * 0.04f, -r.height * 0.18f);
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        escala = Mathf.Min(rectTransform.rect.width / 1280f, rectTransform.rect.height / 720f) * grosor;
        if (escala <= 0f) return;

        // Hilo translúcido, detrás del cuerpo, con recorrido por el centro de cada guía.
        Color hilo = new Color(0.86f, 0.91f, 0.88f, 0.55f);
        Vector2 previo = Punto(0.22f) + LadoInferior(0.22f) * Radio(0.22f);
        foreach (float t in PosicionesGuias)
        {
            Vector2 centro = CentroGuia(t);
            Linea(vh, previo, centro, 0.85f * escala, hilo);
            previo = centro;
        }
        Vector2 agua = EntradaAgua;
        bool combate = tiempoAgua >= 0f;
        Vector2 direccionHilo = (agua - previo).normalized;
        Vector2 lateralHilo = new Vector2(-direccionHilo.y, direccionHilo.x);
        // Sin comba: solo un temblor transversal subpíxel, con extremos anclados.
        Vector2 control = Vector2.Lerp(previo, agua, 0.5f) + lateralHilo *
            (combate ? Mathf.Sin(tiempoAgua * 32f) * 0.5f : 0f) * escala;
        Vector2 anterior = previo;
        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;
            Vector2 p = (1 - t) * (1 - t) * previo + 2 * (1 - t) * t * control + t * t * agua;
            Linea(vh, anterior, p, 0.85f * escala, hilo);
            anterior = p;
        }
        if (combate) DibujarAgua(vh, agua);

        if (EsDaiwa) DibujarDaiwa(vh);
        else if (EsFx) DibujarShimanoFx(vh);
        else if (EsShimano) DibujarShimano(vh);
        else DibujarAbuGarcia(vh);

        foreach (float t in PosicionesGuias) Guia(vh, t);
    }

    private void DibujarAbuGarcia(VertexHelper vh)
    {
        Tubo(vh, 0, 1, 1f, Negro, Grafito, Plata);
        // Motivo de pequeños bloques oscuros sobre el grafito, como en la referencia.
        for (float t = 0.26f; t < 0.63f; t += 0.022f)
        {
            Parche(vh, t, t + 0.009f, -0.68f, -0.12f, Negro);
            Parche(vh, t + 0.006f, t + 0.016f, 0.3f, 0.64f, Grafito);
        }

        // Talón separado, tramo expuesto, empuñadura EVA y asiento con gatillo.
        Tubo(vh, 0f, 0.05f, 1.65f, Negro, Espuma, Grafito);
        Tubo(vh, 0.083f, 0.175f, 1.7f, Negro, Espuma, new Color32(65, 69, 69, 255));
        for (float t = 0.086f; t < 0.173f; t += 0.004f)
            Parche(vh, t, t + 0.0009f, -1.45f, 1.45f, new Color32(29, 33, 34, 130));
        Tubo(vh, 0.177f, 0.225f, 1.28f, Negro, Grafito, Plata);
        Parche(vh, 0.188f, 0.219f, -0.70f, 0.35f, Negro);
        Vector2 gatillo = Punto(0.182f), normal = Normal(0.182f);
        Triangulo(vh, gatillo - normal * 8f * escala,
            gatillo - normal * 27f * escala + (Punto(0.17f) - gatillo) * 0.4f,
            Punto(0.198f) - normal * 7f * escala, Grafito);
        Tubo(vh, 0.225f, 0.265f, 1.6f, Negro, Espuma, Grafito);
        Banda(vh, 0.078f, 0.004f, 1.12f, Plata);
        Banda(vh, 0.173f, 0.004f, 1.72f, Negro);
        Banda(vh, 0.224f, 0.005f, 1.48f, Negro);
        Banda(vh, 0.268f, 0.006f, 1.10f, Negro);
        Banda(vh, 0.274f, 0.0015f, 1.10f, Cobre);

    }

    private void DibujarShimano(VertexHelper vh)
    {
        // Blank negro satinado: reflejo estrecho, sin cuadros ni ataduras cobrizas.
        Color carbono = new Color32(32, 37, 43, 255);
        Color reflejo = new Color32(102, 112, 121, 255);
        Color eva = new Color32(28, 31, 36, 255);
        Tubo(vh, 0, 1, 1f, Negro, carbono, reflejo);
        for (float t = 0.28f; t < 0.52f; t += 0.008f)
            Parche(vh, t, t + 0.0012f, -0.82f, 0.82f, new Color32(108, 117, 126, 38));

        // Talón ensanchado y mango trasero EVA largo de una sola pieza.
        Tubo(vh, 0f, 0.018f, 1.85f, Negro, eva, carbono);
        Tubo(vh, 0.018f, 0.151f, 1.66f, Negro, eva, new Color32(55, 59, 65, 255));
        // Portacarrete cilíndrico de spinning: sin gatillo de casting.
        Tubo(vh, 0.151f, 0.194f, 1.30f, Negro, carbono, reflejo);
        Banda(vh, 0.151f, 0.003f, 1.40f, Plata);
        Banda(vh, 0.164f, 0.004f, 1.38f, Grafito);
        for (float t = 0.174f; t < 0.193f; t += 0.003f)
            Banda(vh, t, 0.0009f, 1.36f, Grafito);
        Banda(vh, 0.194f, 0.004f, 1.58f, Plata);

        // Empuñadura delantera larga y afinada, con collar plateado en la base.
        Tubo(vh, 0.198f, 0.259f, 1.74f, Negro, eva, new Color32(55, 59, 65, 255));
        Tubo(vh, 0.259f, 0.267f, 1.43f, Negro, eva, carbono);
        Banda(vh, 0.267f, 0.002f, 1.10f, Grafito);
        // Textura fina de espuma con contraste bajo para conservar el acabado mate.
        for (float t = 0.022f; t < 0.257f; t += 0.003f)
        {
            if (t > 0.149f && t < 0.200f) continue;
            Parche(vh, t, t + 0.0006f, -1.4f, 1.4f, new Color32(11, 15, 18, 50));
        }
        // Detalles plateados discretos del tramo de identificación.
        Banda(vh, 0.276f, 0.0018f, 1.02f, Plata);
        Parche(vh, 0.285f, 0.319f, -0.30f, -0.02f, Plata);
        Parche(vh, 0.286f, 0.302f, 0.12f, 0.27f, Grafito);
    }

    private void DibujarShimanoFx(VertexHelper vh)
    {
        Color cuerpo = new Color32(48, 51, 47, 255);
        Color reflejo = new Color32(113, 119, 112, 255);
        Color eva = new Color32(30, 32, 31, 255);
        Color niquel = new Color32(167, 158, 140, 255);
        Tubo(vh, 0, 1, 1f, Negro, cuerpo, reflejo);

        // Mango EVA largo, portacarrete negro sencillo y empuñadura frontal corta.
        Tubo(vh, 0f, 0.012f, 1.70f, Negro, eva, Grafito);
        Tubo(vh, 0.012f, 0.139f, 1.72f, Negro, eva, new Color32(59, 62, 59, 255));
        Tubo(vh, 0.139f, 0.220f, 1.24f, Negro, Negro, Grafito);
        // Dos collares anchos de níquel ligeramente cálido, visibles en las referencias.
        Banda(vh, 0.148f, 0.012f, 1.48f, niquel);
        Banda(vh, 0.211f, 0.013f, 1.51f, niquel);
        for (float t = 0.163f; t < 0.179f; t += 0.0032f)
            Banda(vh, t, 0.001f, 1.30f, Grafito);
        Parche(vh, 0.185f, 0.204f, -0.28f, 0.03f, new Color32(145, 149, 141, 255));
        Tubo(vh, 0.224f, 0.262f, 1.67f, Negro, eva, new Color32(59, 62, 59, 255));
        Banda(vh, 0.262f, 0.003f, 1.18f, Negro);
        // Pequeño acento dorado en la salida del mango, conservando el cuerpo oscuro.
        Parche(vh, 0.270f, 0.287f, -0.25f, 0.13f, new Color32(169, 144, 63, 255));
        for (float t = 0.019f; t < 0.258f; t += 0.004f)
        {
            if (t > 0.137f && t < 0.225f) continue;
            Parche(vh, t, t + 0.0006f, -1.4f, 1.4f, new Color32(12, 15, 13, 42));
        }
    }

    private void DibujarDaiwa(VertexHelper vh)
    {
        Color blank = new Color32(32, 38, 37, 255);
        Tubo(vh, 0, 1, 1f, Negro, blank, new Color32(108, 118, 113, 255));
        // Mango de corcho natural, remate negro y tramo de goma estriada.
        Corcho(vh, 0.009f, 0.153f, 1.65f);
        Tubo(vh, 0f, 0.009f, 1.72f, Negro, Negro, Grafito);
        Banda(vh, 0.008f, 0.002f, 1.72f, Plata);
        Tubo(vh, 0.153f, 0.192f, 1.46f, Negro, blank, Grafito);
        for (float t = 0.157f; t < 0.190f; t += 0.003f)
            Banda(vh, t, 0.0014f, 1.51f, Negro);

        // Portacarrete de casting con gatillo y dos collares de metal plateado.
        Tubo(vh, 0.192f, 0.239f, 1.27f, Negro, Negro, Grafito);
        Banda(vh, 0.192f, 0.009f, 1.52f, Plata);
        Banda(vh, 0.233f, 0.009f, 1.50f, Plata);
        Vector2 gatillo = Punto(0.211f), n = LadoInferior(0.211f);
        Triangulo(vh, gatillo + n * Radio(0.211f),
            gatillo + n * 28f * escala + (Punto(0.198f) - gatillo) * 0.55f,
            Punto(0.228f) + n * Radio(0.228f), Negro);
        Corcho(vh, 0.242f, 0.265f, 1.56f);
        Banda(vh, 0.265f, 0.005f, 1.20f, Negro);
        Banda(vh, 0.290f, 0.0015f, 1.02f, Plata);
        Banda(vh, 0.295f, 0.001f, 1.02f, Grafito);
        Parche(vh, 0.303f, 0.337f, -0.32f, -0.02f, Plata);
        Parche(vh, 0.307f, 0.320f, 0.10f, 0.28f, Grafito);
    }

    private void Corcho(VertexHelper vh, float inicio, float fin, float ancho)
    {
        Tubo(vh, inicio, fin, ancho, new Color32(95, 75, 45, 255),
            new Color32(181, 154, 104, 255), new Color32(225, 211, 168, 255));
        // Poros deterministas: no consumen Random ni alteran la selección de peces.
        int motas = Mathf.CeilToInt((fin - inicio) * 650f);
        for (int i = 0; i < motas; i++)
        {
            float t = Mathf.Lerp(inicio + 0.001f, fin - 0.002f, Mathf.Repeat(i * 0.618034f, 1f));
            float x = Mathf.Lerp(-ancho * 0.83f, ancho * 0.63f, Mathf.Repeat(i * 0.381966f, 1f));
            float largo = 0.0005f + (i % 3) * 0.0003f;
            Parche(vh, t, Mathf.Min(fin, t + largo), x, x + 0.12f + (i % 4) * 0.055f,
                new Color32(84, 65, 37, 115));
        }
    }

    private void DibujarAgua(VertexHelper vh, Vector2 centro)
    {
        // Ondas locales con centro fijo; no se desplaza ni se deforma el paisaje.
        for (int onda = 0; onda < 3; onda++)
        {
            float fase = Mathf.Repeat(tiempoAgua * 0.7f + onda / 3f, 1f);
            float radio = Mathf.Lerp(3f, 24f, fase) * escala;
            Color colorAgua = new Color(0.83f, 0.94f, 0.98f, (1f - fase) * 0.35f);
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2 / 40, b = (i + 1) * Mathf.PI * 2 / 40;
                Linea(vh, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.27f) * radio,
                    centro + new Vector2(Mathf.Cos(b), Mathf.Sin(b) * 0.27f) * radio, escala, colorAgua);
            }
        }
        for (int i = 0; i < 4; i++)
        {
            float fase = Mathf.Repeat(tiempoAgua * 1.1f + i * 0.23f, 1f);
            float salto = Mathf.Sin(fase * Mathf.PI);
            Vector2 gota = centro + new Vector2((i - 1.5f) * fase * 5f, salto * (5f + i)) * escala;
            Linea(vh, gota, gota + Vector2.up * 1.4f * escala, escala,
                new Color(0.88f, 0.95f, 1f, salto * 0.45f));
        }
    }

    private Vector2 CentroGuia(float t)
    {
        float altura = Mathf.Lerp(21f, 4f, t) * (EsShimano ? Mathf.Lerp(1.55f, 1f, t) : 1f);
        return Punto(t) + LadoInferior(t) * (Radio(t) + altura * escala);
    }

    private void Guia(VertexHelper vh, float t)
    {
        float inicio = Mathf.Max(0.27f, t - 0.012f);
        Banda(vh, inicio, Mathf.Min(0.024f, 1f - inicio), 1.12f, Negro);
        Color atadura = EsFx ? Negro : (EsShimano || EsDaiwa ? Grafito : Cobre);
        Banda(vh, inicio + 0.001f, 0.0018f, 1.15f, atadura);
        if (t < 0.98f) Banda(vh, t + 0.009f, 0.0018f, 1.15f, atadura);
        Vector2 n = LadoInferior(t), eje = new Vector2(-n.y, n.x), c = CentroGuia(t);
        float radio = Mathf.Lerp(11f, 3.1f, t) * escala * (EsShimano ? Mathf.Lerp(1.45f, 1f, t) : 1f);
        Vector2 pieA = Punto(inicio) + n * Radio(t);
        Vector2 pieB = Punto(Mathf.Min(1f, t + 0.015f)) + n * Radio(t);
        Linea(vh, pieA, c - eje * radio * 0.6f, 2.6f * escala, Negro);
        Linea(vh, pieB, c + eje * radio * 0.6f, 2.6f * escala, Negro);
        Linea(vh, pieA + n * escala, c - eje * radio * 0.6f, 0.8f * escala, EsFx ? Grafito : Plata);
        if (EsShimano)
            Linea(vh, pieB + n * escala, c + eje * radio * 0.6f, 0.9f * escala, Plata);
        // Aro hueco con interior cerámico oscuro y reflejo metálico exterior.
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2f / 32f, b = (i + 1) * Mathf.PI * 2f / 32f;
            Vector2 pa = c + n * (Mathf.Cos(a) * radio) + eje * (Mathf.Sin(a) * radio * 0.72f);
            Vector2 pb = c + n * (Mathf.Cos(b) * radio) + eje * (Mathf.Sin(b) * radio * 0.72f);
            Linea(vh, pa, pb, Mathf.Max(1.7f * escala, radio * 0.3f), Negro);
            Linea(vh, pa + (pa - c).normalized * 0.65f * escala,
                pb + (pb - c).normalized * 0.65f * escala, 0.8f * escala,
                Color.Lerp(EsFx ? Negro : Grafito, EsFx ? Grafito : Plata, 0.5f + 0.5f * Mathf.Cos(a + 1f)));
        }
    }

    private void Banda(VertexHelper vh, float t, float largo, float ancho, Color colorBanda)
    {
        Tubo(vh, t, t + largo, ancho, Negro, colorBanda, Color.Lerp(colorBanda, Color.white, 0.24f));
    }

    private void Tubo(VertexHelper vh, float inicio, float fin, float ancho, Color sombra, Color medio, Color luz)
    {
        int segmentos = Mathf.Max(2, Mathf.CeilToInt((fin - inicio) * 100));
        for (int i = 0; i < segmentos; i++)
        {
            float a = Mathf.Lerp(inicio, fin, i / (float)segmentos);
            float b = Mathf.Lerp(inicio, fin, (i + 1f) / segmentos);
            Vector2 pa = Punto(a), pb = Punto(b), na = Normal(a) * Radio(a) * ancho, nb = Normal(b) * Radio(b) * ancho;
            // Cuatro bandas de iluminación longitudinal simulan una sección redonda.
            Franja(vh, pa, pb, na, nb, -1f, -0.45f, sombra, medio);
            Franja(vh, pa, pb, na, nb, -0.45f, -0.12f, medio, luz);
            Franja(vh, pa, pb, na, nb, -0.12f, 0.52f, luz, medio);
            Franja(vh, pa, pb, na, nb, 0.52f, 1f, medio, sombra);
        }
    }

    private void Parche(VertexHelper vh, float a, float b, float izquierda, float derecha, Color c)
    {
        Franja(vh, Punto(a), Punto(b), Normal(a) * Radio(a), Normal(b) * Radio(b), izquierda, derecha, c, c);
    }

    private static void Franja(VertexHelper vh, Vector2 a, Vector2 b, Vector2 na, Vector2 nb, float x, float y, Color c1, Color c2)
    {
        int i = vh.currentVertCount;
        vh.AddVert(a + na * x, c1, Vector2.zero); vh.AddVert(b + nb * x, c1, Vector2.zero);
        vh.AddVert(b + nb * y, c2, Vector2.zero); vh.AddVert(a + na * y, c2, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }

    private static void Linea(VertexHelper vh, Vector2 a, Vector2 b, float ancho, Color c)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * ancho * 0.5f;
        Franja(vh, a, b, n, n, -1, 1, c, c);
    }

    private static void Triangulo(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int i = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
    }
}
