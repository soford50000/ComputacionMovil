using UnityEngine;
using System.Collections.Generic;

public class FisheryManager : MonoBehaviour
{
    public static FisheryManager Instance; // Patrón Singleton para acceder fácilmente desde otros scripts

    [Header("Base de datos de Peces")]
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// RF2 y RF11: Selecciona un pez aleatoriamente según su rareza y modificadores de cebo.
    /// </summary>
    /// <param name="pezEspecialCebo">Opcional: un pez específico al que el cebo actual le da bonificación (RF11)</param>
    /// <param name="multiplicadorCebo">Multiplicador de probabilidad para el pez del cebo (ej. 3.0 para triplicar su chance)</param>
    public PezData GenerarPezAleatorio(PezData pezEspecialCebo = null, float multiplicadorCebo = 1f)
    {
        if (todosLosPeces.Count == 0)
        {
            Debug.LogError("¡No hay peces cargados en el FisheryManager!");
            return null;
        }

        // 1. Calcular el "peso" total de probabilidad considerando el cebo (RF11)
        Dictionary<PezData, float> probabilidadesPonderadas = new Dictionary<PezData, float>();
        float pesoTotal = 0f;

        foreach (var pez in todosLosPeces)
        {
            float pesoRarezaBase = ObtenerPesoBasePorRareza(pez.rareza);

            // Aplicar la lógica del cebo (RF11): si coincide con el cebo, aumentamos su probabilidad
            if (pez == pezEspecialCebo)
            {
                pesoRarezaBase *= multiplicadorCebo;
            }

            probabilidadesPonderadas.Add(pez, pesoRarezaBase);
            pesoTotal += pesoRarezaBase;
        }

        // 2. Lanzar el "dado" aleatorio entre 0 y el peso total
        float valorAleatorio = Random.Range(0f, pesoTotal);
        float acumulador = 0f;

        // 3. Seleccionar el pez en base a dónde cae el valor aleatorio
        foreach (var par in probabilidadesPonderadas)
        {
            acumulador += par.Value;
            if (valorAleatorio <= acumulador)
            {
                Debug.Log($"¡Pez pescado! Seleccionado: {par.Key.nombreEspecie} (Rareza: {par.Key.rareza})");
                return par.Key;
            }
        }

        // Por seguridad, si algo falla, devolvemos el primer pez de la lista
        return todosLosPeces[0];
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