# Umbra – Vistas

[Français](README.md) · **English**

A toolbar widget for [Umbra](https://github.com/una-xiv/umbra) (FINAL FANTASY XIV, via Dalamud) that tells you which sightseeing log vistas you can go and do **right now**.

## Features

- **In the toolbar:** the number of vistas you have not done yet whose conditions (Eorzea time and weather) are met right now. Optionally, your overall progress (e.g. 74/80).
- **Tooltip:** your progress per expansion.
- **Click:** a list in sections.
  - **Available now:** open vistas, sorted by time left before they close (end of the time window or weather change, whichever comes first).
  - **Soon:** vistas opening within the chosen delay (1 real hour by default), with the time until they open.
  - **No conditions:** vistas with no time or weather requirement (nearly all of them after A Realm Reborn). Optionally, they can also count as available, or be hidden.
- **Each row:** the number, name, zone, emote to perform and the icon of the required weather. A clock marks a vista with a time requirement only.
- **Click a vista:** places the flag on the map at its location. Optionally, opens the map as well, or opens the map and teleports you to the attuned aetheryte closest to the vista.
- **Option:** hide expansions you have not unlocked yet. An expansion counts as unlocked as soon as one of its aetherytes is attuned.

Everything comes from the game data: completed vistas, time windows, weather. The weather is computed locally, with no network calls.

A Realm Reborn vistas 21 to 80 only unlock after the first 20. Until then, the widget does not list them and reminds you in the tooltip.

The widget complements the 3D vista markers Umbra already shows in the world: it does not replace them.

The widget's interface, name included, follows Umbra's language: French when Umbra is in French, English otherwise. Vista, zone, expansion and emote names follow your client's language.

## Installation

1. In game, open **Umbra settings → Plugins**.
2. Add a GitHub repository: owner `Soratix`, repository `UmbraVistas`.
3. Switch the toolbar to edit mode and add the **Vistas** widget (« Exploration » when Umbra is in French). You can also search for `vista`, `sightseeing` or `log`.

## Building it yourself

You need the .NET 10 SDK and XIVLauncher with Umbra installed: the references point to `%AppData%\XIVLauncher`.

```powershell
dotnet build -c Release Vistas\Vistas.csproj
```

## Credits

The weather required by each vista is not part of the game data. The table comes from Umbra (`VistaMarkerFactory`, AGPL-3.0); it is identical to the one in [Tourist](https://github.com/foophoof/Tourist).

## License

AGPL-3.0, like Umbra.
