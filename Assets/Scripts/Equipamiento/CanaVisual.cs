using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>Los modelos comparten pose, tensión y animación; solo cambia su construcción visual.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TensionSystem))]
public sealed class CanaVisual : MonoBehaviour
{
    [Header("Caña equipada · primera persona")]
    [SerializeField] private Vector2 basePantalla = new Vector2(0.74f, -0.12f);
    [SerializeField] private Vector2 puntaPantalla = new Vector2(0.50f, 0.81f);
    [SerializeField, Range(0.5f, 1.5f)] private float grosor = 1f;
    [Header("Curvatura procedural y resorte")]
    [FormerlySerializedAs("flexionMaxima")]
    [SerializeField, Range(0.1f, 1.8f), Tooltip("Giro máximo de la punta en radianes con tensión uno.")]
    private float curvaMaxima = 1.35f;
    [SerializeField, Range(0f, 0.6f), Tooltip("Fracción rígida desde la base.")]
    private float inicioFlexion = 0.25f;
    [SerializeField, Range(1.1f, 5f), Tooltip("Potencia de la distribución angular; valores altos concentran la flexión en la punta.")]
    private float potenciaFlexion = 2f;
    [SerializeField, Range(0f, 0.15f), Tooltip("Caída angular en reposo, en radianes.")]
    private float caidaGravedad = 0.045f;
    [SerializeField, Range(4f, 400f), Tooltip("Rigidez k del resorte con masa unitaria; mayor valor responde más rápido.")]
    private float rigidez = 100f;
    [SerializeField, Range(0.2f, 0.99f), Tooltip("Razón de amortiguación: menor valor produce más rebote; cerca de uno casi no oscila.")]
    private float amortiguacion = 0.55f;
    [FormerlySerializedAs("amplitudVibracion")]
    [SerializeField, Range(0f, 0.01f), Tooltip("Amplitud máxima relativa a la altura del lienzo, solo por encima del límite seguro.")]
    private float intensidadVibracion = 0.002f;
    [SerializeField, Range(8f, 24f), Tooltip("Frecuencia de vibración en hercios; usar 10–12 para dispositivos de 30 FPS.")]
    private float frecuenciaVibracion = 12f;
    [SerializeField, Tooltip("Dibuja el eje flexible y marca la punta y el ancla, sin logs por frame.")]
    private bool depurarAnimacion;

    [Header("Animaciones visuales y sedal")]
    [SerializeField, Range(0.3f, 2f), Tooltip("Duración visual del lanzamiento en segundos.")]
    private float duracionLanzamiento = 0.9f;
    [SerializeField, Range(0f, 0.4f), Tooltip("Desplazamiento horizontal relativo durante el latigazo.")]
    private float amplitudLanzamiento = 0.22f;
    [SerializeField, Range(0.08f, 0.3f), Tooltip("Segundos entre picoteos.")]
    private float intervaloPicoteos = 0.16f;
    [SerializeField, Range(0f, 1f), Tooltip("Intensidad de cada picoteo.")]
    private float intensidadPicoteo = 0.22f;
    [SerializeField, Range(0f, 0.01f), Tooltip("Balanceo de espera relativo a la altura.")]
    private float balanceoEspera = 0.0012f;
    [SerializeField, Range(0.1f, 3f), Tooltip("Frecuencia del balanceo en hercios.")]
    private float frecuenciaEspera = 0.4f;
    [SerializeField, Range(0f, 0.1f), Tooltip("Amplitud de recogida al mantener presionado.")]
    private float pulsoRecogida = 0.025f;
    [SerializeField, Range(0.5f, 5f), Tooltip("Pulsos de recogida por segundo.")]
    private float frecuenciaRecogida = 2f;
    [SerializeField, Range(0f, 8f), Tooltip("Impulso de velocidad del resorte de tirones.")]
    private float fuerzaTironVisual = 4f;
    [SerializeField, Range(0.05f, 0.5f), Tooltip("Tiempo de respuesta del resorte de tirones.")]
    private float respuestaTiron = 0.22f;
    [SerializeField, Range(0.2f, 0.95f), Tooltip("Amortiguación de tirones y latigazos.")]
    private float amortiguacionTiron = 0.68f;
    [SerializeField, Range(0f, 6f), Tooltip("Velocidad de descarga al perder el pez.")]
    private float impulsoEscape = 2.5f;
    [SerializeField, Range(0f, 0.25f), Tooltip("Caída máxima del control Bézier con tensión cero, relativa a la altura del lienzo; disminuye hasta cero con tensión uno.")]
    private float caidaSedal = 0.09f;
    [SerializeField, Range(0f, 8f), Tooltip("Oscilación del sedal en unidades UI.")]
    private float oscilacionSedal = 2f;
    [SerializeField, Range(0.3f, 2f), Tooltip("Grosor del sedal flojo en unidades UI.")]
    private float grosorSedalFlojo = 1.1f;
    [SerializeField, Range(0.3f, 2f), Tooltip("Grosor del sedal tenso en unidades UI.")]
    private float grosorSedalTenso = 0.7f;
    [SerializeField, Range(0f, 1f), Tooltip("Opacidad del sedal flojo.")]
    private float brilloSedalFlojo = 0.4f;
    [SerializeField, Range(0f, 1f), Tooltip("Opacidad del sedal tenso.")]
    private float brilloSedalTenso = 0.85f;
    [SerializeField, Range(0f, 100f), Tooltip("Amplitud del latigazo del sedal al perder el pez.")]
    private float latigazoSedal = 45f;
    [SerializeField, Range(1f, 3f), Tooltip("Amplitud de ondas en combate.")]
    private float ondasCombate = 1.7f;
    [SerializeField, Range(2, 3), Tooltip("Picoteos antes del tirón inicial.")]
    private int cantidadPicoteos = 3;
    private float faseVibracion;
    private float relojVisual, tiempoLanzamiento = -1f, tiempoPique = -1f;
    private float latigazo, velocidadLatigazo;
    private float extensionSedal = 1f, inclinacionLanzamiento;
    private int picoteosEmitidos;
    private bool esperando, recogiendo;
    [Header("Ancla del sedal sobre el agua")]
    [SerializeField, Range(0f, 1f), Tooltip("X del ancla como fracción del ancho de pantalla: 0 izquierda, 1 derecha.")]
    private float posicionXAncla = 0.3f;
    [SerializeField, Range(0f, 0.25f), Tooltip("Profundidad bajo el límite del agua, como fracción de la altura visible del fondo.")]
    private float profundidadAncla = 0.08f;
    [SerializeField, Range(0f, 1f), Tooltip("Y del ancla como fracción de la altura de pantalla: 0 abajo, 1 arriba. Se limita bajo el agua con profundidadAncla como margen.")]
    private float posicionYAncla = 0.24f;
    [SerializeField, Tooltip("Dibuja una marca cian en el ancla y una línea amarilla en el límite del agua convertido a pantalla.")]
    private bool depurarAnclaSedal;
    [SerializeField, Tooltip("Activa un objetivo manual en coordenadas locales de CanaGrafico. La API nullable controla también esta opción.")]
    private bool usarObjetivoSedal;
    [SerializeField, Range(-4096f, 4096f), Tooltip("Coordenada X local del objetivo manual.")]
    private float objetivoSedalX;
    [SerializeField, Range(-4096f, 4096f), Tooltip("Coordenada Y local del objetivo manual.")]
    private float objetivoSedalY;
    private FondoAguaUI fondoAgua;

    // Unity no serializa Nullable<Vector2>: el Inspector usa el interruptor y las dos coordenadas.
    public Vector2? puntoObjetivoSedal
    {
        get
        {
            if (!usarObjetivoSedal) return null;
            Vector2 punto = Vector2.zero;
            punto.x = objetivoSedalX;
            punto.y = objetivoSedalY;
            return punto;
        }
        set
        {
            usarObjetivoSedal = value.HasValue && EsFinito(value.Value.x) && EsFinito(value.Value.y);
            if (!usarObjetivoSedal) return;
            objetivoSedalX = value.Value.x;
            objetivoSedalY = value.Value.y;
        }
    }

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
        fondoAgua = GetComponent<FondoAguaUI>();
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

    /// <summary>Objetivo local del RectTransform de CanaGrafico; null restaura EntradaAgua.</summary>
    public void EstablecerObjetivoSedal(Vector2? punto)
    {
        if (punto.HasValue && (!EsFinito(punto.Value.x) || !EsFinito(punto.Value.y))) punto = null;
        puntoObjetivoSedal = punto;
    }

    private static bool EsFinito(float valor) => !float.IsNaN(valor) && !float.IsInfinity(valor);

    public void AplicarTiron(float intensidad)
    {
        if (!isActiveAndEnabled || !EsFinito(intensidad)) return;
        // Impulso de velocidad: la posición sigue continua y el mismo resorte recupera la pose.
        velocidad = Mathf.Clamp(velocidad + Mathf.Clamp01(intensidad) * fuerzaTironVisual,
            -fuerzaTironVisual * 2f, fuerzaTironVisual * 2f);
    }

    public void EfectoTiron(float intensidad) => AplicarTiron(intensidad);

    /// <summary>Solo presentación; no modifica la captura ni la tensión de juego.</summary>
    public void FinalizarCaptura(bool exito, bool rotura = false)
    {
        if (!isActiveAndEnabled) return;
        esperando = recogiendo = false;
        tiempoPique = tiempoLanzamiento = -1f;
        velocidad = exito ? velocidad * 0.25f : -impulsoEscape;
        if (!exito) velocidadLatigazo = -impulsoEscape * (rotura ? 1.4f : 1f);
    }

    private void AlLanzar()
    {
        esperando = true;
        tiempoLanzamiento = 0f;
        tiempoPique = -1f;
        extensionSedal = 0f;
        latigazo = velocidadLatigazo = 0f;
    }

    private void AlPique()
    {
        esperando = false;
        tiempoLanzamiento = -1f;
        tiempoPique = 0f;
        picoteosEmitidos = 0;
        extensionSedal = 1f;
    }

    private void AlPresionar(bool presionado) { recogiendo = presionado; }

    private void LateUpdate()
    {
        float dt = Mathf.Max(0f, Time.deltaTime);
        relojVisual += dt;
        bool tirando = pesca != null && pesca.isActiveAndEnabled && pesca.EnForcejeo;
        if (esperando && (pesca == null || !pesca.isActiveAndEnabled || pesca.PuedeCambiarCana))
        { esperando = false; tiempoLanzamiento = -1f; }
        if (tirando && !estabaTirando) tiempoTiron = 0f;
        estabaTirando = tirando;
        tiempoTiron += dt;
        inclinacionLanzamiento = 0f;
        if (tiempoLanzamiento >= 0f)
        {
            tiempoLanzamiento += dt;
            float fase = Mathf.Clamp01(tiempoLanzamiento / Mathf.Max(0.3f, duracionLanzamiento));
            if (fase < 0.3f) inclinacionLanzamiento = -amplitudLanzamiento * Mathf.SmoothStep(0f, 1f, fase / 0.3f);
            else if (fase < 0.55f) inclinacionLanzamiento = Mathf.Lerp(-amplitudLanzamiento, amplitudLanzamiento, Mathf.SmoothStep(0f, 1f, (fase - 0.3f) / 0.25f));
            else inclinacionLanzamiento = amplitudLanzamiento * Mathf.Cos((fase - 0.55f) / 0.45f * Mathf.PI * 2f) * Mathf.Pow((1f - fase) / 0.45f, 2f);
            extensionSedal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fase - 0.3f) / 0.5f));
            if (fase >= 1f) tiempoLanzamiento = -1f;
        }
        else extensionSedal = 1f;
        if (tiempoPique >= 0f)
        {
            tiempoPique += dt;
            while (picoteosEmitidos <= cantidadPicoteos && tiempoPique >= picoteosEmitidos * Mathf.Max(0.08f, intervaloPicoteos))
            {
                EfectoTiron(picoteosEmitidos == cantidadPicoteos ? 1f : intensidadPicoteo);
                picoteosEmitidos++;
            }
            if (picoteosEmitidos > cantidadPicoteos) tiempoPique = -1f;
        }
        float objetivo = tirando ? (usarTensionExterna ? tensionExterna : Mathf.Clamp01(0.76f + Mathf.Sin(tiempoTiron * 4.6f) * 0.12f)) : 0f;
        if (tirando && recogiendo) objetivo += pulsoRecogida * Mathf.Sin(relojVisual * frecuenciaRecogida * Mathf.PI * 2f);
        IntegrarResorte(ref tension, ref velocidad, Mathf.Clamp01(objetivo),
            2f / Mathf.Sqrt(Mathf.Clamp(rigidez, 4f, 400f)), amortiguacion, dt);
        IntegrarResorte(ref latigazo, ref velocidadLatigazo, 0f, respuestaTiron, amortiguacionTiron, dt);
        // Conserva la inercia y el pequeño rebote negativo al soltar; solo limita la geometría extrema.
        poseCombate = Mathf.Clamp(tension, -0.25f, 1.5f);
        float cargaReal = usarTensionExterna ? tensionExterna : Mathf.Clamp01(objetivo);
        float roja = Mathf.InverseLerp(TensionSystem.LimiteSuperiorZonaSegura, 1f, cargaReal);
        faseVibracion = Mathf.Repeat(faseVibracion + dt * frecuenciaVibracion * Mathf.PI * 2f, Mathf.PI * 2f);
        vibracion = tirando ? intensidadVibracion * roja * Mathf.Sin(faseVibracion) :
            esperando ? balanceoEspera * Mathf.Sin(relojVisual * frecuenciaEspera * Mathf.PI * 2f) : 0f;
        ActualizarModelo();
        ActualizarBarraTension(tirando);
    }

    // Solución analítica del resorte amortiguado, sin integración Euler.
    private static void IntegrarResorte(ref float posicion, ref float velocidadActual, float objetivo,
        float respuesta, float amortiguamiento, float dt)
    {
        float omega = 2f / Mathf.Max(0.05f, respuesta);
        float zeta = Mathf.Clamp(amortiguamiento, 0.2f, 0.99f);
        float wd = omega * Mathf.Sqrt(1f - zeta * zeta);
        float error = posicion - objetivo;
        float auxiliar = (velocidadActual + zeta * omega * error) / wd;
        float seno = Mathf.Sin(wd * dt), coseno = Mathf.Cos(wd * dt);
        float decaimiento = Mathf.Exp(-zeta * omega * dt);
        posicion = objetivo + decaimiento * (error * coseno + auxiliar * seno);
        velocidadActual = decaimiento * (velocidadActual * coseno - (zeta * omega * auxiliar + wd * error) * seno);
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
        // Una caña recta puede alcanzar el techo: conserva el HUD dentro del lienzo.
        float ajuste = escala;
        baseBarra = Mathf.Min(baseBarra, techo - 132f * ajuste);
        visible &= escala > 0f;
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
        // Rota el eje en unidades UI conservando longitud incluso en otras relaciones de aspecto.
        Vector2 tamano = grafico.rectTransform.rect.size;
        Vector2 eje = Vector2.Scale(puntaPantalla - basePantalla, tamano);
        float seno = Mathf.Sin(inclinacionLanzamiento), coseno = Mathf.Cos(inclinacionLanzamiento);
        Vector2 puntaAnimada = basePantalla;
        puntaAnimada.x += (eje.x * coseno - eje.y * seno) / Mathf.Max(1f, tamano.x);
        puntaAnimada.y += (eje.x * seno + eje.y * coseno) / Mathf.Max(1f, tamano.y);
        float poder = equipada != null ? Mathf.Clamp(equipada.poderCana, 1f, 10f) : 1f;
        float resistenciaVisual = Mathf.Lerp(1f, 0.72f, Mathf.InverseLerp(1f, 10f, poder));
        grafico.ConfigurarFlexion(inicioFlexion, potenciaFlexion, caidaGravedad, depurarAnimacion);
        grafico.Configurar(basePantalla, puntaAnimada, grosor, curvaMaxima * resistenciaVisual, poseCombate, vibracion, estabaTirando ? relojVisual : -1f);
        float cargaSedal = Mathf.Clamp01(tension);
        grafico.ConfigurarAnclaAgua(fondoAgua, posicionXAncla, profundidadAncla, posicionYAncla, depurarAnclaSedal);
        grafico.ConfigurarSedal(puntoObjetivoSedal, extensionSedal,
            caidaSedal * (1f - cargaSedal) * (1f - cargaSedal),
            Mathf.Lerp(grosorSedalFlojo, grosorSedalTenso, cargaSedal),
            Mathf.Lerp(brilloSedalFlojo, brilloSedalTenso, cargaSedal),
            (esperando || estabaTirando ? Mathf.Sin(relojVisual * frecuenciaEspera * Mathf.PI * 2f) * oscilacionSedal * (1f - cargaSedal) : 0f)
                + latigazo * latigazoSedal,
            Mathf.Lerp(1f, ondasCombate, cargaSedal));
    }

    private void OnEnable()
    {
        if (pesca == null) pesca = GetComponent<TensionSystem>();
        pesca.LanzamientoIniciado += AlLanzar;
        pesca.PiqueDetectado += AlPique;
        pesca.PresionCambiada += AlPresionar;
        recogiendo = pesca.EstaPresionado;
        if (lienzo != null) lienzo.SetActive(true);
    }
    private void OnDisable()
    {
        if (pesca != null)
        {
            pesca.LanzamientoIniciado -= AlLanzar;
            pesca.PiqueDetectado -= AlPique;
            pesca.PresionCambiada -= AlPresionar;
        }
        esperando = recogiendo = false;
        tiempoLanzamiento = tiempoPique = -1f;
        latigazo = velocidadLatigazo = relojVisual = faseVibracion = 0f;
        extensionSedal = 1f;
        tension = velocidad = tiempoTiron = vibracion = poseCombate = 0f;
        estabaTirando = false;
        if (lienzo != null) lienzo.SetActive(false);
    }
    private void OnDestroy() { if (lienzo != null) Destroy(lienzo); }
}

