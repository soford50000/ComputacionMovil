using UnityEngine;

[CreateAssetMenu(fileName = "CatalogoCebos", menuName = "Pesca/Catálogo de cebos")]
public sealed class CatalogoCebos : ScriptableObject
{
    public CeboData[] cebos;
}
