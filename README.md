<p align="center">
  <img src="https://user-images.githubusercontent.com/96642047/220611506-da811a05-6c84-41da-89b3-498f5450db84.svg" width="580px" alt="HaselTweaks">
</p>

**HaselTweaks** is an all-in-one plugin for all my tweaks and helpers (well, with the exception of [LeveHelper](https://github.com/Haselnussbomber/LeveHelper) 😜).

You will not find this plugin in the official plugin repository.  
However, you're free to add my custom repository to get updates whenever I release a new version:  
https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

To get started, open the configuration with `/haseltweaks` and enable the tweaks you like.

## Tweaks

### Aether Current Helper

Clicking on a zone in the Aether Currents window opens a helper window that shows where to find the aether currents or which quests unlocks them. Clicking on an aether current in the list flags the position of the aether current or the quest giver on the map.

### Auto Sorter

Sorts items inside various containers when they are opened. Rules can be configured.

Uses the games `/itemsort` command for sorting.

Options:

- Sort armoury on job change

### Auto-open Recipe

If a new daily/tribal quest objective requires you to craft an item, and you have all the materials for it in your inventory at that moment, this tweak will automatically open the recipe, saving you a whopping 4-5 clicks.

### Background Music Keybind

Adds a configurable keybind to toggle the background music, in addition to the game's existing keybind option to toggle the sound. No more `/bgm` macro. Works in cutscenes.

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

A couple of useful chat commands. Each command is separately toggleable.

Available Commands:

- `/itemlink <id>`  
  Prints an item link for the given item id in chat.
- `/whatmount`  
  Target a player and execute the command to see what mount your target is riding and which item teaches this mount.
- `/whatbarding`  
  Target a players chocobo companion and execute the command to see what barding it is wearing.

### Custom Chat Timestamp

Let's you customize the chat timestamp format using C#'s [`DateTime.ToString()`](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings).

### DTR

Shows Instance number (only if the current zone is instanced), FPS and Busy status (only if busy) in the DTR bar.

To enable/disable elements or to change the order go into Dalamud Settings > Server Info Bar.

Options:

- FPS unit text (default: " fps")

### Enhanced Experience Bar

Depending on the settings, the experience bar can be transformed into one of the following bars:

- The PvP Series Bar displays your series rank and experience. If there is a \* symbol next to your rank, it means a rank-up reward is claimable.
- The Sanctuary Bar displays your sanctuary level and island experience.

Available options include:

- Always show PvP Series Bar in PvP Areas
- Always show Sanctuary Bar on the Island
- Hide Job on Sanctuary Bar
- Max Level Override
  - Will switch to the selected bar if your current job is on max level and none of the settings above apply.
- Disable color change

> **Note**
> In order for this tweak to work properly, please make sure "Show Experience Percentage" is disabled in Simple Tweaks.

### Enhanced Isleworks Agenda

Slightly improves the Isleworks "Set Agenda" window.

Options:

- Enable Search Bar: The search bar is automatically shown and focused when the \"Set Agenda\" window opens. Based on the entered item name, the fuzzy search (lowercase only) selects the item on the right side of the window. Pressing the Enter key confirms the selected item, just like by pressing the \"Schedule\" button.
- Disable item tooltips in the list

### Enhanced Login/Logout

Login options:

- Show pets in character selection: Displays a carbuncle for Arcanist/Summoner and a fairy for Scholar next to your character. Position is adjustable.
  > **Note**
  > In order to apply the pet glamor settings, you must have logged in at least once.
- Play emote in character selection: Have your character greet you with an emote!  
  > **Note**
  > Emote settings are per character and not all emotes are supported (e.g. sitting or underwater emotes). What is supported, however, are alternative standing idle poses.
- Preload territory when queued: When it puts you in queue, it will preload the territory textures in the background, just as it does as when you start teleporting.  
  > **Note**
  > Since I only have SSDs, I don't really know if this works at all.

Logout options:

- Clear tell history on logout

### Enhanced Material List

Available options include:

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

### Gear Set Grid

A window that displays a grid view of all the items in each gear set, making it easier to figure out which items to upgrade next.  
This is not meant to be a replacement for the Gear Set List window, but rather a supplement to it.  
Max level jobs for each expansion have item level range colors, with a low item level displayed as red and a high item level displayed as green.

Configuration options:

- Auto-open/close with Gear Set List
- Register `/gsg` command to toggle window
- Convert separator gear set with spacing: When using separator gear sets (e.g. a gearset with name ===========) this option automatically converts it into spacing between rows (in the Gear Set Grid).
  - Spacing between rows can be fully eliminated with the "Disable spacing" option.

### Hide MSQ Complete

Hides the Main Scenario Guide when the MSQ is completed, but still displays Job quests.

### Keep Screen Awake

Prevents the screen from going into standby. Similar to [PowerToys Awake](https://docs.microsoft.com/windows/powertoys/), but only when the game is active.

### Lock Window Position

Lock window positions so you can't move them accidentally anymore.

Adds a context menu entry for the title bar to "Lock/Unlock Position" (can be disabled).  
Alternatively it's possible to add windows by using the window picker in the configuration.

### Material Allocation

Enhances the Islekeep's Index "Material Allocation" window.

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

A helper for editing portraits, featuring:

- A reset button that allows you to easily undo any changes made to the portrait, just as it was when the window was opened.
- Presets can be exported and imported with a short base64-encoded string via clipboard. This allows you to share the preset with others.
- If you wish to only import parts of a preset stored in your clipboard, you can use the Advanced Import Mode to select each setting you want to import separately.
- The Preset Browser allows you to save your portraits into presets and organize them using tags. Simply double click a preset to load it or drag and drop it to change the order or add it to tags.
  - *Note*: The preset list is shared across all tags, but filtered by the selected tag. That means, reordering a preset will change the order across all tags.
  - *Note*: The preview image of a preset is saved as a .png file in the plugins configuration folder.
- The Advanced Edit Mode lets you to precisely control the camera yaw, pitch, distance, x and y position, as well as zoom and rotation, eye and head direction and the animation timestamp.
- An Alignment Tool adds guide lines over the portrait to aid in proper alignment and composition.

Configuration options:

- "Notify if appearance and/or gear doesn't match Portait" (default on)  
  Prints a notification in chat which can be clicked to open the Portrait Editor.
- "Automatically re-equip gear set to re-apply glamour plate" (default off)  
  Works only if the following criteria are met:
  - The gear set has a glamour plate linked.
  - You are in a place where glamour plates are allowed to be applied.
  - The glamour plate covers the slot(s) that caused the mismatch.
  - The mismatch was not caused by mainhand/headgear visibility or visor state.
- "Automatically update portrait" (default off)  
  Only works for gear sets that are not linked with a glamour plate.

### Reveal Duty Requirements

Reveals duty names in duty finder, which were shown as "???" to prevent spoilers. Useful for unlocking Mentor roulette.

### Scrollable Tabs

Allows the mouse wheel to switch tabs (like with LB/RB on controllers) in the following windows, each of which can be toggled separately:

- Aether Currents
- Armoury Chest
- Blue Magic Spellbook
- Character
- Character -> Classes/Jobs
- Character -> Reputation
- Chocobo Saddlebag
- Companion
- Currency
- Fashion Accessories
- Field Records
- Fish Guide
- Glamour Dresser (scrolls pages, not tabs)
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

Adds an entry to item context menus that allows you to quickly search for the item on the market board. Only visible when market board is open!

Supports context menus in the following windows:

- Chat
- Crafting Log
- Ehcatl Nine Delivery Quests (via `/timers`)
- Grand Company Delivery Missions (via `/timers`)
- Inventory
- Materials List
- Recipe Tree
