using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Umbra.Vistas;

/// <summary>
/// Le nom et la description d'un [ToolbarWidget] sont des constantes. Umbra lit l'attribut
/// avec GetCustomAttribute au démarrage de son framework, ce qui accepte une sous-classe :
/// celle-ci choisit le nom et la description dans la langue d'Umbra à ce moment-là.
/// </summary>
internal sealed class LocalizedToolbarWidgetAttribute() : ToolbarWidgetAttribute(
    "VistasTracker",
    Strings.T("WidgetName"),
    Strings.T("WidgetDesc"),
    ["vista", "sightseeing", "log", "tourist", "exploration", "carnet", "panorama"]
);

[LocalizedToolbarWidget]
public sealed unsafe class VistasWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : StandardToolbarWidget(info, guid, configValues)
{
    private enum Section { Now, Soon, Always }

    /// <summary>Les 20 premières vistas d'ARR débloquent les vistas 21 à 80.</summary>
    private const int ArrInitialCount = 20;
    private const int ArrTotalCount   = 80;

    private static readonly Color OpenColor = new(0xFF66DD66);
    private static readonly Color SoonColor = new(0xFF5AB4FF);

    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Text |
        StandardWidgetFeatures.SubText |
        StandardWidgetFeatures.Icon |
        StandardWidgetFeatures.CustomizableIcon;

    protected override string          DefaultIconType        => IconTypeFontAwesome;
    protected override FontAwesomeIcon DefaultFontAwesomeIcon => FontAwesomeIcon.Binoculars;

    public override MenuPopup Popup { get; } = new();

    private readonly Dictionary<Section, MenuPopup.Group> _groups  = [];
    private readonly Dictionary<string, MenuPopup.Button> _buttons = [];

    private bool[]            _done          = [];
    private HashSet<uint>     _unlockedEx    = [];
    private List<VistaWindow> _now           = [];
    private List<VistaWindow> _soon          = [];
    private List<VistaWindow> _always        = [];
    private long              _computedHour  = -1;
    private string            _configKey     = string.Empty;
    private string            _menuSignature = string.Empty;
    private DateTime          _nextStateCheck = DateTime.MinValue;
    private DateTime          _nextLabels     = DateTime.MinValue;
    private bool              _dirty          = true;

    // ------------------------------------------------------------------ Config

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        string cat = Strings.T("WidgetName");

        return [
            ..base.GetConfigVariables(),
            new IntegerWidgetConfigVariable(
                "SoonMinutes", Strings.T("Cfg.SoonMinutes"), Strings.T("Cfg.SoonMinutes.Desc"), 60, 5, 1440
            ) { Category = cat },
            new BooleanWidgetConfigVariable(
                "ShowProgress", Strings.T("Cfg.ShowProgress"), Strings.T("Cfg.ShowProgress.Desc"), false
            ) { Category = cat },
            new BooleanWidgetConfigVariable(
                "HideLockedExpansions", Strings.T("Cfg.HideLocked"), Strings.T("Cfg.HideLocked.Desc"), true
            ) { Category = cat },
            new SelectWidgetConfigVariable(
                "AlwaysAvailable", Strings.T("Cfg.Always"), Strings.T("Cfg.Always.Desc"), "Section",
                new() {
                    ["Section"] = Strings.T("Cfg.Always.Section"),
                    ["Count"]   = Strings.T("Cfg.Always.Count"),
                    ["Hide"]    = Strings.T("Cfg.Always.Hide"),
                }
            ) { Category = cat },
            new IntegerWidgetConfigVariable("MaxEntries", Strings.T("Cfg.MaxEntries"), null, 15, 1, 100) { Category = cat },
            new SelectWidgetConfigVariable(
                "ClickAction", Strings.T("Cfg.ClickAction"), Strings.T("Cfg.ClickAction.Desc"), "FlagMapTeleport",
                new() {
                    ["Flag"]            = Strings.T("Cfg.ClickAction.Flag"),
                    ["FlagMap"]         = Strings.T("Cfg.ClickAction.FlagMap"),
                    ["FlagMapTeleport"] = Strings.T("Cfg.ClickAction.FlagMapTeleport"),
                }
            ) { Category = cat },
        ];
    }

    // --------------------------------------------------------------- Lifecycle

    protected override void OnLoad()
    {
        Popup.OnPopupOpen += OnPopupOpened;

        SetText(Strings.T("WidgetName"));
        SetSubText(Strings.T("Loading"));
    }

    protected override void OnUnload()
    {
        Popup.OnPopupOpen -= OnPopupOpened;
        ClearMenu();
    }

    protected override void OnDraw()
    {
        var utcNow = DateTime.UtcNow;

        // État du joueur (vistas faites, éthérites) : relu toutes les 2 s, ce qui est très peu coûteux.
        // Le calcul des fenêtres n'est refait que si cet état, la configuration ou l'heure d'Eorzéa change.
        if (utcNow >= _nextStateCheck) {
            _nextStateCheck = utcNow.AddSeconds(2);

            if (!Framework.Service<IClientState>().IsLoggedIn) {
                SetText(Strings.T("WidgetName"));
                SetSubText(Strings.T("NotLoggedIn"));
                SetTooltip(null);
                _computedHour = -1;
                return;
            }

            try {
                RefreshPlayerState();
            } catch (Exception e) {
                Logger.Error($"[Vistas] Impossible de lire l'état du joueur : {e}");
                SetSubText(Strings.T("LoadError"));
                return;
            }
        }

        if (_done.Length == 0) return;

        long hour = Eorzea.CurrentHourIndex;

        if (_dirty || hour != _computedHour) {
            try {
                Recompute(hour);
            } catch (Exception e) {
                Logger.Error($"[Vistas] Erreur de calcul des vistas : {e}");
                SetSubText(Strings.T("LoadError"));
                return;
            }

            _dirty        = false;
            _computedHour = hour;
            _nextLabels   = DateTime.MinValue;
            UpdateToolbarButton();
        }

        // Les comptes à rebours du menu sont rafraîchis une fois par seconde, sans recalcul.
        if (Popup.IsOpen && utcNow >= _nextLabels) {
            _nextLabels = utcNow.AddSeconds(1);
            UpdateMenu();
        }
    }

    // -------------------------------------------------------------- Collecte

    private void RefreshPlayerState()
    {
        var vistas = VistaRepository.Vistas;

        PlayerState* ps = PlayerState.Instance();
        if (ps == null) return;

        bool[] done = new bool[vistas.Count];
        for (int i = 0; i < vistas.Count; i++) {
            done[i] = ps->IsAdventureComplete((uint)vistas[i].Index);
        }

        // Une extension compte comme débloquée dès qu'un de ses éthérites est harmonisé.
        HashSet<uint> unlocked = [0];
        foreach (var aetheryte in Framework.Service<IAetheryteList>()) {
            unlocked.Add(VistaRepository.ExVersionOf(aetheryte.TerritoryId));
        }

        string configKey = string.Join("|",
            GetConfigValue<int>("SoonMinutes"),
            GetConfigValue<bool>("HideLockedExpansions"),
            GetConfigValue<string>("AlwaysAvailable"),
            GetConfigValue<bool>("ShowProgress"),
            GetConfigValue<string>("ClickAction"),
            Strings.IsFrench);

        if (!done.SequenceEqual(_done) || !unlocked.SetEquals(_unlockedEx) || configKey != _configKey) {
            _done       = done;
            _unlockedEx = unlocked;
            _configKey  = configKey;
            _dirty      = true;
        }
    }

    private bool IsExpansionVisible(uint exVersion) =>
        !GetConfigValue<bool>("HideLockedExpansions") || _unlockedEx.Contains(exVersion);

    /// <summary>Vistas 21 à 80 verrouillées tant que les 20 premières ne sont pas toutes faites.</summary>
    private bool IsArrLocked => _done.Length >= ArrInitialCount && _done.Take(ArrInitialCount).Any(d => !d);

    private bool IsDoable(Vista v) =>
        !_done[v.Index]
        && IsExpansionVisible(v.ExVersion)
        && !(IsArrLocked && v.Index >= ArrInitialCount && v.Index < ArrTotalCount);

    private void Recompute(long hour)
    {
        long   soonLimit = hour + (long)Math.Ceiling(GetConfigValue<int>("SoonMinutes") * 60.0 / Eorzea.RealSecondsPerHour);
        string always    = GetConfigValue<string>("AlwaysAvailable");

        List<VistaWindow> now = [], soon = [], unconditional = [];

        foreach (var vista in VistaRepository.Vistas) {
            if (vista.Index >= _done.Length || !IsDoable(vista)) continue;

            if (vista.IsUnconditional) {
                if (always == "Count") now.Add(new(vista, true, hour, null, 0));
                else if (always == "Section") unconditional.Add(new(vista, true, hour, null, 0));
                continue;
            }

            var window = VistaSchedule.Compute(vista, hour);
            if (window == null) continue;

            if (window.Value.IsOpen) now.Add(window.Value);
            else if (window.Value.OpensAt <= soonLimit) soon.Add(window.Value);
        }

        // Disponibles : temps restant croissant (sans condition en dernier). Bientôt : ouverture la plus proche.
        _now    = now.OrderBy(w => w.ClosesAt ?? long.MaxValue).ThenBy(w => w.Vista.Index).ToList();
        _soon   = soon.OrderBy(w => w.OpensAt).ThenBy(w => w.Vista.Index).ToList();
        _always = unconditional.OrderBy(w => w.Vista.Index).ToList();

        _menuSignature = string.Empty; // force la reconstruction du menu
    }

    // ----------------------------------------------------------------- Display

    private void UpdateToolbarButton()
    {
        var vistas  = VistaRepository.Vistas;
        var visible = vistas.Where(v => v.Index < _done.Length && IsExpansionVisible(v.ExVersion)).ToList();
        int done    = visible.Count(v => _done[v.Index]);

        if (done == visible.Count) {
            SetText(Strings.T("AllDone"));
        } else {
            SetText(_now.Count == 0 ? Strings.T("NoneAvailable") : Strings.Plural("Available", _now.Count));
        }

        SetSubText(GetConfigValue<bool>("ShowProgress") ? Strings.T("Progress", done, visible.Count) : null);

        // Infobulle : progression par extension.
        List<string> lines = [Strings.T("TooltipTitle")];

        foreach (var ex in VistaRepository.Expansions) {
            var exVistas = vistas.Where(v => v.ExVersion == ex.Id && v.Index < _done.Length).ToList();
            if (exVistas.Count == 0) continue;

            if (!_unlockedEx.Contains(ex.Id)) {
                if (!GetConfigValue<bool>("HideLockedExpansions")) {
                    lines.Add(Strings.T("TooltipLine", ex.Name, exVistas.Count(v => _done[v.Index]), exVistas.Count));
                } else {
                    lines.Add(Strings.T("TooltipLocked", ex.Name));
                }
                continue;
            }

            lines.Add(Strings.T("TooltipLine", ex.Name, exVistas.Count(v => _done[v.Index]), exVistas.Count));
        }

        if (IsArrLocked) {
            lines.Add("");
            lines.Add(Strings.T("ArrLock"));
        }

        lines.Add("");
        lines.Add(Strings.T("TooltipHint", Strings.T(GetConfigValue<string>("ClickAction") switch {
            "FlagMap"         => "Hint.FlagMap",
            "FlagMapTeleport" => "Hint.FlagMapTeleport",
            _                 => "Hint.Flag",
        })));

        SetTooltip(string.Join("\n", lines));
    }

    private void OnPopupOpened()
    {
        _menuSignature = string.Empty;
        _nextLabels    = DateTime.MinValue;
        UpdateMenu();
    }

    private void UpdateMenu()
    {
        int max = GetConfigValue<int>("MaxEntries");

        var sections = new (Section Section, List<VistaWindow> Items)[] {
            (Section.Now, _now), (Section.Soon, _soon), (Section.Always, _always),
        };

        string signature = string.Join(";", sections.Select(s =>
            $"{s.Section}:{string.Join(",", s.Items.Take(max).Select(w => w.Vista.RowId))}:{s.Items.Count}"));

        if (signature != _menuSignature) {
            _menuSignature = signature;
            BuildMenu(sections, max);
        }

        // Mise à jour des comptes à rebours sans reconstruire le menu.
        foreach (var (section, items) in sections) {
            foreach (var w in items.Take(max)) {
                if (!_buttons.TryGetValue(ButtonId(section, w.Vista), out var button)) continue;

                string emote = w.Vista.Emote;
                string time  = section switch {
                    Section.Now when w.ClosesAt.HasValue => Strings.T("ClosesIn", Eorzea.FormatDuration(Eorzea.SecondsUntil(w.ClosesAt.Value))),
                    Section.Soon                         => Strings.T("OpensIn", Eorzea.FormatDuration(Eorzea.SecondsUntil(w.OpensAt))),
                    _                                    => "",
                };

                button.AltText = string.IsNullOrEmpty(time) ? emote
                    : string.IsNullOrEmpty(emote) ? time
                    : $"{emote}  ·  {time}";
            }
        }
    }

    private void BuildMenu((Section Section, List<VistaWindow> Items)[] sections, int max)
    {
        ClearMenu();

        int sort = 0;
        foreach (var (section, items) in sections) {
            if (section != Section.Now && items.Count == 0) continue;

            var group = new MenuPopup.Group(SectionLabel(section)) { SortIndex = sort++ };
            _groups[section] = group;
            Popup.Add(group);

            if (items.Count == 0) {
                group.Add(new MenuPopup.Button(Strings.T("NothingNow")) { Id = "Empty", IsDisabled = true });
                continue;
            }

            int index = 0;
            foreach (var w in items.Take(max)) {
                var vista  = w.Vista;
                var button = new MenuPopup.Button($"{vista.Number:000}  {vista.Name}  ·  {vista.ZoneName}") {
                    Id                = ButtonId(section, vista),
                    SortIndex         = index++,
                    Icon              = LineIcon(w),
                    AltTextColor      = section switch {
                        Section.Now  => OpenColor,
                        Section.Soon => SoonColor,
                        _            => null,
                    },
                    ClosePopupOnClick = true,
                    OnClick           = () => PlaceFlag(vista),
                };

                _buttons[button.Id] = button;
                group.Add(button);
            }

            if (items.Count > max) {
                group.Add(new MenuPopup.Button(Strings.T("More", items.Count - max)) {
                    Id = $"More_{section}", SortIndex = index, IsDisabled = true,
                });
            }
        }
    }

    private string SectionLabel(Section section) => section switch {
        Section.Now  => Strings.T("Section.Now"),
        Section.Soon => Strings.T("Section.Soon", Eorzea.FormatMinutes(GetConfigValue<int>("SoonMinutes"))),
        _            => Strings.T("Section.Always"),
    };

    /// <summary>
    /// Icône de la ligne : la météo requise (celle qui sera en cours pendant la fenêtre),
    /// sinon une horloge pour une vista à condition horaire seule, sinon un repère.
    /// </summary>
    private static object LineIcon(VistaWindow w)
    {
        if (w.WeatherId != 0
            && Framework.Service<IDataManager>().GetExcelSheet<Weather>().TryGetRow(w.WeatherId, out var weather)
            && weather.Icon > 0) {
            return (uint)weather.Icon;
        }

        return w.Vista.HasTime ? FontAwesomeIcon.Clock : FontAwesomeIcon.MapMarkerAlt;
    }

    private static string ButtonId(Section section, Vista vista) => $"{section}_{vista.RowId}";

    private void ClearMenu()
    {
        Popup.Clear(true);
        _groups.Clear();
        _buttons.Clear();
    }

    // ----------------------------------------------------------------- Actions

    private void PlaceFlag(Vista vista)
    {
        if (vista.MapId == 0) return;

        string action = GetConfigValue<string>("ClickAction");

        Framework.DalamudFramework.Run(() => {
            if (action is "FlagMap" or "FlagMapTeleport") {
                // OpenMapWithMapLink pose le drapeau ET ouvre la carte.
                Framework.Service<IGameGui>().OpenMapWithMapLink(vista.TerritoryId, vista.MapId, vista.WorldPosition);

                if (action == "FlagMapTeleport") TeleportToNearestAetheryte(vista);
                return;
            }

            AgentMap* map = AgentMap.Instance();
            if (map == null) return;

            map->SetFlagMapMarker(vista.TerritoryId, vista.MapId, vista.WorldPosition);

            Framework.Service<IChatGui>().Print(
                Strings.T("FlagPlaced", vista.Number, vista.Name, vista.ZoneName),
                Strings.T("ChatSource")
            );
        });
    }

    /// <summary>
    /// Téléporte à l'éthérite harmonisé le plus proche de la vista sur sa carte
    /// (même approche que UmbraTimedNodes). Sans éthérite sur la carte, on se rabat
    /// sur l'éthérite du territoire.
    /// </summary>
    private static void TeleportToNearestAetheryte(Vista vista)
    {
        var toast  = Framework.Service<IToastGui>();
        var player = Framework.Service<IPlayer>();

        if (!player.CanUseTeleportAction) {
            toast.ShowError(Strings.T("CannotTeleport"));
            return;
        }

        IZone zone;
        try {
            zone = Framework.Service<IZoneManager>().GetZone(vista.MapId);
        } catch (Exception e) {
            Logger.Error($"[Vistas] Zone introuvable pour la carte {vista.MapId} : {e.Message}");
            toast.ShowError(Strings.T("ZoneNotFound"));
            return;
        }

        // Éthérites principaux déjà harmonisés.
        var unlocked = Framework.Service<IAetheryteList>().Where(a => a.SubIndex == 0).ToList();
        var target   = new Vector2(vista.WorldPosition.X, vista.WorldPosition.Z);

        var best = zone.StaticMarkers
            .Where(m => m.Type == ZoneMarkerType.Aetheryte)
            .Select(m => (Marker: m, Entry: unlocked.FirstOrDefault(a => a.AetheryteId == m.DataId)))
            .Where(x => x.Entry != null)
            .OrderBy(x => Vector2.Distance(new(x.Marker.WorldPosition.X, x.Marker.WorldPosition.Z), target))
            .FirstOrDefault();

        if (best.Entry == null && vista.FallbackAetheryteId != 0) {
            best = (default, unlocked.FirstOrDefault(a => a.AetheryteId == vista.FallbackAetheryteId));
        }

        if (best.Entry == null) {
            toast.ShowError(Strings.T("NoAetheryte"));
            return;
        }

        string name = best.Entry.AetheryteData.ValueNullable?.PlaceName.ValueNullable?.Name.ExtractText() ?? Strings.T("TheAetheryte");

        if (Telepo.Instance()->Teleport(best.Entry.AetheryteId, (byte)best.Entry.SubIndex)) {
            Framework.Service<IChatGui>().Print(
                Strings.T("Teleporting", name, best.Entry.GilCost, vista.Number, vista.Name),
                Strings.T("ChatSource")
            );
        }
    }
}
