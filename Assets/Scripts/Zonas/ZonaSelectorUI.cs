using UnityEngine;
using UnityEngine.UI;

/// <summary>Mapa de viaje entre zonas; comparte el Canvas del saldo.</summary>
public sealed class ZonaSelectorUI : MonoBehaviour
{
    private FisheryManager gestor;
    private TensionSystem pesca;
    private Button acceso;
    private GameObject ventana;
    private RectTransform panel;
    private Material materialIcono;
    private Button[] destinos;
    private Text[] estados;
    public bool EstaAbierto => ventana != null && ventana.activeSelf;

    public void Preparar(Transform lienzo)
    {
        gestor = GetComponent<FisheryManager>();
        pesca = GetComponent<TensionSystem>();
        var fondo = EstiloExpedicion.Panel(lienzo, "Abrir mapa", EstiloExpedicion.Madera, 1, 1, 1, 1);
        fondo.rectTransform.pivot = Vector2.one;
        fondo.rectTransform.anchoredPosition = new Vector2(-18, -18);
        fondo.rectTransform.sizeDelta = new Vector2(60, 58);
        acceso = fondo.gameObject.AddComponent<Button>();
        acceso.targetGraphic = fondo;
        acceso.onClick.AddListener(Abrir);
        EstiloExpedicion.Boton(acceso);
        var icono = EstiloExpedicion.Rect(fondo.transform, "Mapa", .12f, .12f, .88f, .88f)
            .gameObject.AddComponent<RawImage>();
        icono.texture = Resources.Load<Texture2D>("Interfaz/Mapa");
        // Recorta únicamente el margen transparente del archivo proporcionado.
        icono.uvRect = new Rect(.15f, .18f, .70f, .66f);
        materialIcono = new Material(Resources.Load<Shader>("DecoracionCatalogo/SiluetaNogal"));
        icono.material = materialIcono;
        TemaZonaUI.Colorear(icono, EstiloExpedicion.Dorado);
        icono.raycastTarget = false;

        ventana = EstiloExpedicion.Panel(lienzo, "Mapa de zonas", EstiloExpedicion.Velo, 0, 0, 1, 1).gameObject;
        panel = EstiloExpedicion.Panel(ventana.transform, "Destinos", EstiloExpedicion.Papel, .04f, .05f, .96f, .95f).rectTransform;
        EstiloExpedicion.Cabecera(panel, "Elige tu destino", "Dos paisajes para explorar. Tu equipo y tu saldo viajan contigo.", Cerrar);
        panel.gameObject.AddComponent<FondoNogalCatalogo>().MostrarVetas = false;
        panel.gameObject.AddComponent<VegetacionCatalogo>().Preparar(panel);
        TemaZonaUI.Colorear(panel.Find("Título").GetComponent<Text>(), EstiloExpedicion.Madera);
        TemaZonaUI.Colorear(panel.Find("Ayuda").GetComponent<Text>(), EstiloExpedicion.Madera);
        TemaZonaUI.Colorear(panel.Find("Marca de expedición").GetComponent<Text>(), EstiloExpedicion.Madera);
        panel.Find("Línea de cabecera").gameObject.SetActive(false);
        EstiloExpedicion.PrepararModal(ventana, panel);
        ventana.SetActive(false);
    }

    private void LateUpdate()
    {
        if (acceso == null) return;
        bool disponible = gestor != null && pesca != null && pesca.PuedeCambiarCana;
        if (acceso.gameObject.activeSelf != disponible) acceso.gameObject.SetActive(disponible);
        if (!disponible && EstaAbierto) Cerrar();
    }

    public void Abrir()
    {
        if (gestor == null || pesca == null || !pesca.PuedeCambiarCana) return;
        if (destinos == null)
        {
            destinos = new Button[2];
            estados = new Text[2];
            var disposicion = panel.gameObject.AddComponent<DisposicionExpedicion>();
            CrearDestino(0, gestor.ZonaInicial, "Selva amazónica", disposicion);
            CrearDestino(1, gestor.ZonaSanAndres, "Caribe Insular · 15 especies", disposicion);
        }
        for (int i = 0; i < destinos.Length; i++)
        {
            var zona = i == 0 ? gestor.ZonaInicial : gestor.ZonaSanAndres;
            bool actual = zona != null && zona == gestor.ZonaActual;
            destinos[i].interactable = zona != null && !actual;
            estados[i].text = actual ? "Aquí estás" : zona != null ? "Viajar →" : "Destino no disponible";
        }
        ventana.SetActive(true);
    }

    private void CrearDestino(int indice, ZonaData zona, string descripcion, DisposicionExpedicion disposicion)
    {
        float x = indice == 0 ? .04f : .52f;
        var tarjeta = EstiloExpedicion.Panel(panel, "Destino " + (indice == 0 ? "Amazonas" : "San Andrés"),
            EstiloExpedicion.Papel, x, .07f, x + .44f, .77f);
        var boton = tarjeta.gameObject.AddComponent<Button>();
        boton.targetGraphic = tarjeta;
        boton.onClick.AddListener(() => Viajar(zona));
        EstiloExpedicion.Boton(boton);
        var colores = TemaZonaUI.EstadosOriginales(boton);
        colores.disabledColor = Color.white;
        TemaZonaUI.Estados(boton, colores);
        destinos[indice] = boton;
        float y = indice == 0 ? .44f : .07f;
        disposicion.Agregar(tarjeta.rectTransform, .04f, y, .96f, y + .33f);
        var foto = EstiloExpedicion.Panel(tarjeta.transform, "Paisaje", Color.white, .025f, .32f, .975f, .975f);
        foto.sprite = zona != null ? zona.fondoZona : null;
        foto.preserveAspect = true;
        if (foto.sprite == null) TemaZonaUI.Colorear(foto, Color.clear);
        EstiloExpedicion.Texto(tarjeta.transform, "Nombre", zona != null ? zona.nombreZona : "Próximamente", 26,
            EstiloExpedicion.Tinta, .05f, .22f, .95f, .32f);
        EstiloExpedicion.Texto(tarjeta.transform, "Región", descripcion, 15,
            EstiloExpedicion.Tinta, .05f, .13f, .95f, .22f);
        estados[indice] = EstiloExpedicion.Texto(tarjeta.transform, "Estado", "", 18,
            EstiloExpedicion.Verde, .05f, .02f, .95f, .12f);
    }

    private void Viajar(ZonaData zona)
    {
        if (gestor.CambiarZona(zona)) Cerrar();
    }

    public void Cerrar() { if (ventana != null) ventana.SetActive(false); }

    private void OnDestroy()
    {
        if (materialIcono != null) Destroy(materialIcono);
    }
}
