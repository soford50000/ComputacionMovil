using System;
using UnityEditor;
using UnityEngine;

public static class ValidarBalanceForcejeo
{
    public static void Ejecutar()
    {
        var payara = AssetDatabase.LoadAssetAtPath<PezData>("Assets/Data/Peces/Pez_Payara.asset");
        var fx = AssetDatabase.LoadAssetAtPath<CanaData>("Assets/Data/Canas/Cana_ShimanoFx.asset");
        var colt = AssetDatabase.LoadAssetAtPath<CanaData>("Assets/Data/Canas/Cana_ShimanoColtSniperLimited100H.asset");
        VerificarTirones(payara, fx, 1.16f);
        VerificarTirones(payara, colt, 1.004f);
        if (TensionSystem.CalcularFactorTirones(Rareza.Comun, 10f) != 1f ||
            !Mathf.Approximately(TensionSystem.CalcularFactorTirones(Rareza.Legendaria, 10f), 1.2f))
            throw new InvalidOperationException("Ajuste de tirones fuera de los límites de rareza.");

        int combinaciones = 0;
        var catalogo = Resources.Load<CatalogoCanas>("CatalogoCanas");
        foreach (var guid in AssetDatabase.FindAssets("t:PezData", new[] { "Assets/Data/Peces" }))
        {
            var pez = AssetDatabase.LoadAssetAtPath<PezData>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var cana in catalogo.canas)
            {
                int nivel = 4 - cana.puestoPotenciaPrevisto;
                float fuerza = Mathf.Max(1f, pez.fuerzaPez - 1.3f * nivel);
                float minimo = 0.25f + fuerza * 0.065f - nivel * 0.02f;
                var aprobado = CapturaCalculoRF5.Evaluar(pez, cana, minimo + 0.0001f);
                var fallo = CapturaCalculoRF5.Evaluar(pez, cana, minimo - 0.0001f);
                if (cana.estadisticasPendientes || cana.poderCana != 1 + 2 * nivel ||
                    cana.bonificacionTolerancia != nivel || !aprobado.Capturada || fallo.Capturada ||
                    !Mathf.Approximately(fuerza, aprobado.FuerzaEfectiva) ||
                    !Mathf.Approximately(minimo, aprobado.PorcentajeNecesario))
                    throw new InvalidOperationException(pez.nombreEspecie + " / " + cana.nombreCana);
                combinaciones++;
            }
        }
        if (combinaciones == 0) throw new InvalidOperationException("Catálogo vacío.");
        Debug.Log($"Balance de forcejeo verificado en {combinaciones} combinaciones.");
    }

    private static void VerificarTirones(PezData pez, CanaData cana, float esperado)
    {
        float fuerza = CapturaCalculoRF5.CalcularFuerzaEfectiva(pez.fuerzaPez, cana.poderCana);
        if (!Mathf.Approximately(TensionSystem.CalcularFactorTirones(pez.rareza, fuerza), esperado))
            throw new InvalidOperationException("Tirones incorrectos para " + pez.nombreEspecie + " con " + cana.nombreCana);
    }
}
