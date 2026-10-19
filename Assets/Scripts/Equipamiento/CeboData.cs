using UnityEngine;

[CreateAssetMenu(fileName = "Cebo", menuName = "Pesca/Cebo")]
public sealed class CeboData : ScriptableObject
{
    public string nombre;
    public Sprite icono;
    [TextArea] public string descripcion;
    [Min(0)] public int precio = 50;
    [Min(1f)] public float multiplicadorAparicion = 2f;
    [Range(2, 3)] public int usosPorCompra = 3;
    public PezData[] especiesFavorecidas;

    public int UsosPorCompra => Mathf.Clamp(usosPorCompra, 2, 3);

    public float MultiplicadorPara(PezData pez)
    {
        if (pez == null || especiesFavorecidas == null ||
            float.IsNaN(multiplicadorAparicion) || float.IsInfinity(multiplicadorAparicion)) return 1f;
        foreach (var especie in especiesFavorecidas)
            if (especie == pez) return Mathf.Max(1f, multiplicadorAparicion);
        return 1f;
    }
}
