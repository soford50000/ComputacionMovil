using UnityEngine;

/// <summary>Calcula columnas con el ancho real del viewport, no con píxeles de pantalla.</summary>
[RequireComponent(typeof(UnityEngine.UI.GridLayoutGroup))]
public sealed class CuadriculaExpedicion : MonoBehaviour
{
    public float AnchoPreferido = 215;
    public float Alto = 210;
    public int MaximoColumnas = 4;
    private UnityEngine.UI.GridLayoutGroup cuadricula;

    private void OnEnable() { Actualizar(); }
    private void OnRectTransformDimensionsChange() { Actualizar(); }

    private void Actualizar()
    {
        if (cuadricula == null) cuadricula = GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (cuadricula == null) return;
        float ancho = ((RectTransform)transform).rect.width - cuadricula.padding.horizontal;
        if (ancho <= 0) return;
        float espacio = cuadricula.spacing.x;
        int columnas = Mathf.Clamp(Mathf.FloorToInt((ancho + espacio) / (AnchoPreferido + espacio)), 1, MaximoColumnas);
        cuadricula.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        cuadricula.constraintCount = columnas;
        cuadricula.cellSize = new Vector2((ancho - (columnas - 1) * espacio) / columnas, Alto);
    }
}
