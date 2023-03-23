<p align="center">
  <img src="https://user-images.githubusercontent.com/96642047/220611506-da811a05-6c84-41da-89b3-498f5450db84.svg" width="580px" alt="HaselTweaks">
</p>

Just some tweaks I wrote. :)

Open config with `/haseltweaks`.

Repo for auto-updates:  
https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

## Tweaks

### Aether Current Helper

Clicking on a zone in the Aether Currents window opens a helper window that shows where to find the aether currents or which quests unlocks them. Clicking on an aether current in the list flags the position of the aether current or the quest giver on the map.

### Auto Sorter

Sorts items inside various containers when they are opened. Rules can be configured.

Uses the games `/itemsort` command for sorting.

### Auto-open Recipe

When a new daily/tribal quest objective requires you to craft an item and you have all materials for it in your inventory at that moment, this tweak will automatically open the recipe.

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
> In order for this tweak to work properly, please make sure "Character Window Job Switcher" is disabled in Simple Tweaks.

### Commands

A variety of useful chat commands. Each command is separately toggleable.

Available Commands:

- `/itemlink <id>`
  - Prints an item link for the given item id in chat.
- `/whatmount`
  - Target a player and execute the command to see what mount your target is riding and which item teaches this mount.

### Custom Chat Timestamp

As it says, configurable chat timestamp format. Uses C#'s <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings" target="_blank" rel="noreferrer noopener">`DateTime.ToString()`</a> to format.

### DTR

Shows Instance number (only if the current zone is instanced), FPS and Busy status in DTR bar. To enable/disable elements or to change the order go into Dalamud Settings > Server Info Bar.

### Enhanced Experience Bar

Enhances the Experience Bar with the following modes:

- The PvP Series Bar shows series rank and experience. A little \* after the rank indicates a claimable rank-up reward.
- The Sanctuary Bar shows sanctuary level and island experience.

Available options include:

- Always show PvP Series Bar in PvP Areas
- Always show Sanctuary Bar on the Island
- Hide Job on Sanctuary Bar
- Max Level Override
  - Will switch to the selected bar if your current job is on max level and none of the settings above apply.
- Disable color change

> **Note**
> In order for this tweak to work properly, please make sure "Show Experience Percentage" is disabled in Simple Tweaks.

### Enhanced Material List

Enhances the Material List (and Recipe Tree) with the following options:

- Enable Zone Names: Displays a zone name underneath the item name indicating where it can be gathered. Only the zone with the lowest teleportation cost is displayed. If the name is green it means it's the current zone. Since space is limited it has to shorten the item and zone name.
  - An option is available to disable this for Crystals.
- Enable click to open Map: Allows you to open the map with the gathering marker in said zone.
  - An option is available to disable this for Crystals.
- Auto-refresh Material List/Recipe Tree: Refreshes the material list and/or recipe tree after an item has been bought, crafted, fished or gathered.
- Restore Material List on Login: The material list will reopen with the same recipe and quantity each time you log in as long as the window is locked.
- Add "Search for Item by Crafting Method" context menu entry: No more need to open the recipe tree first.

### Expert Deliveries

Always opens the "Grand Company Delivery Missions" window on the "Expert Delivery" tab.

### Forced Cutscene Music

Auto-unmutes background music for most cutscenes.

I've added this since `/bgm` command doesn't work during cutscenes anymore and I usually play with background music muted.

### Hide MSQ Complete

Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed.

### Keep Screen Awake

Prevents the screen from going into standby. Alternatively one can use [PowerToys Awake](https://docs.microsoft.com/windows/powertoys/).

### Lock Window Position

Lock window positions so they can't move.

Adds a context menu entry for the title bar to "Lock/Unlock Position" (can be disabled).  
Alternatively it's possible to add windows by using the window picker in the configuration.

### Material Allocation

Enhances the Island Sanctuarys "Material Allocation" window.

- Saves the last selected tab between game sessions.
- Clicking on a gatherable item opens the Sanctuary Gathering Log with that item selected.

### Minimap Adjustments

Configuration options:

- Square Collision (for custom minimap mask textures)
- Default Opacity
- Hover Opacity
- Hide Coordinates (only visible on hover)
- Hide Weather (only visible on hover)

### Portrait Helper

Adds Copy/Paste buttons to the "Edit Portrait" window, which allow settings to be copied to other portraits.

The Advanced Mode allows to specify which settings should be pasted.

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
- Island Minion Guide
- Minions
- Mounts
- Retainer Inventory
- Shared FATE
- Sightseeing Log

### Search the markets

Adds a context menu entry to items in Chat, Crafting Log, Inventory, Materials List and Recipe Tree to quickly search for the item on the Market Board. Only visible when Market Board is open.
