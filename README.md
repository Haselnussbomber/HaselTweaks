# HaselTweaks

Some tweaks I wrote for myself, but you may use them too. :)

My repo for auto-updates:

https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

## Tweaks

### Auto Sort Armoury Chest [[src](Tweaks/AutoSortArmouryChest.cs)]

Sorts items inside the Armoury Chest upon opening it.

Uses the <a href="https://eu.finalfantasyxiv.com/lodestone/playguide/db/text_command/a3840353abb/" target="_blank" rel="noreferrer noopener">`/itemsort`</a> command for sorting. Condition and order are configurable.

### Character Class Switcher [[src](Tweaks/CharacterClassSwitcher.cs)]

Clicking on a class/job in the character window finds the gearset with the highest item level and equips it. Hold shift on crafters to open the original desynthesis window.

Basic controller support - only tested with a Xbox 360 Controller.

### Custom Chat Timestamp [[src](Tweaks/CustomChatTimestamp.cs)]

As it says, configurable chat timestamp format. Uses C#'s <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings" target="_blank" rel="noreferrer noopener">`DateTime.ToString()`</a> to format.

### DTR [[src](Tweaks/DTR.cs)]

Shows Instance, FPS and Busy status in DTR bar. Use Dalamud Settings to enable/disable or to change order.

### Expert Deliveries [[src](Tweaks/ExpertDeliveries.cs)]

Opens the Grand Company Delivery Missions window on the third tab (Expert Delivery).

### Forced Cutscene Music [[src](Tweaks/ForcedCutsceneMusic.cs)]

Auto-unmutes background music for cutscenes.

### Hide MSQ Complete [[src](Tweaks/HideMSQComplete.cs)]

Hides the Main Scenario Guide when you've completed the MSQ. Job quests are still being displayed.

### Keep Instant Portrait [[src](Tweaks/KeepInstantPortrait.cs)]

Prevents Instant Portrait from being reset upon saving/updating the current gearset.

### Keep Screen Awake [[src](Tweaks/KeepScreenAwake.cs)]

Prevents the screen going into standby.

### Minimap Adjustments [[src](Tweaks/MinimapAdjustments.cs)]

Configuration options:

- Square Collision (for custom minimap mask textures)
- Default Opacity
- Hover Opacity
- Hide Coordinates (only visible on hover)
- Hide Weather (only visible on hover)

### Refresh Material List [[src](Tweaks/RefreshMaterialList.cs)]

Refreshes the material list and recipe tree when you've crafted or gathered an item.

### Reveal Dungeon Requirements [[src](Tweaks/RevealDungeonRequirements.cs)]

Reveals dungeon names in duty finder. Useful for unlocking Mentor roulette.

### Scrollable Tabs [[src](Tweaks/ScrollableTabs.cs)]

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

### Wondrous Tails Duty Selector [[src](Tweaks/WondrousTailsDutySelector.cs)]

Opens duty finder for the duty you clicked on in the Wondrous Tails Journal.
