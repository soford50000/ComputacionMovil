using UnityEngine;

[CreateAssetMenu(fileName = "Cebo", menuName = "Pesca/Cebo")]
public sealed class CeboData : ScriptableObject
{
    public string nombre;
    public Sprite icono;
    [TextArea] public string descripcion;
    [Min(1f)] public float multiplicadorAparicion = 2f;
    public PezData[] especiesFavorecidas;

    public float MultiplicadorPara(PezData pez)
    {
        if (pez == null || especiesFavorecidas == null ||
            float.IsNaN(multiplicadorAparicion) || float.IsInfinity(multiplicadorAparicion)) return 1f;
        foreach (var especie in especiesFavorecidas)
            if (especie == pez) return Mathf.Max(1f, multiplicadorAparicion);
        return 1f;
    }
}
