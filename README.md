# HaselTweaks

Just some tweaks I wrote. :)

Open config with `/haseltweaks`.

Repo for auto-updates:  
https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

## Tweaks

### Aether Current Helper 

Click on a zone in the Aether Currents window to open up a helper window that gives you details on how to unlock them. Clicking on an aether current in the list opens the map with a flag to the position.

### Auto Sort Armoury Chest 

Sorts items inside the Armoury Chest upon opening it.

Uses the games `/itemsort` command for sorting. Condition and order are configurable.

### Character Class Switcher 

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

### Commands 

A variety of useful chat commands. Each command is separately toggleable.

Available Commands:

- `/itemlink <id>`  
  Prints an item link for the given item id in chat.
- `/whatmount`  
  Target a player and execute the command to see what mount your target is riding and which item teaches this mount.

### Custom Chat Timestamp 

As it says, configurable chat timestamp format. Uses C#'s <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings" target="_blank" rel="noreferrer noopener">`DateTime.ToString()`</a> to format.

### DTR 

Shows Instance, FPS and Busy status in DTR bar. Use Dalamud Settings to enable/disable or to change order.

### Enhanced Experience Bar 

The experience bar shows different level/experience based on your location.

- The PvP Season Bar shows season rank and experience. A little * after the rank indicates a claimable rank-up reward.
- The Sanctuary Bar shows sanctuary level and island experience.

> **Note**
> In order for this tweak to work properly, please make sure to disable "Show Experience Percentage" in Simple Tweaks first.

### Expert Deliveries 

Always opens the "Grand Company Delivery Missions" window on the "Expert Delivery" tab.

### Forced Cutscene Music 

Auto-unmutes background music for cutscenes.

### Hide MSQ Complete 

Hides the Main Scenario Guide when you've completed the MSQ. Job quests are still being displayed.

### Keep Screen Awake 

Prevents the screen from going into standby.

### Minimap Adjustments 

Configuration options:

- Square Collision (for custom minimap mask textures)
- Default Opacity
- Hover Opacity
- Hide Coordinates (only visible on hover)
- Hide Weather (only visible on hover)

### Portrait Helper

Adds Copy/Paste buttons to the "Edit Portrait" window, so you can copy the settings and then paste it to another one.

### Refresh Material List 

Refreshes the material list and recipe tree when you've crafted or gathered an item.

### Requisite Materials 

Always opens the Island Sanctuarys "Requisite Materials" window on the "Current & Next Season" tab.

### Reveal Duty Requirements 

Reveals duty names in duty finder, which were shown as "???" to prevent spoilers. Useful for unlocking Mentor roulette.

### Scrollable Tabs 

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

### Search the markets 

Adds a context menu entry to items in Inventory, Crafting Log, Recipe Tree or Materials List to quickly search for it on the Market Board. Only visible when Market Board is open.
