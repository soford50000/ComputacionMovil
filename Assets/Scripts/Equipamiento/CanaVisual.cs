using UnityEngine;
using UnityEngine.UI;

/// <summary>Los modelos comparten pose, tensión y animación; solo cambia su construcción visual.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TensionSystem))]
public sealed class CanaVisual : MonoBehaviour
{
    [Header("Caña equipada · primera persona")]
    [SerializeField] private Vector2 basePantalla = new Vector2(0.74f, -0.12f);
    [SerializeField] private Vector2 puntaPantalla = new Vector2(0.50f, 0.81f);
    [SerializeField, Range(0.5f, 1.5f)] private float grosor = 1f;
    [SerializeField, Range(0.1f, 0.5f)] private float flexionMaxima = 0.32f;
    [SerializeField, Min(0.05f)] private float suavizado = 0.22f;
    [Header("Combate con pez pesado")]
    [SerializeField, Range(0.2f, 1f)] private float amortiguacion = 0.48f;
    [SerializeField, Range(0f, 0.01f)] private float amplitudVibracion = 0.0015f;

    private TensionSystem pesca;
    private GameObject lienzo;
    private CanaGrafico grafico;
    private BarraTensionGrafico barraTension;
    private float tension, velocidad, tiempoTiron, tensionExterna;
    private bool usarTensionExterna, estabaTirando;
    private float vibracion, poseCombate;
    private CanaData equipoVisible;

    public float TensionVisual => tension;

    private void Awake()
    {
        pesca = GetComponent<TensionSystem>();
        lienzo = new GameObject("Abu Garcia Vengeance · primera persona", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);
        var canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -50; // Encima del agua (-100), detrás de todos los controles.
        var scaler = lienzo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var modelo = new GameObject("Malla flexible con anillas", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanaGrafico));
        modelo.transform.SetParent(lienzo.transform, false);
        grafico = modelo.GetComponent<CanaGrafico>();
        grafico.rectTransform.anchorMin = Vector2.zero;
        grafico.rectTransform.anchorMax = Vector2.one;
        grafico.rectTransform.offsetMin = grafico.rectTransform.offsetMax = Vector2.zero;
        grafico.raycastTarget = false;
        var barra = new GameObject("Barra de tensión · zona estable central", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(BarraTensionGrafico));
        barra.transform.SetParent(lienzo.transform, false);
        barraTension = barra.GetComponent<BarraTensionGrafico>();
        barraTension.raycastTarget = false;
        barraTension.rectTransform.anchorMin = barraTension.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        barraTension.rectTransform.pivot = new Vector2(0.5f, 0f);
        barra.SetActive(false);
        ActualizarModelo();
    }

    /// <summary>RF4: carga local de la punta de 0 a 1; conserva la pose general de combate.</summary>
    public void EstablecerTension(float valor)
    {
        usarTensionExterna = true;
        tensionExterna = float.IsNaN(valor) || float.IsInfinity(valor) ? 0f : Mathf.Clamp01(valor);
    }

    public void UsarTironesSimulados() { usarTensionExterna = false; }

    private void LateUpdate()
    {
        bool tirando = pesca != null && pesca.isActiveAndEnabled && pesca.EnForcejeo;
        if (tirando && !estabaTirando) tiempoTiron = 0f;
        estabaTirando = tirando;
        float objetivo = 0f;
        if (tirando)
        {
            tiempoTiron += Time.deltaTime;
            // Carga sostenida; los tirones breves se superponen sin descargar la caña.
            float pulso = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(tiempoTiron * 4.6f), 6f);
            objetivo = usarTensionExterna ? tensionExterna :
                Mathf.Clamp01(0.76f + pulso * 0.12f + (pesca.EstaPresionado ? 0.08f : 0f));
        }
        // Resorte subamortiguado: solución analítica estable incluso con FPS bajos.
        // A diferencia de SmoothDamp permite un pequeño sobrepaso y retorno elástico.
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        float omega = 2f / Mathf.Max(0.05f, suavizado);
        float zeta = Mathf.Clamp(amortiguacion, 0.2f, 0.99f);
        float wd = omega * Mathf.Sqrt(1f - zeta * zeta);
        float error = tension - objetivo;
        float auxiliar = (velocidad + zeta * omega * error) / wd;
        float seno = Mathf.Sin(wd * dt), coseno = Mathf.Cos(wd * dt);
        float decaimiento = Mathf.Exp(-zeta * omega * dt);
        tension = objetivo + decaimiento * (error * coseno + auxiliar * seno);
        velocidad = decaimiento * (velocidad * coseno -
            (zeta * omega * auxiliar + wd * error) * seno);
        // Se conserva un pequeño rebote al descargar, sin flexión inversa exagerada.
        tension = Mathf.Clamp(tension, -0.025f, 1.06f);
        if (objetivo == 0f && Mathf.Abs(tension) < 0.0005f && Mathf.Abs(velocidad) < 0.005f)
        { tension = 0f; velocidad = 0f; }
        vibracion = tirando ? amplitudVibracion * Mathf.Clamp01(tension) *
            (0.65f * Mathf.Sin(tiempoTiron * 27f) + 0.35f * Mathf.Sin(tiempoTiron * 43f)) : 0f;
        // Solo cambia de pose al entrar o salir del combate. Los tirones no la mueven.
        poseCombate = Mathf.MoveTowards(poseCombate, tirando ? 0.8f : 0f, Time.deltaTime * 2f);
        ActualizarModelo();
        ActualizarBarraTension(tirando);
    }

    private void ActualizarBarraTension(bool visible)
    {
        Rect r = grafico.rectTransform.rect;
        float escala = Mathf.Min(r.width / 1280f, r.height / 720f);
        // Se mide toda la curva: durante el combate la punta cae y deja de ser
        // el punto más alto. El margen también protege las anillas y el borde.
        float alturaCana = r.yMin;
        for (int i = 0; i <= 96; i++)
            alturaCana = Mathf.Max(alturaCana, grafico.Punto(i / 96f).y);
        float baseBarra = alturaCana + 26f * escala;
        float techo = r.yMin + r.height * 0.83f; // Reserva el mensaje de pique.
        float ajuste = Mathf.Min(escala, (techo - baseBarra) / 132f);
        // Espera a que la pose de combate deje espacio suficiente para el arco.
        visible &= ajuste >= escala * 0.45f && escala > 0f;
        barraTension.gameObject.SetActive(visible);
        if (!visible) return;
        barraTension.rectTransform.sizeDelta = new Vector2(420f, 132f) * ajuste;
        barraTension.rectTransform.anchoredPosition = new Vector2(0f, baseBarra);
        // El marcador sigue el valor real que controla RF4; la caña conserva
        // su flexión suavizada para que el movimiento visual no sea brusco.
        barraTension.EstablecerValor(usarTensionExterna ? tensionExterna : tension);
    }

    private void ActualizarModelo()
    {
        if (grafico == null) return;
        CanaData equipada = pesca != null ? pesca.CanaEquipada : null;
        if (equipoVisible != equipada)
        {
            equipoVisible = equipada;
            grafico.EstablecerModelo(equipada != null ? equipada.modeloVisual : ModeloCana.AbuGarciaVengeance);
            lienzo.name = (equipada != null ? equipada.nombreCana : "Caña") + " · primera persona";
        }
        grafico.Configurar(basePantalla, puntaPantalla, grosor, flexionMaxima, poseCombate, vibracion - (tension - poseCombate) * 0.012f, estabaTirando ? tiempoTiron : -1f);
    }

    private void OnEnable() { if (lienzo != null) lienzo.SetActive(true); }
    private void OnDisable()
    {
        tension = velocidad = tiempoTiron = vibracion = poseCombate = 0f;
        estabaTirando = false;
        if (lienzo != null) lienzo.SetActive(false);
    }
    private void OnDestroy() { if (lienzo != null) Destroy(lienzo); }
}

