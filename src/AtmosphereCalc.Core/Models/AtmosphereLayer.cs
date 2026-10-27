namespace AtmosphereCalc.Models;

/// <summary>
/// Расчётный слой атмосферы. Граница между слоями — тропопауза на 11 000 м.
/// </summary>
public enum AtmosphereLayer
{
    Troposphere,
    Stratosphere
}

public static class AtmosphereLayerExtensions
{
    public static string DisplayName(this AtmosphereLayer layer) => layer switch
    {
        AtmosphereLayer.Troposphere => "Тропосфера",
        AtmosphereLayer.Stratosphere => "Стратосфера",
        _ => layer.ToString()
    };
}
