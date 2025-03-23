<img align="left" src="HaselTweaks/Assets/Icon.png" height="60px" alt="HaselTweaks"/>

**HaselTweaks** is an all-in-one plugin for all my tweaks and helpers.<br/>
<br/>
<hr>

You will not find this plugin in the official plugin repository.  
However, you're free to add my custom repository to get updates whenever I release a new version:  
https://raw.githubusercontent.com/Haselnussbomber/MyDalamudPlugins/main/repo.json

<hr>

To get started, open the configuration with `/haseltweaks` and enable the tweaks you like.

## Tweaks

### Achievement Link Tooltip

Hovering the mouse over an achievement link in the chat will display a small tooltip with the name and description of the achievement so that you don't have to click on the link.

Configuration options:

- Show completion status: Will show whether the achievement has been completed or not. This requires the achievement data to be loaded, so you have to open the window once.
- Prevent spoiler: If the Achievement is not unlocked and would not be displayed in the game, the name and/or description will also be displayed as ???.

### Aether Current Helper

Clicking on a zone in the Aether Currents window opens a helper window that shows where to find the aether currents or which quests unlocks them. Clicking on an aether current in the list flags the position of the aether current or the quest giver on the map.

### Auto Sorter

Sorts items inside various containers when they are opened. Rules can be configured.

Uses the games `/itemsort` command for sorting.

Configuration options:

- Sort armoury on job change (default on)

### Auto-open Recipe

If a new daily/tribal quest objective requires you to craft an item, and you have all the materials for it in your inventory at that moment, this tweak will automatically open the recipe, saving you a whopping 4-5 clicks.

### Background Music Keybind

Adds a configurable keybind to toggle the background music, in addition to the game's existing keybind option to toggle the sound. No more `/bgm` macro. Works in cutscenes.

### Bigger Item Dyeing Preview

Increases the size of the character preview in the "Item Dyeing" window.

### Cast Bar Aetheryte Names

Replaces the name of the action "Teleport" with the Aetheryte name of your destination.

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
- An option to make the Character window always open on the Classes/Jobs tab
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
- `/whatemote`  
  Target a player and execute the command to see which emote your target is currently executing.
- `/whatbarding`  
  Target a players chocobo companion and execute the command to see what barding it is wearing.
- `/glamourplate <id>`  
  This shortcut command executes the "/gearset change" command with the current gear set id and the given glamour plate id (1-20).

### Companion Color Preview

Shows a small drop-down menu above the "Companion" window to select the color of your own chocobo. This is just to preview the colors and will not be saved!

### Custom Chat Message Formats

Lets you customize message formats for various chat channels.

### Custom Chat Timestamp

Lets you customize the chat timestamp format using C#'s [`DateTime.ToString()`](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings).

### Disable Mount Tilt

Disables leaning into turns during riding or flying.

### DTR

Shows Instance number (only if the current zone is instanced), FPS and Busy status (only if busy) in the DTR bar.

To enable/disable elements or to change the order go into Dalamud Settings > Server Info Bar.

Configuration options:

- FPS unit text (default: " fps")

### Enhanced Experience Bar

Depending on the settings, the experience bar can be transformed into one of the following bars:

- The PvP Series Bar displays your series rank and experience. If there is a \* symbol next to your rank, it means a rank-up reward is claimable.
- The Sanctuary Bar displays your sanctuary level and island experience.
- The Companion Bar displays your chocobos rank and experience.

Configuration options:

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

Configuration options:

- Enable Search Bar: The search bar is automatically shown and focused when the \"Set Agenda\" window opens. Based on the entered item name, the fuzzy search (lowercase only) selects the item on the right side of the window. Pressing the Enter key confirms the selected item, just like by pressing the \"Schedule\" button.
- Disable item tooltips in the list

### Enhanced Login/Logout

Login options:

- Skip Logo (default on): Instantly shows the title screen.
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

Configuration options:

- Enable Zone Names: Displays a zone name underneath the item name indicating where it can be gathered. Only the zone with the lowest teleportation cost is displayed. If the name is green it means it's the current zone. Since space is limited it has to shorten the item and zone name.
  - An option is available to disable this for Crystals.
- Enable click to open Map: Allows you to open the map with the gathering marker in said zone.
  - An option is available to disable this for Crystals.
- Auto-refresh Material List/Recipe Tree: Refreshes the material list and/or recipe tree when the inventory has changed.
- Restore Material List on Login: The material list will reopen with the same recipe and quantity each time you log in as long as the window is locked.
- Add "Search for Item by Crafting Method" context menu entry: No more need to open the recipe tree first.

### Enhanced Target Info

Configuration options:

- Display Mounted status (default off): The tooltip shows the name of the mount and the unlock status.
- Display Fashion Accessory status (default off): The tooltip shows the name of the fashion accessory and the unlock status.
- Remove leading zero in HP under 10% (default off): For example, instead of 06.7%, 6.7% will be displayed. The format depends on the client language, not on the language set in Dalamud.

### Expert Deliveries

Always opens the "Grand Company Delivery Missions" window on the "Expert Delivery" tab.

### Fast Mouse Click Fix

The game does not fire UI events for single mouse clicks whenever a double click is detected.  
This tweak fixes it by always triggering the normal mouse click in addition to the double click.

Try it out in the crafting window by clicking the recipe category tabs or the arrow buttons next to the dropdown menu.

### Fix Inventory Open Tab

Automatically resets the inventory to the first tab when opened.

### Forced Cutscene Sounds

Automatically unmutes selected sound channels in cutscenes.

Configuration options:

- Unmute Master Volume (default on)
- Unmute BGM (default on)
- Unmute Sound Effects (default on)
- Unmute Voice (default on)
- Unmute Ambient Sounds (default on)
- Unmute System Sounds (default off)
- Unmute Performance (default off)
- Restore mute state after cutscene

### Gear Set Grid

A window that displays a grid view of all the items in each gear set, making it easier to figure out which items to upgrade next.  
This is not meant to be a replacement for the Gear Set List window, but rather a supplement to it.  
Max level jobs for each expansion have item level range colors, with a low item level displayed as red and a high item level displayed as green.

Configuration options:

- Auto-open/close with Gear Set List
- Register `/gsg` command to toggle window
- Convert separator gear set with spacing: When using separator gear sets (e.g. a gearset with name ===========) this option automatically converts it into spacing between rows (in the Gear Set Grid).
  - Spacing between rows can be fully eliminated with the "Disable spacing" option.

### Glamour Dresser Armoire Alert

Opens a small window next to the Glamour Dresser with a list of items that can be stored in the Armoire (only visible if qualified items were found).

### Hide MSQ Complete

Hides the Main Scenario Guide when the MSQ is completed, but still displays Job quests.

### Inventory Highlight

Hold down the shift key while the inventory is open to highlight identical items.

Configuration options:

- "Ignore item quality": Highlights the same items regardless of whether they are high quality or not.

### Keep Screen Awake

Prevents the screen from going into standby. Similar to [PowerToys Awake](https://docs.microsoft.com/windows/powertoys/), but only when the game is active.

### Lock Window Position

Lock window positions so you can't move them accidentally anymore.

Adds a context menu entry for the title bar to "Lock/Unlock Position" (can be disabled).  
Alternatively it's possible to add windows by using the window picker in the configuration.

### Market Board Item Preview

Automatically try on equipment when you hover over an item in the market board search results.

### Material Allocation

Saves the last selected tab in Islekeep's Index "Material Allocation" window between game sessions.

### Minimap Adjustments

Configuration options:

- Square Collision (for custom minimap mask textures)
- Default Opacity
- Hover Opacity
- Hide Coordinates
  - Visible on hover
- Hide Weather
  - Visible on hover
- Hide Time Indicator
  - Visible on hover
- Hide Cardinal Directions
  - Visible on hover

### Portrait Helper

A helper for editing portraits, featuring:

- A reset button that allows you to easily undo any changes made to the portrait, just as it was when the window was opened.
- Presets can be exported and imported with a short base64-encoded string via clipboard. This allows you to share the preset with others.
  - If you want to import only parts of a preset, the Advanced Import Mode lets you choose which settings you want to import.
- The Preset Browser allows you to save your portraits into presets and organize them using tags. Simply double-click a preset to load it or drag and drop it to change the order or add it to tags.
  - *Note*: The preset list is shared across all tags, but filtered by the selected tag. That means, reordering a preset will change the order across all tags.
  - *Note*: The preview image of a preset is saved as a .png file in the plugins configuration folder.
- The Advanced Edit Mode lets you to precisely control the camera yaw, pitch, distance, x and y position, as well as zoom and rotation, eye and head direction and the animation timestamp.
- An Alignment Tool adds guide lines over the portrait to aid in proper alignment and composition.

Configuration options:

- Embed preset codes in thumbnails  
  The preset code, which is also used for clipboard import/export, is written to the Exif metadata as a UserComment.
- "Notify if appearance and/or gear doesn't match Portait"  
  Prints a notification in chat which can be clicked to open the Portrait Editor.
- "Automatically re-equip gear set to re-apply glamour plate"  
  Works only if the following criteria are met:
  - The gear set has a glamour plate linked.
  - You are in a place where glamour plates are allowed to be applied.
  - The glamour plate covers the slot(s) that caused the mismatch.
  - The mismatch was not caused by mainhand/headgear visibility or visor state.

### Reveal Duty Requirements

Reveals duty names in duty finder, which were shown as "???" to prevent spoilers. Useful for unlocking Mentor roulette.

### Safer Market Board Price Check

Prevents you from checking market board prices while a request is running, minimizing the frequency of encountering the "Please wait and try your search again" screen.

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
- Recipes List

### Shop Item Icons

Displays item icons instead of item category icons in shops.

### Simple Aethernet List

Simplifies the behavior of the Aethernet list: mouseover selects the aetheryte and a click teleports you.
