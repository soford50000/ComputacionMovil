using UnityEngine;

/// <summary>Lista única de equipo disponible, independiente de copias antiguas de la escena.</summary>
[CreateAssetMenu(fileName = "CatalogoCanas", menuName = "Pesca/Catálogo de cañas")]
public sealed class CatalogoCanas : ScriptableObject
{
    public CanaData[] canas;
}
