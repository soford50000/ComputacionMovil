#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>Elimina solo los PlayerPrefs de progreso de Road to the Amazon.</summary>
public static class ResetearProgresoJuego
{
    [MenuItem("Road to the Amazon/Resetear progreso guardado")]
    private static void Resetear()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Detén Play", "Detén la ejecución antes de resetear el progreso.", "Entendido");
            return;
        }

        bool confirmar = EditorUtility.DisplayDialog(
            "Resetear progreso guardado",
            "Se borrarán el saldo, las especies descubiertas y las compras de cañas y carnadas de este juego. Los assets y archivos del proyecto no cambiarán.",
            "Resetear",
            "Cancelar");
        if (!confirmar) return;

        PlayerPrefs.DeleteKey("RoadToAmazonas.SaldoOro.v1");
        BorrarClavesDeAssets<PezData>("RoadToAmazonas.Catalogo.v1.", true);
        BorrarClavesDeAssets<CanaData>("RoadToAmazonas.CanasCompradas.v1.", false);
        BorrarClavesDeAssets<CeboData>("RoadToAmazonas.CebosComprados.v1.", false);
        PlayerPrefs.Save();
        Debug.Log("Progreso local de Road to the Amazon reseteado.");
    }

    private static void BorrarClavesDeAssets<T>(string prefijo, bool escaparNombre) where T : UnityEngine.Object
    {
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset == null) continue;
            string id = escaparNombre ? Uri.EscapeDataString(asset.name) : asset.name;
            PlayerPrefs.DeleteKey(prefijo + id);
        }
    }
}
#endif
