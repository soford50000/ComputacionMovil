using UnityEngine;
using System.Collections.Generic;

public class FisheryManager : MonoBehaviour
{
    public static FisheryManager Instance; // Patrón Singleton para acceder fácilmente desde otros scripts

    [Header("Zona inicial (RF1)")]
    [SerializeField] private ZonaData zonaAmazonas;
    private ZonaData zonaActual;
    private TensionSystem pesca;
    public ZonaData ZonaInicial => zonaAmazonas;
    public ZonaData ZonaActual => zonaActual != null ? zonaActual : zonaAmazonas;
    public ZonaData ZonaSanAndres { get; private set; }
    public event System.Action<ZonaData> ZonaCambiada;
    public bool TieneEspecies => ZonaActual != null && ZonaActual.especiesDeEstaZona != null &&
        System.Array.Exists(ZonaActual.especiesDeEstaZona, pez => pez != null && !pez.caracteristicasPendientes);

    [Header("Base de datos de Peces (compatibilidad)")]
    [Tooltip("Arrastra aquí todos los peces (ScriptableObjects) que creaste en el proyecto.")]
    public List<PezData> todosLosPeces = new List<PezData>();

    [Header("Configuración de Rarezas (Pesos base para RF2)")]
    // Esto define qué tan probable es que salga una rareza en general si hay varios peces
    [Tooltip("Probabilidad base para peces Comunes")] public float pesoComun = 50f;
    [Tooltip("Probabilidad base para peces Poco Comunes")] public float pesoPocoComun = 25f;
    [Tooltip("Probabilidad base para peces Raros")] public float pesoRaro = 15f;
    [Tooltip("Probabilidad base para peces Épicos")] public float pesoEpico = 8f;
    [Tooltip("Probabilidad base para peces Legendarios")] public float pesoLegendario = 2f;

    private void Awake()
    {
        // Configuración básica de Singleton para que este manager no se destruya entre escenas
        if (Instance == null)
        {
            Instance = this;
            pesca = GetComponent<TensionSystem>();
            ZonaSanAndres = Resources.Load<ZonaData>("Zonas/SanAndres");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CambiarZona(ZonaData zona)
    {
        if (zona == null || (zona != ZonaInicial && zona != ZonaSanAndres) ||
            (pesca != null && !pesca.PuedeCambiarCana)) return false;
        if (zona == ZonaActual) return true;
        zonaActual = zona;
        ZonaCambiada?.Invoke(zona);
        return true;
    }

    /// <summary>
    /// RF2 y RF11: Selecciona un pez aleatoriamente según su rareza y modificadores de cebo.
    /// </summary>
    /// <param name="pezEspecialCebo">Opcional: un pez específico al que el cebo actual le da bonificación (RF11)</param>
    /// <param name="multiplicadorCebo">Multiplicador del peso de sorteo del pez; el porcentaje final se normaliza.</param>
    /// <param name="cebo">Carnada para múltiples especies. Tiene prioridad sobre los parámetros anteriores.</param>
    public PezData GenerarPezAleatorio(PezData pezEspecialCebo = null, float multiplicadorCebo = 1f, CeboData cebo = null)
    {
        if (!TieneEspecies)
        {
            return null;
        }

        // 1. Calcular el "peso" total de probabilidad considerando el cebo (RF11)
        Dictionary<PezData, float> probabilidadesPonderadas = new Dictionary<PezData, float>();
        float pesoTotal = 0f;

        foreach (var pez in ZonaActual.especiesDeEstaZona)
        {
            if (pez == null || pez.caracteristicasPendientes || probabilidadesPonderadas.ContainsKey(pez)) continue;
            float pesoRarezaBase = Mathf.Max(0f, ObtenerPesoBasePorRareza(pez.rareza));

            // Aplicar la lógica del cebo (RF11): si coincide con el cebo, aumentamos su probabilidad
            if (cebo != null)
            {
                pesoRarezaBase *= cebo.MultiplicadorPara(pez);
            }
            else if (pez == pezEspecialCebo)
            {
                pesoRarezaBase *= Mathf.Max(1f, multiplicadorCebo);
            }

            if (float.IsNaN(pesoRarezaBase) || float.IsInfinity(pesoRarezaBase) || pesoRarezaBase <= 0f) continue;
            probabilidadesPonderadas.Add(pez, pesoRarezaBase);
            pesoTotal += pesoRarezaBase;
        }

        if (pesoTotal <= 0f || float.IsInfinity(pesoTotal))
        {
            Debug.LogError("La zona actual no tiene especies con probabilidades válidas.", this);
            return null;
        }

        // 2. Lanzar el "dado" aleatorio entre 0 y el peso total
        float valorAleatorio = Random.Range(0f, pesoTotal);
        float acumulador = 0f;

        // 3. Seleccionar el pez en base a dónde cae el valor aleatorio
        PezData ultimoPez = null;
        foreach (var par in probabilidadesPonderadas)
        {
            ultimoPez = par.Key;
            acumulador += par.Value;
            if (valorAleatorio <= acumulador)
            {
                Debug.Log($"¡Pez pescado! Seleccionado: {par.Key.nombreEspecie} (Rareza: {par.Key.rareza})");
                return par.Key;
            }
        }

        // Por seguridad, si algo falla, devolvemos el primer pez de la lista
        return ultimoPez;
    }

    [ContextMenu("Probar selección en Amazonas")]
    private void ProbarSeleccionEnAmazonas()
    {
        GenerarPezAleatorio();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private float ObtenerPesoBasePorRareza(Rareza rareza)
    {
        switch (rareza)
        {
            case Rareza.Comun: return pesoComun;
            case Rareza.PocoComun: return pesoPocoComun;
            case Rareza.Rara: return pesoRaro;
            case Rareza.Epica: return pesoEpico;
            case Rareza.Legendaria: return pesoLegendario;
            default: return 10f;
        }
    }
}
