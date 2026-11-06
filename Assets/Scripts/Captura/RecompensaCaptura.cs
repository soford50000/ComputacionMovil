using System;

/// <summary>Economía inicial: recompensa entera, acotada y relativa a cada especie.</summary>
public static class RecompensaCaptura
{
    public static int Calcular(PezData pez, float pesoKg, float longitudCm)
    {
        if (pez == null) throw new ArgumentNullException(nameof(pez));
        double peso = Normalizar(pesoKg, pez.pesoMinimo, pez.pesoMaximo);
        double longitud = Normalizar(longitudCm, pez.longitudMinima, pez.longitudMaxima);
        if (float.IsNaN(pez.fuerzaPez) || float.IsInfinity(pez.fuerzaPez))
            throw new ArgumentException("La fuerza del pez debe ser finita.", nameof(pez));

        double fuerza = Math.Max(1d, Math.Min(10d, pez.fuerzaPez));
        double dificultad = 1d + 0.04d * (fuerza - 1d);
        double ejemplar = 0.70d + 0.60d * (0.70d * peso + 0.30d * longitud);
        // Se redondea una sola vez; las monedas nunca se almacenan como decimales.
        return Math.Max(1, (int)Math.Round(
            BasePorRareza(pez.rareza) * dificultad * ejemplar,
            MidpointRounding.AwayFromZero));
    }

    public static int BasePorRareza(Rareza rareza)
    {
        switch (rareza)
        {
            case Rareza.Comun: return 40;
            case Rareza.PocoComun: return 75;
            case Rareza.Rara: return 140;
            case Rareza.Epica: return 260;
            case Rareza.Legendaria: return 500;
            default: throw new ArgumentOutOfRangeException(nameof(rareza));
        }
    }

    private static double Normalizar(float valor, float min, float max)
    {
        if (float.IsNaN(valor) || float.IsInfinity(valor) ||
            float.IsNaN(min) || float.IsInfinity(min) ||
            float.IsNaN(max) || float.IsInfinity(max) || min < 0f || max < min ||
            valor < min || valor > max)
            throw new ArgumentException("La medida debe estar dentro de un rango válido de la especie.");

        // Una medida fija aporta un valor neutral, sin premiar ni castigar a la especie.
        return max == min ? 0.5d : ((double)valor - min) / ((double)max - min);
    }
}
