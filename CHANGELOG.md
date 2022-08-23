# Changelog

## [Unreleased]

### Keep Instant Portrait

- **Tweak Removed:** Obsolete due to changes to the portrait system in Patch 6.2.

### Search the markets

- Marked as outdated, because Dalamud.ContextMenu broke

## [0.4.1] (2022-08-23)

- Preliminary update for Patch 6.2

## [0.4.0] (2022-08-20)

### New Tweak: Series Exp Bar

The experience bar shows series rank and experience instead. A little * after the rank indicates a claimable reward.

> **Note**
> In order for this tweak to work properly, please make sure to disable "Show Experience Percentage" in Simple Tweaks first.

## [0.3.3] (2022-08-13)

- **Added:** A proper changelog! :D Releases will share the same format.

### Scrollable Tabs
- **Added:** Support for the Bozjan Field Records window.
- **Added:** Support for scrolling in and out of the favorites tab in the Mounts and Minions windows.
- **Added:** Support for scrolling between the normal sized Inventory and the Key Items window.
  - While at it, scrolling through tabs in Key Items window has been added, but not tested. Sorry for not having over 35 key items handy to have a second tab. :P
- **Changed:** On fresh installations the Inventory option is now enabled by default.
- **Fixed:** Scrolling too fast doesn't skip tabs anymore. Don't ask.

## [0.3.2] (2022-08-10)

- **Workaround:** When the config file can't be read or parsed, the plugin would crash on load. Now, the plugin will just create a new config (RIP your settings) and continue loading. It will show an dalamud error notification for 5 seconds in the corner of your screen whenever that happens.

## [0.3.1] (2022-08-08)

- **Added:** The Dalamud Plugin Installer now has a button to visit the plugins GitHub repository.
- **Added:** The repository now has a sponsor link to my [Ko-Fi](https://ko-fi.com/haselnussbomber) page for people with too much money. ;)

### DTR

- **Fixed:** Busy status text should now properly appear.

## [0.3.0] (2022-08-05)

### New Tweak: Search the markets

Adds a context menu entry to items in Inventory, Crafting Log, Recipe Tree or Materials List to quickly search for it on the Market Board. Only visible when Market Board is open.

## [0.2.6] (2022-07-11)

### Character Class Switcher

- **Added:** Support for PvP Character window.

## [0.2.5] (2022-07-09)

### Character Class Switcher

- **Added:** Controller support now reads "Select Target/Confirm" button binding.

## [0.2.4] (2022-06-21)

- **Fixed:** Adjusted code to accommodate FFXIVClientStructs breaking changes.

## [0.2.3] (2022-06-20)

### Minimap Adjustments

- **Fixed:** Square Collision flags are now checked each frame instead just on enabling the tweak.

## [0.2.2] (2022-05-12)

### Keep Instant Portrait

- **Hotfixed:** A for a copy paste mistake that crashes the game. Sorry.

## [0.2.1] (2022-05-12)

> **Warning**
> **Please update to v0.2.2.** Under-the-hood changes make Keep Instant Portrait crash the game due to a copy paste error.

### Character Class Switcher

- **Added:** A new config option to disable tooltips.

## [0.2.0] (2022-05-10)

- **Added:** A tweak is now marked as outdated if the signature fails.
- **Changed:** Tweak tooltip in the config window has been enhanced.

### New "Tweak": Commands

A variety of useful chat commands.

Starting with:

- `/itemlink <id>`  
  Prints an item link for the given item id in chat.
- `/whatmount`  
  Target a player and execute the command to see what mount your target is riding and which item teaches this mount.

### DTR

- **Changed:** Entries are now created on load (instead on setup) and removed on unload (instead on dispose).

## [0.1.2] (2022-05-05)

- Under-the-hood changes featuring a new auto-hook system.

### Tweak removed: Wondrous Tails Duty Selector

Use ezWondrousTails instead, as it does the same.

## [0.1.1] (2022-05-04)

- **Added:** Auto-adjust renamed tweaks in config.
- Renamed RevealDungeonRequirements to RevealDutyRequirements 🙄

## [0.1.0] (2022-05-04)

### New Tweak: ScrollableTabs

Enables mouse wheel to switch tabs (like with LB/RB on controllers) in the following windows:

- Aether Currents
- Armoury Chest
- Blue Magic Spellbook
- Fashion Accessories
- Fish Guide
- Gold Saucer -> Card List
- Gold Saucer -> Decks -> Edit Deck
- Gold Saucer -> Lord of Verminion -> Minion Hotbar
- Inventory
- Minions
- Mounts
- Retainer Inventory
- Shared FATE
- Sightseeing Log

## [0.0.11] (2022-04-30)

### Character Class Switcher

- **Added:** Basic controller support!
  - Only tested with a Xbox 360 Controller.
  - Desynthesis window still only opens when holding shift on a keyboard!
- **Added:** Clicking on a non-crafter job now plays UI clicking sound.
- **Changed:** Hover/pointer effect now shows only for unlocked jobs.

## [0.0.10] (2022-04-28)

### Character Class Switcher

- **Added:** Hovering over non-crafters now shows the same effect as on crafters.
- **Fixed:** Find gearsets with an average itemlevel of 0.

## [0.0.9] (2022-04-27)

### Character Class Switcher

- **Added:** Support for crafters and gatherers!

## [0.0.8] (2022-04-26)

- Reworked Forced Cutscene Music.
- Bugfixes.

## [0.0.7] (2022-04-25)

- Renamed KeepInstantProfile to KeepInstantPortrait 🙄

## [0.0.6] (2022-04-24)

### Forced Cutscene Music

- **Fixed:** Disable for bed scene on login/logout.

## [0.0.5] (2022-04-24)

### New Tweak: Forced Cutscene Music

Auto-unmutes background music for cutscenes.

## [0.0.4] (2022-04-24)

- **Added:** Labels for configuration options.
- Renamed NaviMapOpacity to MinimapAdjustments

## [0.0.3] (2022-04-24)

### Navi Map Opacity

- **Added:** Option to change collision box into a square.
- **Added:** Option for default opacity.
- **Added:** Option for mouseover opacity.
- **Added:** Option to hide coordinations.
- **Added:** Option to hide weather.

## [0.0.2] (2022-04-23)

No plugin changes. Just added CI to automatically push updates to [my dalamud plugins repo](https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json). :)

## [0.0.1] (2022-04-23)

First release! 🥳

Starting with the following Tweaks:

###  Auto Sort Armoury Chest

Automatically runs the shared macro in the third slot when the armoury is opened. Use `/isort` in the macro.

###  Character Class Switcher

Always equips the gearset with the highest average item level.

###  Chat Timestamp Fixer

At least in the german client the game uses the format `[H:mm]` and it bugged me to have a single digit in the early mornings, so I changed it to `[HH:mm]` and added a space afterwards for better visibility.

###  DTR

Shows Instance, FPS and Busy status in DTR bar. Use Dalamud Settings to enable/disable or to change order.

### Expert Deliveries

Always opens the Grand Company Delivery Missions window on the third tab (Expert Delivery).

### Hide MSQ Complete

Hides the Main Scenario Guide when you've completed the MSQ. Job quests are still being displayed.

### Keep Instant Profile

Prevents Instant Profile from being reset upon saving/updating the current gearset.

### Keep Screen Awake

Prevents the screen from going into standby.

### Navi Map Opacity

When not hovering over the minimap this tweak applies an opacity of 80%. Also makes its collision box square, for a custom texture I use.

### Refresh Material List

Refreshes the material list and recipe tree when you've crafted or gathered an item.

### Wondrous Tails Duty Selector

Opens duty finder for the duty you clicked on in the Wondrous Tails Journal.


[Unreleased]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.4.1...HEAD
[0.4.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.3.3...v0.4.0
[0.3.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.3.2...v0.3.3
[0.3.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.6...v0.3.0
[0.2.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.4...v0.2.5
[0.2.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.3...v0.2.4
[0.2.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.1.2...v0.2.0
[0.1.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.11...v0.1.0
[0.0.11]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.10...v0.0.11
[0.0.10]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.9...v0.0.10
[0.0.9]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.8...v0.0.9
[0.0.8]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.7...v0.0.8
[0.0.7]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.6...v0.0.7
[0.0.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.5...v0.0.6
[0.0.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.4...v0.0.5
[0.0.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.3...v0.0.4
[0.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.2...v0.0.3
[0.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/Haselnussbomber/HaselTweaks/commit/7ba7c3ab
