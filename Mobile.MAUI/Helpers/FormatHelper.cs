namespace Mobile.MAUI.Helpers;

public static class FormatHelper
{
    public static string FormatQuantity(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##");
    }

    public static decimal RoundOfNearestHundredThousands(decimal value)
    {
        return Math.Round(value, 5, MidpointRounding.AwayFromZero);
    }
}
