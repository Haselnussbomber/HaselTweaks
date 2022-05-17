# HaselTweaks

Just some tweaks I wrote. :)

Repo for auto-updates:

https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

## Tweaks

### Auto Sort Armoury Chest [[src](Tweaks/AutoSortArmouryChest.cs)]

Sorts items inside the Armoury Chest upon opening it.

Uses the <a href="https://eu.finalfantasyxiv.com/lodestone/playguide/db/text_command/a3840353abb/" target="_blank" rel="noreferrer noopener">`/itemsort`</a> command for sorting. Condition and order are configurable.

### Character Class Switcher [[src](Tweaks/CharacterClassSwitcher.cs)]

Clicking on a class/job in the character window finds the gearset with the highest item level and equips it. Hold shift on crafters to open the original desynthesis window.

Basic controller support - only tested with a Xbox 360 Controller.

### Commands [[src](Tweaks/Commands.cs)]

A variety of useful chat commands. Each command is separately toggleable.

Available Commands:

- `/itemlink <id>`  
  Prints an item link for the given item id in chat.
- `/whatmount`  
  Target a player and execute the command to see what mount your target is riding and which item teaches this mount.

### Custom Chat Timestamp [[src](Tweaks/CustomChatTimestamp.cs)]

As it says, configurable chat timestamp format. Uses C#'s <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings" target="_blank" rel="noreferrer noopener">`DateTime.ToString()`</a> to format.

### DTR [[src](Tweaks/DTR.cs)]

Shows Instance, FPS and Busy status in DTR bar. Use Dalamud Settings to enable/disable or to change order.

### Expert Deliveries [[src](Tweaks/ExpertDeliveries.cs)]

Always opens the Grand Company Delivery Missions window on the third tab (Expert Delivery).

### Forced Cutscene Music [[src](Tweaks/ForcedCutsceneMusic.cs)]

Auto-unmutes background music for cutscenes.

### Hide MSQ Complete [[src](Tweaks/HideMSQComplete.cs)]

Hides the Main Scenario Guide when you've completed the MSQ. Job quests are still being displayed.

### Keep Instant Portrait [[src](Tweaks/KeepInstantPortrait.cs)]

Prevents Instant Portrait from being reset upon saving/updating the current gearset.

This is for everyone who extracts materia from their fully spiritbond items and has the urge to click the update gearset button, because it is clickable after that.  
Updating the gearset unfortunately removes the instant portrait link from it, regardless of whether the gear has actually changed or not, which is pretty annoying.  
This tweak simply completely skips the code which removes the link.

The real solution would probably be that the gearset won't be marked as changed when spiritbond of an item is reset. That way the update gearset button wouldn't even activate.  
Hopefully this is fixed in a future game update.

### Keep Screen Awake [[src](Tweaks/KeepScreenAwake.cs)]

Prevents the screen from going into standby.

### Minimap Adjustments [[src](Tweaks/MinimapAdjustments.cs)]

Configuration options:

- Square Collision (for custom minimap mask textures)
- Default Opacity
- Hover Opacity
- Hide Coordinates (only visible on hover)
- Hide Weather (only visible on hover)

### Refresh Material List [[src](Tweaks/RefreshMaterialList.cs)]

Refreshes the material list and recipe tree when you've crafted or gathered an item.

### Reveal Duty Requirements [[src](Tweaks/RevealDutyRequirements.cs)]

Reveals duty names in duty finder, which were shown as "???" to prevent spoilers. Useful for unlocking Mentor roulette.

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
