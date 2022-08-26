# HaselTweaks

Just some tweaks I wrote. :)

Open config with `/haseltweaks`.

Repo for auto-updates:

https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

## Tweaks

### Aether Current Helper [[src](Tweaks/AetherCurrentHelper.cs)]

Click on a zone in the Aether Currents window to open up a helper window that gives you details on how to unlock them. Clicking on an aether current in the list opens the map with a flag to the position.

### Auto Sort Armoury Chest [[src](Tweaks/AutoSortArmouryChest.cs)]

Sorts items inside the Armoury Chest upon opening it.

Uses the games `/itemsort` command for sorting. Condition and order are configurable.

### Character Class Switcher [[src](Tweaks/CharacterClassSwitcher.cs)]

Clicking on a class/job in the character window finds the matching gearset and equips it.

Features:

- Always equips the matching gearset with the highest average item level
- Supports crafter jobs
  - Hold shift to open the original desynthesis window if needed
- Supports controller input
  - Checks the controller button with "Select Target/Confirm" binding
  - Technical limitation: desynthesis window still only opens when holding shift on a keyboard
- Supports PvP Character window
- Adds hover effect for non-crafters
- Option to disable Tooltips

> **Note**
> In order for this tweak to work properly, please make sure to disable \"Character Window Job Switcher\" in Simple Tweaks first.

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

### Enhanced Experience Bar [[src](Tweaks/EnhancedExpBar.cs)]

The experience bar shows different level/experience based on your location.

- The PvP Season Bar shows season rank and experience. A little * after the rank indicates a claimable rank-up reward.
- The Sanctuary Bar shows sanctuary level and island experience.

> **Note**
> In order for this tweak to work properly, please make sure to disable "Show Experience Percentage" in Simple Tweaks first.

### Expert Deliveries [[src](Tweaks/ExpertDeliveries.cs)]

Always opens the Grand Company Delivery Missions window on the third tab (Expert Delivery).

### Forced Cutscene Music [[src](Tweaks/ForcedCutsceneMusic.cs)]

Auto-unmutes background music for cutscenes.

### Hide MSQ Complete [[src](Tweaks/HideMSQComplete.cs)]

Hides the Main Scenario Guide when you've completed the MSQ. Job quests are still being displayed.

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

### Search the markets [[src](Tweaks/SearchTheMarkets.cs)]

Adds a context menu entry to items in Inventory, Crafting Log, Recipe Tree or Materials List to quickly search for it on the Market Board. Only visible when Market Board is open.
