using UnityEngine;

public enum ModeloCana
{
    AbuGarciaVengeance = 0,
    ShimanoColtSniperLimited100H = 1,
    ShimanoFx = 2,
    DaiwaDSK25BF662M = 3
}

[CreateAssetMenu(fileName = "NuevaCana", menuName = "Pesca/Cana")]
public class CanaData : ScriptableObject
{
    [Header("Identidad")]
    public string nombreCana;
    public Sprite iconoCana; // Imagen visual para la tienda o inventario
    public int precio;
    public ModeloCana modeloVisual;
    [Tooltip("Posición prevista, sin efecto en captura. 1 = más potente; 0 = sin definir.")]
    public int puestoPotenciaPrevisto;
    public bool estadisticasPendientes;

    [Header("Estadísticas (RF7)")]
    [Tooltip("Poder de la caña. Cuanto mayor sea, más contrarrestará la fuerza de los peces difíciles.")]
    [Range(1f, 10f)]
    public float poderCana = 1f;

    [Tooltip("Tolerancia extra en la barra. En RF5, cada punto reduce en 2 puntos porcentuales el control seguro requerido.")]
    public float bonificacionTolerancia = 0f;
}
