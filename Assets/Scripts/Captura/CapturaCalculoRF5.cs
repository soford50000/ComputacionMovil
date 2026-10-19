using System;
using UnityEngine;

/// <summary>Resultado calculado para un intento de captura RF5.</summary>
public sealed class CapturaEvaluacion
{
    public bool Capturada { get; }
    public float PorcentajeControlado { get; }
    public float PorcentajeNecesario { get; }
    public float FuerzaEfectiva { get; }

    internal CapturaEvaluacion(bool capturada, float controlado, float necesario, float fuerzaEfectiva)
    {
        Capturada = capturada;
        PorcentajeControlado = controlado;
        PorcentajeNecesario = necesario;
        FuerzaEfectiva = fuerzaEfectiva;
    }
}

/// <summary>
/// RF5: combina el control del jugador registrado por RF4 con la fuerza del pez,
/// el poder de la caña y su bonificación de tolerancia.
/// </summary>
public static class CapturaCalculoRF5
{
    /// <param name="porcentajeControlado">
    /// Proporción del intento que RF4 mantuvo en rango seguro, de 0 a 1.
    /// </param>
    public static CapturaEvaluacion Evaluar(PezData pez, CanaData cana, float porcentajeControlado)
    {
        if (pez == null) throw new ArgumentNullException(nameof(pez));
        if (cana == null) throw new ArgumentNullException(nameof(cana));
        if (float.IsNaN(porcentajeControlado) || float.IsInfinity(porcentajeControlado))
            throw new ArgumentOutOfRangeException(nameof(porcentajeControlado));

        float control = Mathf.Clamp01(porcentajeControlado);
        float fuerzaPez = Mathf.Clamp(pez.fuerzaPez, 1f, 10f);
        float poderCana = Mathf.Clamp(cana.poderCana, 1f, 10f);

        // Cada punto de poder sobre el nivel base reduce 0.65 de fuerza efectiva.
        float fuerzaEfectiva = Mathf.Max(1f, fuerzaPez - (poderCana - 1f) * 0.65f);

        // Un pez más fuerte exige más tiempo de control seguro. La tolerancia
        // de la caña reduce el requisito en 2 puntos porcentuales por punto.
        float tolerancia = float.IsNaN(cana.bonificacionTolerancia) ||
                           float.IsInfinity(cana.bonificacionTolerancia)
            ? 0f
            : Mathf.Max(0f, cana.bonificacionTolerancia);
        float porcentajeNecesario = Mathf.Clamp(
            0.25f + fuerzaEfectiva * 0.065f - tolerancia * 0.02f,
            0.15f,
            0.95f);

        return new CapturaEvaluacion(
            control >= porcentajeNecesario,
            control,
            porcentajeNecesario,
            fuerzaEfectiva);
    }
}
