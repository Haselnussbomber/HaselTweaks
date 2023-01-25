# Changelog

### [Unreleased]

### DTR

- **Fixed:** DTRs without data will be hidden again.

### Enhanced Experience Bar

- **Fixed:** Messed up signature for PvP Stats.

### [0.8.1] (2023-01-25)

### DTR

- **Fixed:** FPS should properly appear again.

### [0.8.0] (2023-01-25)

Some internal restructuring and performance optimizations.

### New Tweak: Auto Sort Inventory

Just like "Auto Sort Armoury Chest", but for inventory. ([PR](https://github.com/Haselnussbomber/HaselTweaks/pull/22) by [53m1k0l0n](https://github.com/53m1k0l0n). Thanks!)

### [0.7.14] (2023-01-23)

### Refresh Material List

- **Fixed:** Possible crash fix.

### Scrollable Tabs

- **Fixed:** Minions and Mounts windows should be able to scroll out of favorites again.
- **Changed:** The plugin now uses the game's UI collision system to detect which window is being hovered instead of finding a match based on cursor and window position.

### [0.7.13] (2023-01-20)

### Search the markets

- **Fixed:** Searching via Recipe Tree or Materials List doesn't crash the game anymore.

### [0.7.12] (2023-01-19)

### Scrollable Tabs

- **Added:** Support for Island Minion Guide window.

### [0.7.11] (2023-01-14)

### Enhanced Experience Bar

- **Fixed:** The Experience Bar should now update as intended.

### [0.7.10] (2023-01-14)

### Enhanced Experience Bar

- **Fixed:** Signature update, so PvP Series Bar works again.

### [0.7.9] (2023-01-13)

Additional code to automatically re-scan cached hotfix adresses.

### [0.7.8] (2023-01-13)

Preliminary update for Patch 6.3 Hotfix 1.  
Fixed some internal bugs introduced with the last version.

### [0.7.7] (2023-01-12)

Preliminary update for Patch 6.3.

### Enhanced Experience Bar

- **Changed:** Corrects the name of "PvP Season Bar" to "PvP Series Bar".
  - Configuration is automatically updated.

### [0.7.6] (2022-12-09)

### Aether Current Helper

- **Added:** A new config option to disable centering of the distance column, which *might* help in case the window keeps expanding endlessly to the right.
- **Fixed:** Corrects the Compass Directions (East/West was swapped).

### [0.7.5] (2022-11-03)

### Aether Current Helper

- **Fixed:** Corrects the Dravanian Forelands quest ids.

### [0.7.4] (2022-10-06)

### Aether Current Helper

- **Fixed:** Some AetherCurrent entries link to the wrong quest. Added 3 of 5 as special case in the plugin. (see issue #15)

### [0.7.3] (2022-09-17)

### Aether Current Helper

- **Fixed:** Window expanding infinitely.

### [0.7.2] (2022-09-04)

- Improved Tweak Descriptions

### Renamed Tweak: Requisite Materials -> Material Allocation

I play with the german client and picked the wrong window title from the games sheets. 😳 Sorry for the confusion.

## [0.7.1] (2022-09-02)

### Enhanced Experience Bar

- **Fixed:** Island Sanctuary experience broke in last update. It's fixed now.

## [0.7.0] (2022-09-02)

- **Fixed:** The layout shift upon opening the configuration window is now fixed due to the font being loaded up-front.

### New Tweak: Portrait Helper

Adds Copy/Paste buttons to the "Edit Portrait" window, so you can copy the settings and then paste it to another one.

## [0.6.0] (2022-08-30)

- **Changed:** The configuration window has been reworked.

### Reworked Tweak: Series Exp Bar -> Enhanced Experience Bar

- **Added:** Sanctuary Bar for the new Island, because why not?
- **Added:** Max Level Override setting.
  - Will switch to the selected bar if your current job is on max level and none of the other settings apply.
  - *Note:* Sanctuary Bar is not available as Max Level Override, because data is only loaded once you travel to the island. PvP Series data is always available.
- **Added:** Option to disable color change.

### New Tweak: Requisite Materials

Always opens the Island Sanctuarys "Requisite Materials" window on the "Current & Next Season" tab.

### Aether Current Helper

- **Added:** An option to always show distance (if in same zone) despite being unlocked.

## [0.5.1] (2022-08-24)

### Scrollable Tabs

- **Fixed:** Scrolling state is now reset each frame.

## [0.5.0] (2022-08-24)

Updated for Patch 6.2.

### Added Tweak: Aether Current Helper

Click on a zone in the Aether Currents window to open up a helper window that gives you details on how to unlock them. Clicking on an aether current in the list opens the map with a flag to the position.

### Removed Tweak: Keep Instant Portrait

Obsolete due to changes to the portrait system in Patch 6.2.

## [0.4.1] (2022-08-23)

Preliminary update for Patch 6.2.

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


[Unreleased]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.8.1...HEAD
[0.8.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.8.0...v0.8.1
[0.8.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.14...v0.8.0
[0.7.14]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.13...v0.7.14
[0.7.13]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.12...v0.7.13
[0.7.12]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.11...v0.7.12
[0.7.11]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.10...v0.7.11
[0.7.10]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.9...v0.7.10
[0.7.9]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.8...v0.7.9
[0.7.8]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.7...v0.7.8
[0.7.7]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.6...v0.7.7
[0.7.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.5...v0.7.6
[0.7.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.4...v0.7.5
[0.7.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.3...v0.7.4
[0.7.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.2...v0.7.3
[0.7.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.1...v0.7.2
[0.7.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.7.0...v0.7.1
[0.7.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.5.1...v0.6.0
[0.5.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.4.1...v0.5.0
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
