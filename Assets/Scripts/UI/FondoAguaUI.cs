using UnityEngine;
using UnityEngine.UI;

/// <summary>Fondo fotográfico con ondulaciones suaves, detrás del HUD de pesca.</summary>
public sealed class FondoAguaUI : MonoBehaviour
{
    [SerializeField] private Texture2D fotografia;
    [SerializeField] private Shader shaderAgua;
    [Tooltip("Amplitud en píxeles de pantalla, independiente de la resolución de la foto.")]
    [SerializeField, Range(0f, 5f)] private float intensidad = 2.5f;
    [SerializeField, Range(0f, 2f)] private float velocidad = 0.6f;
    [Tooltip("Altura desde abajo de la fotografía. Mantener por debajo de las orillas.")]
    [SerializeField, Range(0.05f, 0.5f)] private float limiteAgua = 0.34f;

    private GameObject fondo;
    private RawImage imagen;
    private Material material;
    private float tiempoOndas;

    private void Awake()
    {
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

    private void Update()
    {
        tiempoOndas += Time.deltaTime * velocidad;
        ActualizarFondo();
    }

    private void ActualizarFondo()
    {
        if (material == null) return;
        material.SetFloat("_Intensity", intensidad);
        material.SetFloat("_RippleTime", tiempoOndas);
        material.SetFloat("_Waterline", limiteAgua);

        // Cubrir la pantalla sin estirar la foto; la máscara sigue sus coordenadas.
        float pantalla = Screen.width / (float)Mathf.Max(1, Screen.height);
        float aspectoFoto = fotografia.width / (float)fotografia.height;
        float ancho = Mathf.Min(1f, pantalla / aspectoFoto);
        float alto = Mathf.Min(1f, aspectoFoto / pantalla);
        imagen.uvRect = new Rect((1f - ancho) * 0.5f, (1f - alto) * 0.5f, ancho, alto);
        material.SetVector("_RippleUvPerPixel", new Vector4(
            ancho / Mathf.Max(1, Screen.width), alto / Mathf.Max(1, Screen.height), 0f, 0f));
    }

    private void OnEnable() { if (fondo != null) fondo.SetActive(true); }
    private void OnDisable() { if (fondo != null) fondo.SetActive(false); }
    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (fondo != null) Destroy(fondo);
    }
}
