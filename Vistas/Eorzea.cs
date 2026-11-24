using Lumina.Excel.Sheets;

namespace Umbra.Vistas;

/// <summary>
/// Heure et météo d'Eorzéa, calculées sans appel au jeu.
/// Une heure d'Eorzéa dure 175 secondes réelles, et la météo change toutes les 8 heures d'Eorzéa
/// (1 400 s). Les deux sont alignées sur l'époque Unix : on travaille donc en « index d'heure »
/// = secondes Unix / 175.
/// </summary>
public static class Eorzea
{
    public const long RealSecondsPerHour = 175;
    public const long HoursPerWeather    = 8;

    public static double NowUnixSeconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    /// <summary>Index de l'heure d'Eorzéa courante (nombre d'heures d'Eorzéa depuis l'époque Unix).</summary>
    public static long CurrentHourIndex => (long)(NowUnixSeconds / RealSecondsPerHour);

    public static int HourOfDay(long hourIndex) => (int)(hourIndex % 24);

    /// <summary>Secondes réelles entre maintenant et le début de l'heure d'Eorzéa donnée.</summary>
    public static double SecondsUntil(long hourIndex) => hourIndex * RealSecondsPerHour - NowUnixSeconds;

    /// <summary>
    /// Valeur 0-99 qui détermine la météo d'une période. Algorithme connu du jeu
    /// (le même que FFXIVWeather et les sites de prévision).
    /// </summary>
    public static int WeatherChance(long unixSeconds)
    {
        uint bell      = (uint)(unixSeconds / RealSecondsPerHour);
        uint increment = (bell + 8 - bell % 8) % 24;
        uint totalDays = (uint)(unixSeconds / (RealSecondsPerHour * 24));
        uint calcBase  = totalDays * 100 + increment;
        uint step1     = (calcBase << 11) ^ calcBase;
        uint step2     = (step1 >> 8) ^ step1;

        return (int)(step2 % 100);
    }

    private static readonly Dictionary<(uint Rate, long Period), uint> Cache = [];

    /// <summary>Météo (RowId de la feuille Weather) d'une table WeatherRate pendant l'heure d'Eorzéa donnée.</summary>
    public static uint WeatherAt(uint weatherRateId, long hourIndex)
    {
        long period = hourIndex / HoursPerWeather;

        if (Cache.TryGetValue((weatherRateId, period), out uint cached)) return cached;
        if (Cache.Count > 100_000) Cache.Clear();

        uint weather = 0;
        var  rates   = Framework.Service<IDataManager>().GetExcelSheet<WeatherRate>();

        if (rates.TryGetRow(weatherRateId, out var rate)) {
            int chance     = WeatherChance(period * HoursPerWeather * RealSecondsPerHour);
            int cumulative = 0;

            for (int i = 0; i < rate.Rate.Count; i++) {
                cumulative += rate.Rate[i];
                if (chance < cumulative) {
                    weather = rate.Weather[i].RowId;
                    break;
                }
            }
        }

        return Cache[(weatherRateId, period)] = weather;
    }

    public static string FormatDuration(double seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(seconds)));

        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes}:{ts.Seconds:00}";
    }

    public static string FormatMinutes(int minutes) =>
        minutes >= 60 ? Strings.T("HoursMinutes", minutes / 60, minutes % 60) : Strings.T("Minutes", minutes);
}

/// <summary>État d'une vista non faite à un instant donné.</summary>
/// <param name="IsOpen">Conditions remplies maintenant.</param>
/// <param name="OpensAt">Index de l'heure d'Eorzéa d'ouverture (= maintenant si ouverte).</param>
/// <param name="ClosesAt">Index de l'heure d'Eorzéa de fermeture, null si jamais dans l'horizon de recherche.</param>
/// <param name="WeatherId">Météo pendant la fenêtre (requise), 0 si la vista n'a pas de condition météo.</param>
public readonly record struct VistaWindow(Vista Vista, bool IsOpen, long OpensAt, long? ClosesAt, uint WeatherId);

public static class VistaSchedule
{
    /// <summary>Horizon de recherche : 2 400 heures d'Eorzéa ≈ 4,9 jours réels.</summary>
    private const int HorizonHours = 2400;

    private static bool IsOk(Vista v, long hour) =>
        v.IsTimeOk(Eorzea.HourOfDay(hour))
        && (!v.HasWeather || v.Weathers.Contains(Eorzea.WeatherAt(v.WeatherRateId, hour)));

    /// <summary>
    /// Prochaine fenêtre de la vista à partir de l'heure donnée (en cours ou à venir),
    /// null si aucune dans l'horizon de recherche.
    /// </summary>
    public static VistaWindow? Compute(Vista v, long nowHour)
    {
        if (v.IsUnconditional) return new(v, true, nowHour, null, 0);

        long limit = nowHour + HorizonHours;
        long start = nowHour;

        while (start < limit && !IsOk(v, start)) start++;
        if (start >= limit) return null;

        long end = start + 1;
        while (end < limit && IsOk(v, end)) end++;

        uint weather = v.HasWeather ? Eorzea.WeatherAt(v.WeatherRateId, start) : 0;

        return new(v, start == nowHour, start, end >= limit ? null : end, weather);
    }
}
