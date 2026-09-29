using UnityEngine;

[CreateAssetMenu(fileName = "NuevaCana", menuName = "Pesca/Cana")]
public class CanaData : ScriptableObject
{
    [Header("Identidad")]
    public string nombreCana;
    public Sprite iconoCana; // Imagen visual para la tienda o inventario
    public int precio;

    [Header("Estadísticas (RF7)")]
    [Tooltip("Poder de la caña. Cuanto mayor sea, más contrarrestará la fuerza de los peces difíciles.")]
    [Range(1f, 10f)]
    public float poderCana = 1f;

    [Tooltip("Tolerancia extra en la barra de tensión (ej. hace que la zona segura sea un poco más flexible).")]
    public float bonificacionTolerancia = 0f;
}