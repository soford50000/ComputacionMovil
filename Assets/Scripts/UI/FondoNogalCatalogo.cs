using UnityEngine;

/// <summary>Acabado del Image existente: gradiente de 135°, veta opcional y sombra interior.</summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public sealed class FondoNogalCatalogo : MonoBehaviour
{
    private const int Resolucion = 384;
    private static readonly Color Claro = EstiloExpedicion.Hex("CDAC7B");
    private static readonly Color Oscuro = EstiloExpedicion.Hex("BE9A68");
    private static readonly Color Veta = EstiloExpedicion.Hex("5A4326");
    private static readonly Color Sombra = EstiloExpedicion.Hex("2A1D10");
    private static readonly Color MarClaro = EstiloExpedicion.Hex("D6F0F7");
    private static readonly Color MarOscuro = EstiloExpedicion.Hex("B5DCEB");
    private static readonly Color MarSombra = EstiloExpedicion.Hex("4F8EA6");
    private UnityEngine.UI.Image imagen;
    private Texture2D textura;
    private Sprite superficie;
    private Color32[] pixeles;
    private Vector2 ultimoTamano;
    private bool mostrarVetas = true;
    private bool marino;

    public bool MostrarVetas
    {
        get => mostrarVetas;
        set
        {
            if (mostrarVetas == value) return;
            mostrarVetas = value;
            ultimoTamano = Vector2.zero;
            if (isActiveAndEnabled) Actualizar();
        }
    }

    private void OnEnable() { ActualizarTema(); }
    private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) Actualizar(); }

    public void ActualizarTema()
    {
        bool nuevo = TemaZonaUI.EsMarino;
        if (marino != nuevo) { marino = nuevo; ultimoTamano = Vector2.zero; }
        Actualizar();
    }

    private void Actualizar()
    {
        if (imagen == null) imagen = GetComponent<UnityEngine.UI.Image>();
        if (imagen == null) return;
        Vector2 tamano = imagen.rectTransform.rect.size;
        if (tamano.x <= 0 || tamano.y <= 0 || tamano == ultimoTamano) return;
        ultimoTamano = tamano;
        if (textura == null)
        {
            textura = new Texture2D(Resolucion, Resolucion, TextureFormat.RGBA32, false)
            {
                name = "Nogal del catálogo", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            pixeles = new Color32[Resolucion * Resolucion];
            superficie = Sprite.Create(textura, new Rect(0, 0, Resolucion, Resolucion), new Vector2(.5f, .5f));
            imagen.sprite = superficie;
            // La textura contiene la paleta completa; no multiplicarla por el color de papel.
            TemaZonaUI.Excluir(imagen);
            imagen.color = Color.white;
        }

        for (int y = 0; y < Resolucion; y++)
        for (int x = 0; x < Resolucion; x++)
        {
            float u = x / (float)(Resolucion - 1), v = y / (float)(Resolucion - 1);
            // Proyección diagonal en unidades del panel: conserva 135° en ambas orientaciones.
            float avance = (u * tamano.x + (1 - v) * tamano.y) / (tamano.x + tamano.y);
            Color color = Color.Lerp(marino ? MarClaro : Claro, marino ? MarOscuro : Oscuro, avance);
            if (marino)
            {
                // Ondas suaves en unidades del panel, sin animación ni interferencia con el texto.
                float onda = Mathf.Sin(v * tamano.y / 24f + Mathf.Sin(u * tamano.x / 110f) * 1.2f);
                color = Color.Lerp(color, Color.white, .09f * Mathf.Pow(Mathf.Max(0, onda), 10));
            }
            else if (mostrarVetas)
            {
                float ondulacion = Mathf.Sin(u * 7 + v * 3) * .8f + Mathf.PerlinNoise(u * 4, v * 2) * 2;
                float veta = Mathf.Pow(Mathf.Abs(Mathf.Sin(v * 170 + ondulacion)), 18);
                color = Color.Lerp(color, Veta, .1f * veta);
            }
            float distanciaBorde = Mathf.Min(Mathf.Min(u, 1 - u) * tamano.x, Mathf.Min(v, 1 - v) * tamano.y);
            color = Color.Lerp(color, marino ? MarSombra : Sombra, .06f * Mathf.Exp(-distanciaBorde / 14));
            pixeles[y * Resolucion + x] = color;
        }
        textura.SetPixels32(pixeles);
        textura.Apply(false);
    }

    private void OnDestroy()
    {
        if (superficie != null) Destroy(superficie);
        if (textura != null) Destroy(textura);
    }
}
