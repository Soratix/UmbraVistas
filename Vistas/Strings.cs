using Dalamud.Game;

namespace Umbra.Vistas;

/// <summary>
/// Textes du widget en français et en anglais.
/// Umbra ne permet pas aux widgets tiers d'ajouter leurs clés à sa propre table I18N :
/// on suit la langue d'Umbra (I18N.GetCurrentLanguage : réglage d'Umbra, sinon langue de
/// l'interface Dalamud), et à défaut celle du client du jeu. Français si « fr », anglais sinon.
/// </summary>
internal static class Strings
{
    public static bool IsFrench
    {
        get {
            try {
                string lang = I18N.GetCurrentLanguage();
                if (!string.IsNullOrEmpty(lang)) return lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
            } catch {
                // I18N indisponible : on se rabat sur la langue du client.
            }

            try {
                return Framework.Service<IClientState>().ClientLanguage == ClientLanguage.French;
            } catch {
                return false;
            }
        }
    }

    private static readonly Dictionary<string, (string Fr, string En)> Table = new() {
        ["WidgetName"]  = ("Exploration", "Vistas"),
        ["WidgetDesc"]  = ("Les vistas du carnet d'exploration disponibles maintenant (heure et météo d'Eorzéa) et celles qui vont bientôt ouvrir.",
                           "Sightseeing log vistas available right now (Eorzea time and weather) and the ones opening soon."),
        ["Loading"]     = ("Chargement…", "Loading…"),
        ["LoadError"]   = ("Erreur de lecture", "Read error"),
        ["NotLoggedIn"] = ("Hors connexion", "Not logged in"),

        // Réglages
        ["Cfg.SoonMinutes"]          = ("Délai « Bientôt » (minutes réelles)", "\"Soon\" window (real minutes)"),
        ["Cfg.SoonMinutes.Desc"]     = ("Les vistas qui ouvrent dans ce délai apparaissent dans la section « Bientôt ».", "Vistas opening within this delay are listed in the \"Soon\" section."),
        ["Cfg.ShowProgress"]         = ("Afficher la progression globale", "Show overall progress"),
        ["Cfg.ShowProgress.Desc"]    = ("Ajoute le nombre de vistas faites sur le total (ex. 74/80) sous le compteur.", "Adds the number of completed vistas out of the total (e.g. 74/80) under the counter."),
        ["Cfg.HideLocked"]           = ("Masquer les extensions non débloquées", "Hide locked expansions"),
        ["Cfg.HideLocked.Desc"]      = ("Une extension compte comme débloquée dès qu'un de ses éthérites est harmonisé.", "An expansion counts as unlocked as soon as one of its aetherytes is attuned."),
        ["Cfg.Always"]               = ("Vistas sans condition", "Vistas without conditions"),
        ["Cfg.Always.Desc"]          = ("Vistas sans heure ni météo requise (presque toutes celles d'après A Realm Reborn).", "Vistas with no time or weather requirement (nearly all of them after A Realm Reborn)."),
        ["Cfg.Always.Section"]       = ("Section à part, hors compteur", "Separate section, not counted"),
        ["Cfg.Always.Count"]         = ("Comptées comme disponibles", "Counted as available"),
        ["Cfg.Always.Hide"]          = ("Masquées", "Hidden"),
        ["Cfg.MaxEntries"]           = ("Lignes maximum par section", "Maximum rows per section"),
        ["Cfg.ClickAction"]          = ("Clic sur une vista", "Clicking a vista"),
        ["Cfg.ClickAction.Desc"]     = ("Ce que fait un clic sur une ligne de la liste.", "What clicking a row of the list does."),
        ["Cfg.ClickAction.Flag"]     = ("Poser le drapeau", "Place the map flag"),
        ["Cfg.ClickAction.FlagMap"]  = ("Poser le drapeau et ouvrir la carte", "Place the map flag and open the map"),
        ["Cfg.ClickAction.FlagMapTeleport"] = ("Drapeau + carte + téléportation à l'éthérite le plus proche", "Flag + map + teleport to the nearest aetheryte"),

        // Barre et infobulle
        ["Available.One"]   = ("{0} dispo", "{0} available"),
        ["Available.Many"]  = ("{0} dispo", "{0} available"),
        ["NoneAvailable"]   = ("Aucune vista dispo", "No vista available"),
        ["AllDone"]         = ("Carnet complet", "Log complete"),
        ["Progress"]        = ("{0}/{1}", "{0}/{1}"),
        ["TooltipTitle"]    = ("Progression du carnet d'exploration", "Sightseeing log progress"),
        ["TooltipLine"]     = ("{0} : {1}/{2}", "{0}: {1}/{2}"),
        ["TooltipLocked"]   = ("{0} : non débloquée", "{0}: locked"),
        ["ArrLock"]         = ("Vistas 21 à 80 : terminez d'abord les 20 premières, puis parlez à Millith Ironheart (Vieille Gridania).",
                               "Vistas 21 to 80: complete the first 20, then talk to Millith Ironheart (Old Gridania)."),
        ["TooltipHint"]     = ("Clic : liste · clic sur une vista : {0}", "Click: list · click a vista: {0}"),
        ["Hint.Flag"]       = ("drapeau sur la carte", "map flag"),
        ["Hint.FlagMap"]    = ("drapeau et carte", "flag and map"),
        ["Hint.FlagMapTeleport"] = ("drapeau, carte et téléportation", "flag, map and teleport"),

        // Menu
        ["Section.Now"]     = ("Disponibles maintenant", "Available now"),
        ["Section.Soon"]    = ("Bientôt (dans {0})", "Soon (within {0})"),
        ["Section.Always"]  = ("Sans condition", "No conditions"),
        ["NothingNow"]      = ("Aucune vista disponible pour l'instant.", "No vista available right now."),
        ["More"]            = ("… et {0} autres", "… and {0} more"),
        ["ClosesIn"]        = ("ferme dans {0}", "closes in {0}"),
        ["OpensIn"]         = ("ouvre dans {0}", "opens in {0}"),
        ["FlagPlaced"]      = ("Drapeau posé : vista n° {0}, {1} ({2}).", "Flag placed: vista #{0}, {1} ({2})."),
        ["ChatSource"]      = ("Exploration", "Vistas"),
        ["CannotTeleport"]  = ("Téléportation impossible pour le moment.", "Cannot teleport right now."),
        ["ZoneNotFound"]    = ("Zone introuvable.", "Zone not found."),
        ["NoAetheryte"]     = ("Aucun éthérite harmonisé pour rejoindre cette vista.", "No attuned aetheryte to reach this vista."),
        ["TheAetheryte"]    = ("l'éthérite", "the aetheryte"),
        ["Teleporting"]     = ("Téléportation vers {0} ({1} gils) pour la vista n° {2}, {3}.", "Teleporting to {0} ({1} gil) for vista #{2}, {3}."),

        // Durées
        ["Minutes"]         = ("{0} min", "{0} min"),
        ["HoursMinutes"]    = ("{0} h {1:00}", "{0}h {1:00}"),
    };

    public static string T(string key)
    {
        if (!Table.TryGetValue(key, out var entry)) return key;
        return IsFrench ? entry.Fr : entry.En;
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>Choisit la clé « .One » ou « .Many » selon le nombre.</summary>
    public static string Plural(string key, int count) => T(count > 1 ? $"{key}.Many" : $"{key}.One", count);
}
