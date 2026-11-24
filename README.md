# Umbra – Exploration

**Français** · [English](README.en.md)

Plugin pour [Umbra](https://github.com/una-xiv/umbra) (FINAL FANTASY XIV, via Dalamud) qui ajoute un widget de barre d'outils pour savoir quelles vistas du carnet d'exploration aller faire **maintenant**.

## Fonctionnalités

- **Dans la barre :** le nombre de vistas pas encore faites dont les conditions (heure et météo d'Eorzéa) sont remplies maintenant. En option, la progression globale (ex. 74/80).
- **Infobulle :** la progression par extension.
- **Clic :** une liste en sections.
  - **Disponibles maintenant :** les vistas ouvertes, triées par temps restant avant fermeture (fin du créneau horaire ou changement de météo, selon ce qui arrive en premier).
  - **Bientôt :** les vistas qui ouvriront dans le délai choisi (1 h réelle par défaut), avec le temps avant ouverture.
  - **Sans condition :** les vistas sans heure ni météo requise (presque toutes celles d'après A Realm Reborn). En option, elles peuvent aussi compter comme disponibles ou être masquées.
- **Chaque ligne :** le numéro, le nom, la zone, l'emote à faire et l'icône de la météo requise. Une horloge signale une vista avec une condition horaire seulement.
- **Clic sur une vista :** pose le drapeau sur la carte à son emplacement. En option, ouvre aussi la carte, ou ouvre la carte et téléporte à l'éthérite harmonisé le plus proche de la vista.
- **Option :** masquer les extensions pas encore débloquées. Une extension compte comme débloquée dès qu'un de ses éthérites est harmonisé.

Tout vient des données du jeu : vistas faites, horaires, météo. La météo est calculée localement, sans aucun appel réseau.

Les vistas 21 à 80 d'A Realm Reborn ne se débloquent qu'après les 20 premières. Tant que ce n'est pas fait, le widget ne les propose pas et le rappelle dans l'infobulle.

Le widget complète les marqueurs 3D de vistas qu'Umbra affiche déjà dans le monde : il ne les remplace pas.

L'interface du widget, nom compris, suit la langue d'Umbra : en français si Umbra est en français, en anglais sinon. Les noms des vistas, des zones, des extensions et des emotes suivent la langue du client.

## Installation

1. Dans le jeu, ouvrez les **paramètres d'Umbra → Plugins**.
2. Ajoutez un dépôt GitHub : propriétaire `Soratix`, dépôt `UmbraVistas`.
3. Passez la barre d'outils en mode édition et ajoutez le widget **« Exploration »** (« Vistas » si Umbra n'est pas en français). Dans la recherche, vous pouvez aussi taper `exploration`, `vista` ou `carnet`.

## Compiler soi-même

Il faut le SDK .NET 10 et XIVLauncher avec Umbra installé : les références pointent vers `%AppData%\XIVLauncher`.

```powershell
dotnet build -c Release Vistas\Vistas.csproj
```

## Crédits

Les météos requises par les vistas ne figurent pas dans les données du jeu. La table vient d'Umbra (`VistaMarkerFactory`, AGPL-3.0) ; elle est identique à celle de [Tourist](https://github.com/foophoof/Tourist).

## Licence

AGPL-3.0, comme Umbra.
