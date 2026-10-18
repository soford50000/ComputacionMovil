using UnityEngine;
using UnityEngine.UI;

/// <summary>Fondo fotográfico con ondulaciones suaves, detrás del HUD de pesca.</summary>
public sealed class FondoAguaUI : MonoBehaviour
{
    [SerializeField] private Texture2D fotografia;
    [SerializeField] private Shader shaderAgua;
    [Header("Agua del Amazonas")]
    [Tooltip("Amplitud en píxeles de pantalla, independiente de la resolución de la foto.")]
    [SerializeField, Range(0f, 5f)] private float intensidad = 3.51f;
    [SerializeField, Range(0f, 2f)] private float velocidad = 1.03f;
    [Tooltip("Altura desde abajo de la fotografía. Mantener por debajo de las orillas.")]
    [SerializeField, Range(0.05f, 0.5f)] private float limiteAgua = 0.433f;

    [Header("San Andrés · configuración del agua")]
    [Tooltip("Intensidad de respaldo si se usa AguaTranquila en San Andrés.")]
    [SerializeField, Range(0f, 5f)] private float intensidadSanAndres = 3.94f;
    [Tooltip("Velocidad de respaldo si se usa AguaTranquila en San Andrés.")]
    [SerializeField, Range(0f, 2f)] private float velocidadSanAndres = 1.48f;
    [SerializeField, Range(0.05f, 0.5f)] private float limiteAguaMar = 0.478f;

    [Header("Mar de San Andrés")]
    [SerializeField, Range(0f, 8f)] private float intensidadMar = 2.6f;
    [SerializeField, Range(0f, 2f)] private float velocidadMar = 1.52f;
    [SerializeField, Range(0f, 0.3f)] private float reflejosMar = 0.188f;
    private Shader shaderMar;
    private bool esMar;
    private bool UsaShaderMar => esMar && shaderMar != null;
    private static readonly int IntensidadId = Shader.PropertyToID("_Intensity");
    private static readonly int TiempoId = Shader.PropertyToID("_RippleTime");
    private static readonly int LimiteId = Shader.PropertyToID("_Waterline");
    private static readonly int PixelId = Shader.PropertyToID("_RippleUvPerPixel");
    private static readonly int ReflejosId = Shader.PropertyToID("_GlintStrength");

    private GameObject fondo;
    private RawImage imagen;
    private Material material;
    private float tiempoOndas;
    private FisheryManager gestorZonas;
    private Texture2D fotografiaInicial;

    private void Awake()
    {
        fotografiaInicial = fotografia;
        if (fotografia == null || shaderAgua == null)
        {
            Debug.LogError("Falta la fotografía o el shader del fondo de agua.", this);
            enabled = false;
            return;
        }

        fondo = new GameObject("Fondo Amazonas", typeof(RectTransform), typeof(Canvas));
        fondo.transform.SetParent(transform, false);
        var canvas = fondo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        var objetoImagen = new GameObject("Agua animada", typeof(RectTransform), typeof(RawImage));
        objetoImagen.transform.SetParent(fondo.transform, false);
        imagen = objetoImagen.GetComponent<RawImage>();
        imagen.rectTransform.anchorMin = Vector2.zero;
        imagen.rectTransform.anchorMax = Vector2.one;
        imagen.rectTransform.offsetMin = imagen.rectTransform.offsetMax = Vector2.zero;
        imagen.texture = fotografia;
        imagen.raycastTarget = false;
        material = new Material(shaderAgua);
        material.mainTexture = fotografia;
        imagen.material = material;
        ActualizarFondo();
    }

    private void Start()
    {
        shaderMar = Resources.Load<Shader>("Agua/MarCaribe");
        gestorZonas = GetComponent<FisheryManager>();
        if (gestorZonas == null) return;
        gestorZonas.ZonaCambiada += CambiarZona;
        CambiarZona(gestorZonas.ZonaActual);
    }

    private void CambiarZona(ZonaData zona)
    {
        if (imagen == null || material == null || zona == null) return;
        fotografia = zona.fondoZona != null ? zona.fondoZona.texture : fotografiaInicial;
        esMar = zona == gestorZonas.ZonaSanAndres;
        material.shader = UsaShaderMar ? shaderMar : shaderAgua;
        imagen.texture = fotografia;
        material.mainTexture = fotografia;
        fondo.name = "Fondo " + zona.nombreZona;
        tiempoOndas = 0;
        ActualizarFondo();
    }

    private void Update()
    {
        tiempoOndas += Time.deltaTime * (UsaShaderMar ? velocidadMar : esMar ? velocidadSanAndres : velocidad);
        ActualizarFondo();
    }

    private void ActualizarFondo()
    {
        if (material == null) return;
        material.SetFloat(IntensidadId, UsaShaderMar ? intensidadMar : esMar ? intensidadSanAndres : intensidad);
        material.SetFloat(TiempoId, tiempoOndas);
        material.SetFloat(LimiteId, esMar ? limiteAguaMar : limiteAgua);
        if (UsaShaderMar) material.SetFloat(ReflejosId, reflejosMar);

        // Cubrir la pantalla sin estirar la foto; la máscara sigue sus coordenadas.
        float pantalla = Screen.width / (float)Mathf.Max(1, Screen.height);
        float aspectoFoto = fotografia.width / (float)fotografia.height;
        float ancho = Mathf.Min(1f, pantalla / aspectoFoto);
        float alto = Mathf.Min(1f, aspectoFoto / pantalla);
        imagen.uvRect = new Rect((1f - ancho) * 0.5f, (1f - alto) * 0.5f, ancho, alto);
        material.SetVector(PixelId, new Vector4(
            ancho / Mathf.Max(1, Screen.width), alto / Mathf.Max(1, Screen.height), 0f, 0f));
    }

    private void OnEnable() { if (fondo != null) fondo.SetActive(true); }
    private void OnDisable() { if (fondo != null) fondo.SetActive(false); }
    private void OnDestroy()
    {
        if (gestorZonas != null) gestorZonas.ZonaCambiada -= CambiarZona;
        if (material != null) Destroy(material);
        if (fondo != null) Destroy(fondo);
    }
}
