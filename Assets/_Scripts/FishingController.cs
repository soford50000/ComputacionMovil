using UnityEngine;
using UnityEngine.UI;

public class FishingController : MonoBehaviour
{
    [Header("Referencias de Equipamiento Actual")]
    public CanaData canaActual;       // La caña que el jugador tiene equipada (RF7)
    private PezData pezEnganchado;     // El pez que picó (RF2)

    [Header("Estados del Juego")]
    public bool estaPescando = false;
    private bool mordioAnzuelo = false;

    [Header("Configuración de Tensión (RF3 y RF4)")]
    [Range(0f, 100f)] public float tensionActual = 0f;
    public float velocidadSubidaTension = 25f;
    public float velocidadBajadaTension = 30f;
    private bool jugadorEstaPresionando = false;

    [Header("UI de Tensión (Referencias visuales)")]
    public Slider barraTensionSlider; // Asigna aquí el Slider de la UI de Unity
    public GameObject panelMinijuegoTension; // Panel que se activa cuando inicia el minijuego

    void Update()
    {
        if (estaPescando)
        {
            GestionarTension();
        }
    }

    /// <summary>
    /// RF3: El jugador inicia el lanzamiento del anzuelo.
    /// </summary>
    public void BotonLanzarPescar()
    {
        if (!estaPescando)
        {
            // Simulamos que lanzamos y tras un momento corto, pica un pez (RF2)
            Debug.Log("Anzuelo lanzado... esperando a que pique un pez...");

            // Generamos el pez usando el FisheryManager que hicimos antes
            if (FisheryManager.Instance != null)
            {
                pezEnganchado = FisheryManager.Instance.GenerarPezAleatorio();
            }

            // Iniciamos el minijuego de tensión
            IniciarMinijuegoTension();
        }
    }

    void IniciarMinijuegoTension()
    {
        estaPescando = true;
        tensionActual = 20f; // Empieza en zona segura
        if (panelMinijuegoTension != null) panelMinijuegoTension.SetActive(true);
    }

    /// <summary>
    /// Método para conectar con el botón táctil inferior (RF3): Mantener presionado sube tensión, soltarla baja.
    /// </summary>
    public void OnPointerDownTension()
    {
        jugadorEstaPresionando = true;
    }

    public void OnPointerUpTension()
    {
        jugadorEstaPresionando = false;
    }

    /// <summary>
    /// RF4: Lógica de la barra de tensión y resistencia del pez (afectada por la caña RF7)
    /// </summary>
    void GestionarTension()
    {
        // Si el jugador presiona, la tensión sube. Si suelta, baja.
        if (jugadorEstaPresionando)
        {
            tensionActual += velocidadSubidaTension * Time.deltaTime;
        }
        else
        {
            tensionActual -= velocidadBajadaTension * Time.deltaTime;
        }

        // Aplicar la resistencia del pez (modificada por el poder de la caña RF7)
        if (pezEnganchado != null && canaActual != null)
        {
            float dificultadEfectiva = pezEnganchado.fuerzaPez - canaActual.poderCana;
            // La fuerza del pez empuja constantemente la tensión hacia arriba o genera tirones
            tensionActual += (dificultadEfectiva * 2f) * Time.deltaTime;
        }

        // Limitar la tensión entre 0 y 100
        tensionActual = Mathf.Clamp(tensionActual, 0f, 100f);

        // Actualizar la barra visual en la UI
        if (barraTensionSlider != null)
        {
            barraTensionSlider.value = tensionActual / 100f;
        }

        // Comprobar condiciones de victoria o derrota (RF5)
        if (tensionActual >= 100f)
        {
            Debug.Log("¡La línea se rompió! El pez escapó.");
            TerminarPesca(false);
        }
        else if (tensionActual <= 0f)
        {
            // Si baja a 0 por soltar demasiado, el pez también se va
            Debug.Log("El pez se soltó por falta de tensión.");
            TerminarPesca(false);
        }
    }

    void TerminarPesca(bool exito)
    {
        estaPescando = false;
        if (panelMinijuegoTension != null) panelMinijuegoTension.SetActive(false);

        if (exito)
        {
            Debug.Log($"¡Captura exitosa de {pezEnganchado.nombreEspecie}!");
            // Aquí dispararemos el RF6 (Ventana emergente con datos) y RF8/RF9 (Guardar en catálogo)
        }
    }
}