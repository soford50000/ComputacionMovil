using UnityEngine;

public enum Rareza
{
    Comun,
    PocoComun,
    Rara,
    Epica,
    Legendaria
}

[CreateAssetMenu(fileName = "NuevoPez", menuName = "Pesca/Pez")]
public class PezData : ScriptableObject
{
    [Header("Identidad")]
    public string nombreEspecie;
    public Sprite foto;

    [Header("Rareza y dificultad")]
    public Rareza rareza;
    [Tooltip("Variable principal que controla la dificultad de captura (tensión, tirones, etc.)")]
    [Range(1f, 10f)]
    public float fuerzaPez = 1f;

    [Header("Datos informativos (ficha / catálogo)")]
    public float pesoMinimo;
    public float pesoMaximo;
    public float longitudMinima;
    public float longitudMaxima;
    public float velocidadRecogida = 1f;

    [Header("Descripción")]
    public string habitat;
    [TextArea(3, 8)]
    public string descripcion;
}