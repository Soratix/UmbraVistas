using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Umbra.Vistas;

/// <summary>Une vista du carnet d'exploration, avec ses conditions.</summary>
public sealed record Vista(
    int     Index,          // index dans le carnet (0 = vista n° 1), utilisé par PlayerState.IsAdventureComplete
    uint    RowId,
    string  Name,
    string  ZoneName,
    uint    TerritoryId,
    uint    MapId,
    Vector3 WorldPosition,
    string  Emote,
    uint    ExVersion,
    int     StartHour,      // heure d'Eorzéa d'ouverture, -1 si aucune condition horaire
    int     EndHour,        // heure d'Eorzéa de fermeture (exclue)
    uint    WeatherRateId,
    uint[]  Weathers        // météos requises (vide si aucune)
)
{
    public int  Number         => Index + 1;
    public bool HasTime        => StartHour >= 0;
    public bool HasWeather     => Weathers.Length > 0;
    public bool IsUnconditional => !HasTime && !HasWeather;

    public bool IsTimeOk(int hourOfDay)
    {
        if (!HasTime) return true;

        return StartHour < EndHour
            ? hourOfDay >= StartHour && hourOfDay < EndHour
            : hourOfDay >= StartHour || hourOfDay < EndHour; // fenêtre à cheval sur minuit
    }
}

public sealed record Expansion(uint Id, string Name);

public static class VistaRepository
{
    private static List<Vista>?      _vistas;
    private static List<Expansion>?  _expansions;
    private static Dictionary<uint, uint>? _territoryExVersion;

    public static IReadOnlyList<Vista>     Vistas     => _vistas ??= Load();
    public static IReadOnlyList<Expansion> Expansions => _expansions ??= LoadExpansions();

    /// <summary>Extension (ExVersion) d'un territoire, 0 si inconnu.</summary>
    public static uint ExVersionOf(uint territoryId)
    {
        _territoryExVersion ??= Framework.Service<IDataManager>().GetExcelSheet<TerritoryType>()
            .ToDictionary(t => t.RowId, t => t.ExVersion.RowId);

        return _territoryExVersion.GetValueOrDefault(territoryId);
    }

    private static List<Vista> Load()
    {
        var vistas = BuildFrom(Framework.Service<IDataManager>().Excel);
        Logger.Info($"[Vistas] {vistas.Count} vistas chargées.");
        return vistas;
    }

    private static List<Expansion> LoadExpansions() =>
        Framework.Service<IDataManager>().GetExcelSheet<ExVersion>()
            .Select(e => new Expansion(e.RowId, e.Name.ExtractText()))
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .ToList();

    /// <summary>Construit la liste des vistas à partir de la feuille Adventure du jeu.</summary>
    public static List<Vista> BuildFrom(ExcelModule excel)
    {
        List<Vista> result = [];
        int         index  = 0;

        // L'index d'une vista dans le carnet est sa position dans la feuille Adventure
        // (comme Tourist ; Umbra utilise RowId - 2162688, ce qui revient au même).
        foreach (var adv in excel.GetSheet<Adventure>()) {
            int i = index++;

            if (!adv.Level.TryGetValue(out var level)) continue;
            if (!level.Territory.TryGetValue(out var territory)) continue;

            string name = adv.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(name)) continue;

            string zone  = territory.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
            string emote = adv.Emote.ValueNullable?.TextCommand.ValueNullable?.Command.ExtractText() ?? "";

            // MinTime / MaxTime sont au format HHMM ; MaxTime est la dernière minute incluse
            // (ex. 1200 → 1659 = de 12:00 à 17:00). 0 et 0 = pas de condition horaire.
            int startHour = -1, endHour = -1;
            if (adv.MinTime != 0 || adv.MaxTime != 0) {
                int startMinute = adv.MinTime / 100 * 60 + adv.MinTime % 100;
                int endMinute   = (adv.MaxTime / 100 * 60 + adv.MaxTime % 100 + 1) % 1440;

                if (startMinute % 60 != 0 || endMinute % 60 != 0) {
                    Logger.Warning($"[Vistas] Horaire non aligné sur l'heure pour la vista {i + 1} : {adv.MinTime}-{adv.MaxTime}");
                }

                startHour = startMinute / 60;
                endHour   = (endMinute + 59) / 60 % 24;
            }

            result.Add(new(
                i,
                adv.RowId,
                name,
                zone,
                territory.RowId,
                level.Map.RowId,
                new(level.X, level.Y, level.Z),
                emote,
                territory.ExVersion.RowId,
                startHour,
                endHour,
                territory.WeatherRate.RowId,
                RequiredWeathers.GetValueOrDefault(adv.RowId, [])
            ));
        }

        return result;
    }

    /// <summary>
    /// Météos requises par vista. La feuille Adventure ne contient pas cette information :
    /// seule une partie des vistas d'A Realm Reborn en a une. Table reprise d'Umbra
    /// (Umbra/src/Markers/Library/VistaMarkerFactory.cs, AGPL-3.0), identique à celle de Tourist.
    /// Clé : RowId de la feuille Adventure. Valeurs : RowId de la feuille Weather.
    /// </summary>
    private static readonly IReadOnlyDictionary<uint, uint[]> RequiredWeathers = new Dictionary<uint, uint[]> {
        [2162688] = [2, 1],
        [2162689] = [1, 2],
        [2162690] = [7, 8],
        [2162691] = [2, 1],
        [2162692] = [3],
        [2162693] = [2, 1],
        [2162694] = [4],
        [2162695] = [2, 1],
        [2162696] = [3],
        [2162697] = [1, 2],
        [2162698] = [2, 1],
        [2162699] = [2, 1],
        [2162700] = [1, 2],
        [2162701] = [2, 1],
        [2162702] = [3],
        [2162703] = [2, 1],
        [2162704] = [4],
        [2162705] = [7, 8],
        [2162706] = [3],
        [2162707] = [2, 1],
        [2162708] = [2, 1],
        [2162709] = [1, 2],
        [2162710] = [7, 8],
        [2162711] = [1, 2],
        [2162712] = [7, 8],
        [2162713] = [1, 2],
        [2162714] = [6],
        [2162715] = [2, 1],
        [2162716] = [1, 2],
        [2162717] = [2, 1],
        [2162718] = [1, 2],
        [2162719] = [10],
        [2162720] = [2, 1],
        [2162721] = [3],
        [2162722] = [1, 2],
        [2162723] = [7, 8],
        [2162724] = [2, 1],
        [2162725] = [7, 8],
        [2162726] = [7, 8],
        [2162727] = [1, 2],
        [2162728] = [2, 1],
        [2162729] = [1, 2],
        [2162730] = [9],
        [2162731] = [10],
        [2162732] = [1, 2],
        [2162733] = [4],
        [2162734] = [2, 1],
        [2162735] = [2, 1],
        [2162736] = [1, 2],
        [2162737] = [3],
        [2162738] = [1, 2],
        [2162739] = [2, 1],
        [2162740] = [11],
        [2162741] = [1, 2],
        [2162742] = [1, 2],
        [2162743] = [2, 1],
        [2162744] = [8],
        [2162745] = [4],
        [2162746] = [2, 1],
        [2162747] = [14],
        [2162748] = [2, 1],
        [2162749] = [14],
        [2162750] = [1, 2],
        [2162751] = [2, 1],
        [2162752] = [2, 1],
        [2162753] = [3],
        [2162754] = [4],
        [2162755] = [1, 2],
        [2162756] = [4],
        [2162757] = [16],
        [2162758] = [2, 1],
        [2162759] = [1, 2],
        [2162760] = [16, 15],
        [2162761] = [2, 1],
        [2162762] = [2, 1],
        [2162763] = [1, 2],
        [2162764] = [17],
        [2162765] = [2, 1],
        [2162766] = [1, 2],
        [2162767] = [2, 1],
    };
}
