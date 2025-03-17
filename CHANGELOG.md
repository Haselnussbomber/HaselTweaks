# Changelog

## [33.0.2] (2025-03-17)

### Glamour Dresser Armoire Alert

- **Fixed:** It was only possible to withdraw the first item due to an ImGui id conflict.

## [33.0.1] (2025-03-17)

### Glamour Dresser Armoire Alert

- **Fixed:** The window wouldn't open when items were detected. My bad.

## [33.0.0] (2025-03-08)

### New Tweak: Enhanced Target Info

You guessed it, it enhances the target info.

Current options:

- Display Mounted status (default off): The tooltip shows the name of the mount and the unlock status.
- Display Fashion Accessory status (default off): The tooltip shows the name of the fashion accessory and the unlock status.
- Remove leading zero in HP under 10% (default off): For example, instead of 06.7%, 6.7% will be displayed. The format depends on the client language, not on the language set in Dalamud.

## [32.1.1] (2025-02-16)

### HaselTweaks

- **Fixed:** ImGui window positions of this plugin should no longer reset every game session. Sorry about that.

## [32.1.0] (2025-02-15)

### HaselTweaks

- Mostly internal changes.
- Positions of ImGui windows of this plugin will be reset. Sorry about that.
- Fixed a possible deadlock caused by a hook when the plugin loaded.
- No longer ships with the yoga layout engine, because isn't needed right now.

### Portrait Helper

Re-enabled the option "Automatically update portrait".  
To make this work, the tweak will attempt to update the portrait after saving a gearset. If that didn't work it will show the Portrait Update Preview window as the game intended to do.

## [32.0.4] (2025-01-21)

Preliminary update for Patch 7.16.

## [32.0.3] (2025-01-18)

Okay, the fix in 32.0.1 didn't work. Now I changed my approach, so this should work once again.

## [32.0.2] (2025-01-18)

Forgot to update the version of the plugin again, so it wouldn't update. Sorry about that.

## [32.0.1] (2025-01-15)

### HaselTweaks

- **Fixed:** An exception "Scene is not yet ready" could've been thrown when the plugin loads.  
  Explanation: To handle font scale changes, the plugin registered an event on the default Dalamud font, but because the plugin is loaded before ImGui might be ready the exception could be thrown. To resolve this the plugin now waits for the UI to be ready before registering the event.

## [32.0.0] (2025-01-14)

### New Tweak: Fix Inventory Open Tab

Automatically resets the inventory to the first tab when opened.

## [31.0.0] (2025-01-02)

Happy New Year! 🥳

### New Tweak: Fast Mouse Click Fix

I'm going to try a different method this time, as alternative to the Reduced Mouse Click Throttle tweak I had.

The problem is that the game doesn't fire events for single mouse clicks whenever a double click is detected.  
So instead of reducing the delay between clicks used for double click detection (which breaks them unless you're a really really fast clicker), this time the tweak will make the game always trigger the normal mouse click in addition to the double click.

Hopefully nothing breaks this time. 😅

## [30.0.2] (2024-12-19)

### Aether Current Helper

- **Fixed:** Improved performance by caching excel lookups.

## [30.0.1] (2024-12-18)

### Forced Cutscene Sounds

- **Changed:** The default setting of "Unmute Sound Effects" is now on. I don't know why I thought of Chat sound effects, which are handled in System Sounds...
- **Fixed:** The default setting of "Unmute System Sounds" should've been off, but was on...

Not my day. Sorry.

## [30.0.0] (2024-12-18)

### Renamed Tweak: Forced Cutscene Music -> Forced Cutscene Sounds

New configuration options allow you to unmute specific audio channels:

- Master Volume (default on)
- BGM (default on)
- Sound Effects (default off)
- Voice (default on)
- Ambient Sounds (default on)
- System Sounds (default off)
- Performance (default off)

### Tweak removed: Reduced Mouse Click Throttle

This tweak had the unfortunate side effect of preventing double-clicks, for example when selecting a gear set in the Gear Set List window.

## [29.0.2] (2024-12-17)

Preliminary update for Patch 7.15.

### Scrollable Tabs

- **Fixed:** It should no longer be possible to scroll into disabled tabs in the Currency window.

## [29.0.1] (2024-12-11)

### Shop Item Icons

- **Fixed**: Wrong icons were displayed when the shop contained locked/hidden items (for example, on Allied Society vendors).

## [29.0.0] (2024-12-09)

### New Tweak: Reduced Mouse Click Throttle

Reduces the throttling for the MouseClick event of the UI from 300ms to 100ms.

## [28.0.5] (2024-11-25)

### Enhanced Experience Bar

- **Fixed:** The actual experience bar would only be a tenth long of what it was supposed to be.
- **Fixed:** After teleporting out of a PvP area, the bar would remain in PvP bar mode.

## [28.0.4] (2024-11-21)

Just some code cleanup and fixes.

### Market Board Item Preview

- **Changed**: Items that cannot be previewed due to race or sex restrictions are now ignored.

## [28.0.3] (2024-11-18)

Updated ClientStructs for some AgentLobby offset fixes.

### Enhanced Login/Logout

- **Changed:** As crash prevention, the tweak now hooks `AgentLobby.OpenLoginWaitDialog` only when "Preload territory when queued" is enabled.

## [28.0.2] (2024-11-18)

### Enhanced Login/Logout

- **Fixed:** The tweak never actually checked if "Preload territory when queued" was enabled or not and would always preload the territory in queue.

## [28.0.1] (2024-11-18)

### Portrait Helper

- **Fixed:** Animation timeline in the Advanced Edit Mode would always reset when seeking backwards (offset fixes).

## [28.0.0] (2024-11-17)

Preliminary update for 7.1 (Dalamud staging).

### New Tweak: Outfit Glamour Try On

I'm bad with names, I know, but this one adds a context menu entry to the items in the "Outfit Glamour Creation" window so you can try them on.

### Removed Tweak: Disable Mount Tilt

No longer needed, because Tilt Configuration was added in Patch 7.1 (Character Configuration -> Control Settings -> Character tab).

### Portrait Helper

Disabled the option "Automatically update portrait", because I simply don't have the headroom to reverse-engineer this right now. (This was the option for portraits not linked to gear sets.)

## [27.0.3] (2024-10-29)

### Shop Item Icons

- **Fixed**: Currencies, like Wolf Marks, were shown without icon, because the item id is 0 in the data that is sent to the shop window (which is also why there is no tooltip). The tweak cannot provide an icon for these items and will ignore them from now on, showing the original category icon.

## [27.0.2] (2024-10-26)

### Shop Item Icons

- **Fixed:** Incorrect icons were displayed for items in the Shops Buyback tab.

## [27.0.1] (2024-10-22)

### Shop Item Icons

Sorry for the instant update, I forgot to remove a little note that the zone needs to be reloaded when changing the Shops option from early development.  
This is not the case in the released version. The shop windows just need to be reopened.

## [27.0.0] (2024-10-22)

### New Tweak: Shop Item Icons

Displays item icons instead of item category icons in shops.

## [26.1.1] (2024-08-28)

### HaselTweaks

- **Fixed:** When the plugin unloaded, some disabled tweaks ran code to reset states to default.

### Gear Set Grid

- **Fixed:** Missing icons for empty slots.

## [26.1.0] (2024-08-20)

### Commands

- **Added:** New command `/whatemote`  
  Target a player and execute the command to see which emote your target is currently executing.
- **Fixed:** `/whatbarding` used the wrong command description (when enabled, visible in the Plugin Installer).

## [26.0.2] (2024-08-18)

### HaselTweaks

- **Fixed:** Chat commands were incorrectly registred for disabled tweaks.

## [26.0.1] (2024-08-17)

### Portrait Helper

- **Fixed:** Updating the gearset caused the Gearset List to refresh and scroll to the top.  
  Note: This is caused by a bug in ClientStructs (wrong return type of a function). If other plugins hook this too, my fix won't be effective and you have to wait for a Dalamud update.

### DTR

- **Known Issue:** When the plugin is reloaded via Dalamuds auto-update feature, the DTR entries might not be re-created. This is a bug in Dalamud and is being worked on.

## [26.0.0] (2024-08-17)

### New Tweak: Disable Mount Tilt

Disables leaning into turns during riding or flying.

## [25.2.12] (2024-08-15)

### Search the markets

- **Fixed:** The tweak incorrectly used Dalamuds language setting to search for the item instead of the clients language (yes, again).  
  (I re-wrote the function to get proper item names and forgot to pass the client language.)

## [25.2.11] (2024-08-14)

### HaselTweaks

- **Fixed:** The main plugin window did not stay docked after restarting the game. (This only affects people who enabled window docking in the Dalamud settings and had the window docked.)

## [25.2.10] (2024-08-09)

### Commands

- **Fixed:** /whatbarding did not return the correct color. (Sometimes you just miss something!)

## [25.2.9] (2024-08-09)

### Commands

- **Fixed:** /whatbarding did not return the correct information. (I forgot to fix it after Dawntrail released, sorry!)

## [25.2.8] (2024-08-02)

### Scrollable Tabs

- **Fixed:** Fixed tabs not changing their enabled state in the Buddy window.

## [25.2.7] (2024-07-30)

### Expert Deliveries

- **Fixed:** Turning in an item would reset the item selection to the first entry. (This only affected controller users.)

### DTR

- **Fixed:** A change in the fps format would not be reflected immediately.

## [25.2.6] (2024-07-29)

### Auto Sorter

- **Fixed:** An issue where the `/isort` commands would use parameters based on the plugin language instead of the client language. (Yes, again. The language parameter got lost in a code rewrite.)

### Custom Chat Message Formats

- **Fixed:** Tell (Incoming) and Tell (Outgoing) were swapped.

### DTR

- **Fixed:** Instance not showing up again after disabling and re-enabling the tweak.

## [25.2.5] (2024-07-19)

### DTR

- **Fixed:** Instance not showing up again after switching to a zone without instances and switching back to a zone with the same Instance Id.

### Search the markets

- **Fixed:** The tweak incorrectly used Dalamuds language setting to search for the item instead of the clients language.
- **Fixed:** It wasn't possible to search for items if they were high quality.

## [25.2.4] (2024-07-17)

### Enhanced Login/Logout

- **Fixed:** The login queue territory preload option now correctly preloads the outside housing territory when you've logged out inside of a Company Workshop, the Apartment Lobby or an Apartment and now also supports Shirogane and Empyreum.

## [25.2.3] (2024-07-16)

### DTR

- **Fixed:** The busy status no longer updates its text every frame, but only when needed. (Thanks to @nebel for pointing this out)

## [25.2.2] (2024-07-14)

### Portrait Helper

- **Fixed:** Portraits of gearsets without a glamour plate linked weren't automatically updated when the the gearset was updated.

## [25.2.1] (2024-07-15)

### DTR

- **Changed:** The configuration option to set the FPS unit was changed to a customizable format string. This allows to have text in front of it as well as being able to utilize the custom number formatting options C# provides. Configuration should migrate automatically.
- **Changed:** Server Info Bar entries by HaselTweaks now show the plugin name in their tooltips.

### Enhanced Experience Bar

- **Changed:** Tweak almost completely reworked internally. Updates should now be seamless.

## [25.2.0] (2024-07-14)

### Portrait Helper

- **Fixed:** A bug involving the gearset index that occurs when assigning gearset ids manually.
- **Fixed:** A check that blocked the gear mismatch check from occuring in any other condition than just stading around, like sitting, has been changed to checking if the player is not moving between areas.

### Search the markets

- **Added:** Support for Gathering Log window.

## [25.1.2] (2024-07-14)

### Enhanced Login/Logout

- **Fixed:** Pet glamour settings were not updated when logging in.

### Forced Cutscene Music

- **Fixed:** No longer applies to the title screen cutscene.

### Minimap Adjustments

- **Fixed:** Nodes that should have been hidden were visible.

### Portrait Helper

- **Possibly Fixed:** Clicking the notification in chat would not open the correct gear set.
  - *Note:* I'm calling a different function now which resolves the gearset id. This seems to be necessary for some older gearsets.

## [25.1.1] (2024-07-12)

### Portrait Helper

- **Changed:** Gear checksum is no longer checked during duties, where you can't update your portrait anyways. Instead, it will check your gear once you have left the duty.

## [25.1.0] (2024-07-09)

### Portrait Helper

- **Added:** An option to ignore notifications for DoH/DoL jobs.

## [25.0.0] (2024-07-05)

### New Tweak: Bigger Item Dyeing Preview

Increases the size of the character preview in the "Item Dyeing" window.

## [24.1.3] (2024-07-04)

### Character Class Switcher

- **Fixed:** Hovering over a job in the PvP Character window would crash the game.

## [24.1.2] (2024-07-03)

### Character Class Switcher

- **Fixed:** Buttons weren't clickable and some of them would crash the game. Tweak enabled again.

## [24.1.1] (2024-07-03)

### Portrait Helper

- **Fixed:** Calculating the checksum would crash the game. Sorry!

### Known Issues

- **Character Class Switcher:** Buttons aren't clickable and some of them crash the game. I've disabled the tweak for now until I find a solution. Sorry!

## [24.1.0] (2024-07-03)

Preliminary update for Dawntrail (Dalamud staging).

### DTR

- **Fixed:** Fixed Instance not being shown after relogging.

### Glamour Dresser Armoire Alert

- **Fixed:** After the window closed, hitting the Escape key would no longer do anything.

### Material Allocation

- **Removed:** The option "Open Sanctuary Gathering Log for gatherable items" was removed, because it was added to the game!!! 🥳

### Portrait Helper

- **Fixed:** The Preset Browser wouldn't close properly after loading a preset.
- **Fixed:** The Advanced Import Overlay wouldn't close properly after importing a preset.

## [24.0.3] (2024-06-24)

### HaselTweaks

- **Fixed:** The plugin might've not registered its `/haseltweaks` command.

## [24.0.2] (2024-06-24)

### HaselTweaks

- **Changed:** Completely reworked/simplified the code to draw the configuration.
- **Fixed:** Starting with v24.0.0 some tweaks didn't correctly unload when unloading the plugin.
- **Fixed:** Updated Chinese Translations. (Thanks to @cyf5119)

## [24.0.1] (2024-06-21)

### Enhanced Isleworks Agenda

- **Fixed:** The search bar was only focused the first time it was opened, and the text wasn't cleared. Sorry about that!

## [24.0.0] (2024-06-21)

### New Tweak: Market Board Item Preview

Automatically try on equipment when you hover over an item in the market board search results.

### Enhanced Isleworks Agenda

- **Changed:** Search text is now trimmed before fuzzy searching for the item. (Thanks to @CallumCarmicheal)

### Forced Cutscene Music

- **Changed:** The tweak should now support more cutscenes. This is done by hooking the CutSceneController class instead of the PlayCutSceneTask.

### Portrait Helper

- **Changed:** Gear checksum mismatch is now checked when the game updates the class/job info in the agents. (Using a hook now instead of checking class/job id every frame.)
- **Fixed:** The menu bar might've been incorrectly positioned while initializing.
- **Fixed:** The rename tag dialog didn't use translated texts.

### HaselTweaks

Not so important note:  
I'm currently working on restructuring the plugins source code, but I'm not fully done yet.  
The plugin may or may not have some issues because of it, but so far everything seems fine.  
If you find anything that doesn't work, please let me know via the plugins [GitHub Issues](https://github.com/Haselnussbomber/HaselTweaks/issues). Thanks!

Also I'm shipping ClientStructs for now, since I've already adapted to these changes, which results in a slightly bigger size.  
I didn't expect to release another update so close to the expansion, but I was asked to fix a bug. :>  
Will probably switch back to Dalamuds ClientStructs again once it's released for Dawntrail.

## [23.0.2] (2024-05-15)

### Scrollable Tabs

- **Fixed:** The game would crashe when scrolling too far in the Expanded Retainer Inventory or Companion window.

## [23.0.1] (2024-05-14)

### Cast Bar Aetheryte Names

- **Fixed:** The cast bar should no longer display the aetheryte name on subsequent casts.

## [23.0.0] (2024-05-02)

### New Tweak: Achievement Link Tooltip

Hovering the mouse over an achievement link in the chat will display a small tooltip with the name and description of the achievement so that you don't have to click on the link.

![Screenshot](https://github.com/Haselnussbomber/HaselTweaks/assets/96642047/579e6085-c537-46b3-bdcd-1d84618ba15e)

Options include:

- Show completion status (default on): Will show whether the achievement has been completed or not. This requires the achievement data to be loaded, so you have to open the window once.
- Prevent spoilers (default on): If the Achievement is not unlocked and would not be displayed in the game, the name and/or description will also be displayed as ???.

### New Tweak: Cast Bar Aetheryte Names

Replaces the name of the action \"Teleport\" with the Aetheryte name of your destination.

![Screenshot](https://github.com/Haselnussbomber/HaselTweaks/assets/96642047/07af2799-c989-4be2-8832-2b77aa2974dd)

## [22.0.1] (2023-04-16)

### Inventory Highlight

- **Fixed:** Hovering HQ items would not highlight similar items when "Ignore item quality" was enabled.

## [22.0.0] (2023-04-15)

### New Tweak: Inventory Highlight

Hold down the shift key when the inventory is open to highlight identical items.  
An option is available to ignore the item quality (default on).

### Enhanced Login/Logout

- **Added:** An option to skip the startup logo (default on). Instantly shows the title screen.

### Commands

- **Added:** New Command `/glamourplate <id>`  
  This shortcut command executes the "/gearset change" command with the current gear set id and the given glamour plate id (1-20).

## [21.0.0] (2023-04-06)

### New Tweak: Custom Chat Message Formats

Lets you customize message formats for various chat channels.

## [20.4.0] (2023-03-31)

### HaselTweaks

- **Added:** The plugin has been translated to Simplified Chinese thanks to @cyf5119.  
  - *Note:* I don't understand any of this, so please submit fixes if anything is wrong or missing (this also applies to other languages). Thank you! 😊

### Auto-open Recipe

The tweak has been slightly reworked and now listens for "inventory item added" events, instead of quest objective changes. As a result of this...

- **Added:** Support for Namazu quests!

Please report any issues you may encounter due to the rework. Thanks! 🙂

### Portrait Helper

- **Changed:** The animation timestamp slider now has 3-digit precision when the animation is less than 100 frames long.

## [20.3.2] (2023-03-26)

### Custom Chat Timestamp

- **Fixed:** The preview text was sometimes invisible.

### Portrait Helper

- **Changed:** Equipment data for the checksum is now loaded directly from a game function instead of manually gathering all item and dye ids. This potentially fixes issues with HQ items, but simultaneously breaks respecting class/job restrictions on glamoured items introduced in [20.0.1] as this is not something the game does.
- **Fixed:** The sidebar title ("Tags") in the Preset Browser is now sticky at the top, too.

## [20.3.1] (2023-03-26)

### Portrait Helper

- **Fixed:** Backgrounds, frames, accents and poses that are not unlocked on the character will no longer be applied when importing or loading a preset.
- **Fixed:** The Advanced Import overlay now closes properly when the clipboard changed to something different than a preset string.
- **Fixed:** The weird 1px scroll flicker bug in the Preset Browser should be gone now.

## [20.3.0] (2023-03-20)

### HaselTweaks

- The target framework has been updated to .NET 8 and some code has been adjusted to take advantage of the additions it brings.
- Dependencies that are no longer needed have been removed:
  - Dalamud.ContextMenu: This is now included in Dalamud.
  - FuzzySharp: The FuzzyMatcher from Dalamud is now used instead.
  - NoAlloq: The game window detection has been further optimised.
- Performance optimizations:
  - More efficient loops in code that runs every frame.
  - AutoSorter is now notified when inventory windows are opened, instead of manually checking each inventory window every frame.
  - Code that doesn't need to be executed every frame while logged out is now skipped.

### Portrait Helper

- **Added:** Preset codes are now written to the thumbnail Exif metadata as UserComment.  
  This can be disabled in the tweak settings. Existing thumbnails are **not** updated retroactively.

## [20.2.1] (2023-03-05)

### HaselTweaks

- Updated dependency SixLabors.ImageSharp to 3.1.3 (security fix for [CVE-2024-27929](https://github.com/advisories/GHSA-65x7-c272-7g7r)).
- Removed dependency System.IO.Hashing, which is no longer needed in HaselTweaks.

### Portrait Helper

- **Changed:** Thumbnails now use the preset ID instead of a hash for filenames. Existing thumbnails have been automatically renamed.  
  If errors occur, please check the console (`/xllog` - the lines are prefixed with `[MigrationV6]`) to see which files could not be renamed.

## [20.2.0] (2023-02-28)

### Character Class Switcher

- **Added:** An option to always open the Character window on the Classes/Jobs tab.

### Minimap Adjustments

- **Added:** New options to hide the time indicator and to show it when hovering.
- **Added:** New options to hide the cardinal directions and to show them when hovering.
- **Changed:** The transparency now has a smooth transition when it changes.

## [20.1.4] (2023-01-28)

### HaselTweaks

- **Changed:** The plugin now uses Dalamud's language setting and will fall back to English if there's no translation available. If you haven't changed the language in HaselTweaks itself, nothing will change for you.
- **Fixed:** Item names, chocobo colors or portrait expressions, are now respecting the language selected in Dalamud instead of using the client's language.

### Gear Set Grid

- **Fixed:** When opened in game and returned to the main menu, the window was still visible. It is now automatically hidden when you're not logged in.

## [20.1.3] (2023-01-07)

Small address resolver hotfix for [v20.1.2](https://github.com/Haselnussbomber/HaselTweaks/releases/tag/v20.1.2).

## [20.1.2] (2023-01-07)

### HaselTweaks

- **Fixed:** Tweaks were not notified when the config was updated.

### Material Allocation

- **Fixed:** Config is now saved directly upon changing tabs.
- **Fixed:** The currently selected language in the dropdown was not highlighted.
- **Fixed:** Removed the language selection from the tweak config that was added there by mistake.

### Portrait Helper

- **Fixed:** Stopped the maximum timestamp value from updating while the animation is loading.

## [20.1.1] (2023-12-28)

Small UI hotfix for [v20.1.0](https://github.com/Haselnussbomber/HaselTweaks/releases/tag/v20.1.0).

### Portrait Helper

- **Fixed:** For some emotes, the frame count in the animation timestamp input would display decimals.

## [20.1.0] (2023-12-28)

### Enhanced Isleworks Agenda

- **Added:** A drop-down menu has been added to the right of the search bar that allows you to change the language in which the search is performed.

### Portrait Helper

- **Changed:** Dragging the animation timestamp forward now advances the animation smoothly. 🎉  
  Unfortunately, moving backwards still uses the old method of reloading the animation at a specific timestamp, which causes flickering.

## [20.0.3] (2023-12-20)

### HaselTweaks

- **Changed:** The configuration has been rewritten.
  - It now uses .NETs `System.Text.Json` instead of `Newtonsoft.Json`.
  - It'll create a backup of the current config to `%APPDATA%\XIVLauncher\pluginConfigs\HaselTweaks.json.bak` before migrating.
  - If the migration failed or the new config can't be loaded, please [make an issue on GitHub](https://github.com/Haselnussbomber/HaselTweaks/issues/new) and upload the dalamud.log (or just the error), the current config, and if available the backup. In my testing everything worked, but you never know... Good luck! :)
- **Changed:** Detection of inventory changes (currently only used for the Enhanced Material List tweak) now uses Dalamuds new `GameInventory` service.

### Portrait Helper

- **Fixed:** The Preset Browser as a window (when using a larger font scaling) was not movable.
- **Fixed:** An error would occur when closing the portrait editor window.

## [20.0.2] (2023-12-12)

### Portrait Helper

- **Fixed:** Follow up to 20.0.1 - Dyes are now also ignored when the glamoured item could not be applied due to restrictions.

## [20.0.1] (2023-12-11)

### Portrait Helper

- **Fixed:** When generating the gear checksum, the tweak will now respect class/job restrictions on glamoured items and fall back to the original item id.

## [20.0.0] (2023-11-17)

### New Tweak: Safer Market Board Price Check

Prevents you from checking market board prices while a request is running, minimizing the frequency of encountering the "Please wait and try your search again" screen.

### Aether Current Helper

- **Changed:** The window now has a button in its title bar to lock/unlock its position and size.
- **Changed:** The window does no longer resets its size upon opening.

### Gear Set Grid

- **Changed:** The window now has a button in its title bar to lock/unlock its position and size. The window context menu was removed.
- **Changed:** The window now has a minimum size to make sure it always shows all columns.

## [19.1.0] (2023-11-07)

### Search the markets

- **Added:** Support for Recipes List window.

## [19.0.5] (2023-11-04)

### Enhanced Material List

Reworked internals to make the following changes:

- **Changed:** The material list and recipe tree will now refresh after an inventory change, rather than when certain windows are opened/closed.
- **Fixed:** The plugin will now respect the refresh button state and won't trigger a refresh while a refresh is already running.
- **Fixed:** The game will no longer crash when the plugin unloads while the material list was open.

## [19.0.4] (2023-10-10)

### HaselTweaks

- **Changed:** Dropped the SVG rendering library and replaced the logo in the settings with a simple PNG image. Making it scaleable with the UI was a nice thought, but in the end it just wasn't worth it.

## [19.0.3] (2023-10-06)

### Glamour Dresser Armoire Alert

- **Fixed:** Info text didn't display correctly due to changed translation key.

## [19.0.2] (2023-10-06)

### Enhanced Isleworks Agenda

- **Changed:** The search automatically selects the item in the list now!

## [19.0.1] (2023-10-06)

### Enhanced Isleworks Agenda

- **Fixed?:** Possible fix for a game crash when searching for an item without having all items unlocked yet.  
  (Because I'm already at rank 18 and have all items unlocked it's a guessing game for me as to why it crashes.)

## [19.0.0] (2023-10-06)

### New Tweak: Simple Aethernet List

Simplifies the behavior of the Aethernet list: mouseover selects the aetheryte and a click teleports you.

## [18.0.4] (2023-10-06)

### Lock Window Position

- **Fixed:** Game crashed upon clicking the context menu entry.

## [18.0.3] (2023-10-05)

### Character Class Switcher

- **Fixed:** Tweak didn't work due to shifted AtkUnitBase functions.

### Lock Window Position

- **Fixed:** Gear Set List window lock didn't work due to shifted AtkUnitBase functions.

### Portrait Helper

- **Fixed:** Gear checksum mismatched due to shifted number array.

### Scrollable Tabs

- **Fixed:** Currency window didn't switch tabs due to shifted number array.
- **Fixed:** Character -> Reputation didn't switch expansions due to shifted number array.

## [18.0.2] (2023-10-04)

### Portrait Helper

- **Fixed:** Another fix regarding emotes, this time in the Advanced Edit Mode.

## [18.0.1] (2023-10-04)

### Enhanced Login/Logout

- **Fixed:** Playing an emote would crash the game.

## [18.0.0] (2023-10-04)

Update for Patch 6.5.

### HaselTweaks

- **Added:** Options that automatically send network requests are now marked with a yellow bolt ⚡ icon. Not that anything has happened, but I just wanted to let the user know that they are using it at their own risk.
  - At the moment this only applies to Portrait Helper.

### New Tweak: Glamour Dresser Armoire Alert

Opens a small window next to the Glamour Dresser with a list of items that can be stored in the Armoire (only visible if qualified items were found).

### Enhanced Experience Bar

- **Fixed:** Companion Bar no longer shows 0/0 experience on rank 20.

### Material Allocation

- **Fixed:** Sanctuary Gathering Log did not update the map for items gathered in the cave.

### Scrollable Tabs

- **Fixed:** Due to an incorrect tab count, it was possible to scroll into hidden tabs of the Key Items window (normal inventory).

## [17.0.2] (2023-09-25)

### Aether Current Helper

- **Fixed:** The list didn't render due to an error.

## [17.0.1] (2023-09-20)

### Enhanced Isleworks Agenda

- **Fixed:** Search Bar got confused when the item list was sorted by Category.

## [17.0.0] (2023-09-20)

### New Tweak: Enhanced Isleworks Agenda

Slightly improves the Isleworks "Set Agenda" window.

Options:

- Enable Search Bar: The search bar is automatically shown and focused when the \"Set Agenda\" window opens. Based on the entered item name, the fuzzy search (lowercase only) selects the item on the right side of the window. Pressing the Enter key confirms the selected item, just like by pressing the \"Schedule\" button.
- Disable item tooltips in the list

## [16.2.2] (2023-09-13)

A lot of internal restructuring due to my new HaselCommon library, which is now used in all my plugins. I hope it works fine!

### Gear Set Grid

- **Fixed:** Closing the window didn't remove it from the WindowManager, making it difficult to reopen.

## [16.2.1] (2023-08-26)

### Commands

- **Fixed:** The rarity color of item links could have been incorrect depending on the UI theme (I misinterpreted the UI colors sheet).

### Gear Set Grid

- **Changed:** The Id column now uses the colored, framed job icon, and the layout has been changed to place the icon above the Id.

## [16.2.0] (2023-08-20)

### Auto Sorter

- **Fixed:** An issue where the `/isort` commands would use parameters based on the plugin language instead of the client language.

### Portrait Helper

- **Added:** A new option "Automatically update portrait" has been added. Thanks to @Rycko1 for helping!  
  This is only for gear sets that do not have a glamour plate linked! It will automatically send a portrait update to the server, when you update the gear set.
- **Fixed:** The reset button should now properly reset the portrait to the initial state of the *current* portrait.

## [16.1.3] (2023-08-19)

### Enhanced Login/Logout

- **Fixed:** Pet glamours should now be applied correctly.

## [16.1.2] (2023-08-18)

### HaselTweaks

- **Fixed:** Text from the Addon sheet wasn't read in the selected language.
- **Fixed:** Text cache was not cleared when switching languages.

### Portrait Helper

- **Changed:** The Animation Timestamp input in the Advanced Edit Mode has been reworked. It is a slider now and supports dragging to scroll through the animation. But, two things:
  - The flickering has not been fixed yet, as it still relies on the same game function that restart/reloads the animation at the specific timestamp.
  - As I understand it, the end timestamp used for the slider is based on the duration of the emote, which is different from the duration of the race-specific animation. Since I can't find the actual animation duration for the life of me, we have to use the general emote duration for now.
- **Fixed:** The overlays didn't update their position when the window moved.

## [16.1.1] (2023-08-17)

### HaselTweaks

- **Fixed:** Changing tweak settings didn't save the config. Sorry about that!

### Portrait Helper

- **Changed:** The overlays should now play nicely with the different UI themes. 🎉

## [16.1.0] (2023-08-15)

### HaselTweaks

- **Added:** You can now set the plugins language via a Plugin Configuration menu bar in the plugin window. To toggle the menu bar, click on the gear button in the top right corner. The default language, which was previously determined by the client language, is now whatever is set in Dalamud (if not supported, it defaults to English). You can also choose to automatically use whatever the client is using, or set the language directly.  
  *Please note:* I can only provide full translation in English and German. While French and Japanese are supported as they are part of the game, I only included the few lines I had already translated using ChatGPT. If you want to help translating the plugin into another language, feel free to edit the Translations.json and submit a pull request! :)  
  To avoid confusion I decided not to translate tweak names (for now).

### Aether Current Helper

- **Changed:** The window now hides and unhides automatically with the game UI.

### DTR

- **Added:** A configuration field to change the FPS unit text (default: " fps").

### Enhanced Experience Bar

- **Fixed:** The experience bar should no longer get stuck on old data, as it will now try to update it every frame until it's done.

### Gear Set Grid

- **Added:** The id column now has a tooltip with the gear set name and the glamour plate id (if linked).
- **Added:** The id column now also has a context menu to link/change/unlink glamour plates and edit portraits.
- **Changed:** Removed the "Allow switching gearsets" option, because that's the default now.
- **Fixed:** The mouse cursor no longer changes to a hand when hovering over something because gear sets and items can't be dragged out of the ImGui window.

## [16.0.4] (2023-08-05)

### Scrollable Tabs

- **Fixed:** It was not possible to scroll the Chocobo Saddlebag when viewing the retainer inventory.

## [16.0.3] (2023-08-02)

### Background Music Keybind

- **Fixed:** The game would respond to the keybind. Now the key press state is reset after handling it.

### Gear Set Grid

- **Fixed:** Calculating the item level color caused the table to suddenly stop rendering, because it was using a wrong column in the ClassJob sheet.

## [16.0.2] (2023-07-31)

### Scrollable Tabs

- **Fixed:** Scrolling in the Blue Magic Spellbook broke with Patch 6.45.

## [16.0.1] (2023-07-31)

- **Fixed:** I reworked the way the plugin detects when windows are opened/closed in the last update and forgot to check if the window is fully loaded before interacting with it. This update should fix focus issues with the armoury chest when Auto Sorter is enabled.

## [16.0.0] (2023-07-31)

### New Tweak: Background Music Keybind

Adds a configurable keybind to toggle the background music, in addition to the game's existing keybind option to toggle the sound. No more `/bgm` macro. Works in cutscenes.

### New Tweak: Gear Set Grid

A window that displays a grid view of all the items in each gear set, making it easier to figure out which items to upgrade next.  
This is not meant to be a replacement for the Gear Set List window, but rather a supplement to it.

### Commands

- **Added:** New Command `/whatbarding`  
  Target a players chocobo companion and execute the command to see what barding it is wearing.

### Portrait Helper

- **Fixed:** The notification that the gear does not match the portrait did not show up when you were outside of a sanctuary.

## [15.3.3] (2023-07-20)

- **Fixed:** Sometimes the game would freeze, especially when reloading the plugin, due to asynchronously enabling the hooks. Now, the plugin makes sure to enable them on the framework thread.

## [15.3.2] (2023-07-19)

Update for Patch 6.45.

- **Added:** The AGPL 3.0 license has been added to the project.
- **Changed:** The texture cache has been reworked to make it suitable for future use.
- **Changed:** Debug information are now embedded as [Portable PDB](https://github.com/dotnet/core/blob/main/Documentation/diagnostics/portable_pdb.md).
- **Fixed:** A signature to set the portrait frame, that had multiple matches after the patch, was replaced.
- **Fixed:** The new Dalamud windows sound effects would play for the portrait helper menu bar and overlays.

## [15.3.1] (2023-07-14)

- Multiple code adjustments due to ClientStructs and Dalamud additions and minor refactoring.
- **Fixed:** An error that occured when unloading the plugin, because a tweak would remove a window from the window system after it has been disposed already.
- **Fixed:** A potential issue with loading icon textures if the path was redirected by Penumbra.

## [15.3.0] (2023-07-10)

### Search the markets

- **Added:** Support for Ehcatl Nine Delivery Quests items (via `/timers`).

## [15.2.0] (2023-07-10)

### Search the markets

- **Added:** Support for Grand Company Delivery Missions items (via `/timers`).

## [15.1.5] (2023-07-08)

Maintenance update with optimizations, bug fixes and a lot of behind-the-scenes changes.

## [15.1.4] (2023-07-01)

### Custom Chat Timestamp

- **Added:** Changing the format will now automatically refresh the chat.

## [15.1.3] (2023-07-01)

### Custom Chat Timestamp

- **Fixed:** Tweak didn't apply when in-game setting "Clock Type" was set to "12-hour Format".

## [15.1.2] (2023-07-01)

### Character Class Switcher

- **Changed:** The message "Couldn't find a suitable gearset." has been translated into German, and using ChatGPT into French and Japanese. Please file a bug if the translation is incorrect.

### Portrait Helper

- **Fixed:** The changes in v15.1.0 broke the logic of the menu bar, so I reworked the way it handles the overlays/windows. Hopefully it's bug-free now.

## [15.1.1] (2023-06-30)

### HaselTweaks

- **Optimized:** Replaced the logo with an SVG, which eliminates the startup lag caused by rebuilding fonts.
- **Fixed:** Logo now also respects Dalamuds Global Font Scale setting.

## [15.1.0] (2023-06-29)

### HaselTweaks

- **Fixed:** Various UI elements now respect Dalamuds Global Font Scale setting.
  > **Note**
  > Portrait Helper overlays will open as windows instead if the global font scale is greater than 12pt.

### Auto Sorter

- **Added:** An option "Sort armoury on job change" (on by default). Sorts only when the armory is open.

### Enhanced Login/Logout

- **Added:** An option "Play emote in character selection" (off by default). Have your character greet you with an emote!  
  To set an emote, you'll need to log in, click the "Change" button, then perform the emote you want to use, and finally click the "Stop Recording" button.
  > **Note**
  > Emote settings are per character and not all emotes are supported (e.g. sitting or underwater emotes). What is supported, however, are alternative standing idle poses.

## [15.0.0] (2023-06-21)

### New Tweak: Enhanced Login/Logout

Login options:

- Show pets in character selection: Displays a carbuncle for Arcanist/Summoner and a fairy for Scholar next to your character. Position is adjustable.
  > **Note**
  > In order to apply the pet glamor settings, you must have logged in at least once.
- Preload territory when queued: When it puts you in queue, it will preload the territory textures in the background, just as it does as when you start teleporting.  
  > **Note**
  > Since I only have SSDs, I don't really know if this works at all.

Logout options:

- Clear tell history on logout

### DTR

- **Fixed:** Sometimes text would not be set, resulting in an empty DTR entry.

## [14.12.1] (2023-06-06)

### HaselTweaks

- **Fixed:** Resolver wouldn't resolve the same signature with different offsets (happened to Forced Cutscene Music).

### Enhanced Experience Bar

- **Fixed:** The PvP Series Bar will now update after claiming a rank to clean the claimable rank indicator.

## [14.12.0] (2023-06-03)

### HaselTweaks

- **Optimized:** The plugins setup function is now run in an asynchronous thread to reduce startup lag.

### Aether Current Helper

- **Optimized:** The window is now created on demand.

### DTR

- **Fixed:** Framerate number now correctly rounds up.

### Scrollable Tabs

- **Added:** Support for the Companion window.
- **Added:** Support for the Glamour Dresser window (scrolls pages, not tabs).

## [14.11.3] (2023-05-28)

Maintenance update with optimizations and bug fixes.

### HaselTweaks

- **Fixed:** Disabled buttons now display their intended tooltips again.
- **Optimized:** Multiple hooks now use cached addresses for signatures, which makes the plugin start a bit faster due to less scanning.
- **Optimized:** The FPS display in the DTR tweak now only updates when the frame rate has changed to avoid formatting a number into a string on every frame.

### Scrollable Tabs

- **Fixed:** It was possible to scroll in windows when they were the last thing the cursor hovered before tabbing out. Now scrolling is disabled when the game isn't focused.

## [14.11.2] (2023-05-26)

### Enhanced Material List

- **Fixed:** After closing the "Currency Exchange" window the material list didn't refresh, because I simply forgot to check for this window.
- **Fixed:** "Search for Item by Crafting Method" context menu entry wasn't shown due to wrong offsets.

## [14.11.1] (2023-05-24)

Another update for Patch 6.4 (staging).

- **Search the markets:** Enabled again, because Dalamud.ContextMenu has been updated.

### Known Issues

- **DTR:** Busy status and Instance number are not showing until ClientStructs is updated and shipped with a new Dalamud version.

## [14.11.0] (2023-05-24)

Preliminary update for Patch 6.4 (staging).

### Known Issues

- **DTR:** Busy status and Instance number are not showing until ClientStructs is updated and shipped with a new Dalamud version.
- **Search the markets:** Currently disabled until Dalamud.ContextMenu is updated.

### Scrollable Tabs

- **Added:** Support for the Character window, including Classes/Jobs and Reputation tabs.

## [14.10.0] (2023-05-20)

Some text has been translated into French and Japanese by using ChatGPT (commit [2d24063](https://github.com/Haselnussbomber/HaselTweaks/commit/2d24063)).  
Please file a bug if any translation is incorrect.

### Scrollable Tabs

- **Added:** Support for the Chocobo Saddlebag window.
  - The second tab requires a subscription to the Companion Premium Service.
- **Added:** Support for the Currency window.
- **Fixed:** In the "Open All" Inventory, scrolling over the crystal grid now correctly switches tabs.

## [14.9.3] (2023-05-03)

### Enhanced Experience Bar

- **Fixed:** Exiting the Duty Recorder would crash the game, because the experience bar is used immediately after the zone change before it's fully set up.

## [14.9.2] (2023-04-30)

Maintenance update with some internal restructuring and bug fixes.

### HaselTweaks

- **Changed:** For safety, any drawn text is now drawn with `ImGui.TextUnformatted`.
- **Changed:** Configuration fields now have consistent indentation.
- **Changed:** The description for dropdown configuration fields is now correctly below the dropdown.
- **Fixed:** Reset buttons now work correctly when more than one is displayed.

### Custom Chat Timestamp

- **Added:** The configuration now shows the result of the current format with an example message.
- **Added:** The configuration now shows an error if the format is invalid.
- **Added:** The format field in the configuration now has a reset to default button.
- **Fixed:** The timestamp will not be modified if the format field is empty.

## [14.9.1] (2023-04-29)

### Portrait Helper

- **Fixed:** Added some extra gearset id checks (only allows 0 - 99; the game may return 255 if the gearset is not found).

### Search the markets

- **Fixed:** Disabled the context menu entry for collectable items.
- **Fixed:** The HQ symbol is now stripped from the item name.

## [14.9.0] (2023-04-28)

### Custom Chat Timestamp

- **Fixed:** Errors caused by an invalid format are now caught and don't crash the game anymore.

### Portrait Helper

The tweak will now display a notification whenever the appearance and/or gear doesn't match what was saved in the portait (can be disabled).

- **Added:** A new option `Notify if appearance and/or gear doesn't match Portait` (default on):
  - Prints a notification in chat which can be clicked to open the Portrait Editor.
- **Changed:** The option `Re-equip Gearset when it was updated` was renamed to `Try to fix by automatically re-equipping Gearset to reapply Glamour Plate` and now depends on the notification option.
  - The functionality was not changed. Still only works in places where Glamour Plates are allowed to be applied, if the Glamour Plate covers the correct slots and if the gear checksum mismatch was not caused by a mismatch of mainhand/headgear visibility or visor state.

## [14.8.0] (2023-04-23)

Sorry for the quick updates, but I just came up with this solution:

### Portrait Helper

- **Added:** "Re-equip Gearset when it was updated" option (default **off** to prevent confusion).
  - This option ensures the glamour plate is applied after updating the gearset by re-equipping it, which should help with portrait resetting to default. Of course it only works in places where glamour plates are allowed to be applied, if the gearset actually is linked to a glamour plate and if the glamour plate covers the changed slots.

## [14.7.0] (2023-04-23)

### Portrait Helper

- **Removed:** The option "Auto-update Portrait when Gearset was updated", which was added in v0.14.6, didn't work as expected - in fact, it probably did nothing at all. I'm sorry.
- **Fixed:** Under certain conditions the clipboard didn't get closed, which prevented it from working anywhere in Windows until the game was closed.

## [0.14.6] (2023-04-20)

### Portrait Helper

- **Added:** New config option "Auto-update Portrait when Gearset was updated" (default on).
  - This updates the gearset hash in the linked portrait when you save your gearset, so it no longer resets to the default portrait when you changed an item, glamour or the linked glamour plate.

---

Starting with the next release, the versioning will change to make things clearer. The major version (currently 0) will increase when I add/remove/rework tweaks, the minor version (currently 14) will increase when I add/remove things to existing tweaks, and the patch version (currently 6) will increase when I fix things. So don't get confused if it suddenly becomes v14.6.1, v14.7.0 or v15.0.0. 😄

## [0.14.5] (2023-04-17)

- **Fixed:** The "Configuration" section title in the Tweak configuration is now hidden when there are no fields to configure.

### Portrait Helper

- **Added:** An Alignment Tool which adds guide lines over the portrait to aid in proper alignment and composition.
  - Hold Shift when clicking the button to open the settings, where you can change the number and color of the lines.

## [0.14.4] (2023-04-11)

### Portrait Helper

- **Fixed:** Apparently copying an image to the clipboard is harder than I thought. Updated the code for better compatibility with programs like Paint and Discord.

## [0.14.3] (2023-04-11)

### Portrait Helper

- **Added:** The preset context menu in the Preset Browser has been expanded to include a "Copy Image" function.
- **Fixed:** To avoid an error when reading the contents of the clipboard, a check has been added to see if any text is present (because you can actually copy other stuff with it, who knew).
- **Fixed:** Restore UI visibility when in Advanced Edit Mode and a game popup was opened (e.g. the close confirmation window or context menu of title bar).

## [0.14.2] (2023-04-10)

### Portrait Helper

- **Changed:** Instead of raw Camera and Camera Target Positions the Advanced Edit Mode now has Yaw, Pitch, Distance, X and Y controls.
  - _Note:_ Due to a bug, I had to disable the input fields for all of them except for Distance. Dragging the fields while holding down the left mouse button should still be fine enough for accurate positioning, even though values might flicker a bit.
- **Added:** Zoom and Rotation controls added to Advanced Edit Mode.

## [0.14.1] (2023-04-09)

### Portrait Helper

- **Added:** An Advanced Edit Mode to precisely set camera position and target, eye and head directions and the animation timestamp, but don't get your hopes up too high - I think the camera uses a spherical coordinate system. Good luck!
- **Fixed:** Some game UI elements did not reappear after closing the Advanced Import or Preset Browser.

## [0.14.0] (2023-04-09)

### Tweak reworked: Portrait Helper

Updated UI and features!

- **Added:** A reset button has been added. This returns the portrait to the state it was in when the window was opened.
- **Added:** Its now possible to save portraits into presets. Use the brand-new preset browser to load presets and organize them using tags. A double click on the preset will load it and you can drag and drop them to easily change their order or add them to tags!
  - _Please note:_ As the list of presets is shared by all tags, but filtered by the selected tag, reordering a preset will change the order across all tags.
- **Changed:** The copy and paste buttons have been replaced by export and import buttons, which make use of the clipboard and export the portrait as a short base64-encoded string.

### Enhanced Material List

- **Fixed:** Due to a condition check being evaluated too late, some gatherable items did not have a zone name.

### Material Allocation

- **Fixed:** Restored a hook to correctly switch tabs when opening the window. Since v0.13.3, it looked like it changed the tabs, but it only updated the buttons, not the content.

## [0.13.7] (2023-04-01)

### Lock Window Position

- **Fixed:** Fixed a game crash caused by an infinite loop due to an overload function removed in v0.13.5 as part of a code cleanup.

## [0.13.6] (2023-03-31)

### Hide MSQ Complete

- **Fixed:** Accidentally flipped a check in v0.13.5.

## [0.13.5] (2023-03-31)

### Auto Sorter

- **Fixed:** Game should no longer crash when EventFramework wasn't available during tweak setup.

## [0.13.4] (2023-03-31)

### Auto Sorter

- **Fixed:** Inventory and Armoury will auto sort again.

## [0.13.3] (2023-03-30)

### Character Class Switcher

- **Fixed:** The tweak now enables DoH buttons when desynthesis is not unlocked.

### Enhanced Experience Bar

- **Fixed:** The experience bar will refresh after level syncing for a FATE to remove leftover PvP Series data set by the Max Level Override.

### Material Allocation

- **Fixed:** Sanctuary Gathering Log now correctly switches to the item you clicked on in the Material Allocation window.

## [0.13.2] (2023-03-23)

### Auto-open Recipe

- **Changed:** In addition to daily quests, tribal quests are now also allowed. Duh.

## [0.13.1] (2023-03-21)

### Auto-open Recipe

- **Added:** Support for older daily quests (Ixal and Moogle), which did not return item ids on their objectives.
- **Changed:** Restricted the tweak to daily quests. Just to make sure it doesn't do anything weird.
- **Changed:** Just in case if the player is not on a crafter job, the recipe search will now open. Technically, you shouldn't be able to talk to the NPC/progress the quest in the first place, if you're not a crafter.
- **Fixed:** The check if all materials are available (in your inventory) will now include HQ materials and be calculated correctly when the objective requires you to craft more than one of an item.

### Character Class Switcher

- **Changed:** Now using Dalamuds GameConfig service to detect which button is bound to "Accept".

### Search the markets

- **Fixed:** Item names were not always plain text, causing the search input to bug out.

## [0.13.0] (2023-03-18)

### New Tweak: Auto-open Recipe (Experimental)

When a quest progresses and the new quest step requires an item that can be crafted, and you have all the materials for that item in your inventory, this tweak will automatically open the recipe.

I marked this as experimental because I'm still not 100% sure about some things and have only tested it with a few quests so far (Loporrit daily quests).

### Enhanced Material List

- **Added:** Option to enable the "Search for Item by Crafting Method" context menu entry. No more need to open the recipe tree first.
- **Fixed:** When Zone Names are enabled, it now removes new lines from item names so they can fit in one line.

### DTR

- **Fixed:** It still sometimes didn't display the right instance or busy status, for example after logging out and back in, so I got mad and removed caching of values altogether. Now it just reads the values on each frame.

### Search the markets

- **Added:** Automatically closes the Search Results window when starting a new search.

## [0.12.1] (2023-03-13)

Forgot to update the plugin version in the last release, so please check out the [0.12.0](https://github.com/Haselnussbomber/HaselTweaks/releases/tag/v0.12.0) changes regarding "Refresh Material List", which is now "Enhanced Material List"!

### Enhanced Experience Bar

- **Fixed:** Max Level Override should work again. I confused the type of nodes involved.

### Enhanced Material List

- **Added:** Auto-refresh upon closing for Market Board Search Results and Vendor windows.
- **Added:** Restore Material List on Login: The material list will reopen with the same recipe and quantity each time you log in as long as the window is locked.

## [0.12.0] (2023-03-08)

### Tweak reworked: Refresh Material List -> Enhanced Material List

- **Added:** Enable Zone Names: Displays a zone name underneath the item name indicating where it can be gathered. Only the zone with the lowest teleportation cost is displayed. If the name is green it means it's the current zone. Since space is limited it has to shorten the item and zone name.
  - An option is available to disable this for Crystals.
- **Added:** Enable click to open Map: Allows you to open the map with the gathering marker in said zone.
  - An option is available to disable this for Crystals.
- **Changed:** Auto-refresh Material List/Auto-refresh Recipe Tree separated and converted to options.

### Character Class Switcher

- **Fixed:** The game no longer crashes when checking which button was pressed.  
  _Dev Note:_ This is a result of config IDs changing with each patch. I've reworked the function to find the config option by name and no longer rely on ClientStructs being updated with the correct IDs.

## [0.11.2] (2023-03-07)

Preliminary update for Patch 6.35.

### Lock Window Position

- **Fixed:** Configuration will now save when toggling the enable state of a window lock.  
  This wasn't really worth a hotfix, since the configuration is saved when the plugin unloads (e.g. when closing the game).

## [0.11.1] (2023-03-01)

### Lock Window Position

- **Added:** A config option to invert the logic (locks all windows).
- **Added:** A "Toggle All" button to easily toggle all locked window states.

## [0.11.0] (2023-02-23)

### Tweak reworked: Material Allocation

- **Added:** A config option to enable clicking on gatherable items to open the Sanctuary Gathering Log (enabled by default).
- **Changed:** Since this tweak now does two things, the previous function has been reworked and added as config option. Instead of always selecting the third tab, it now opens the window with the last selected tab, saved between game sessions.

## [0.10.0] (2023-02-22)

### New Tweak: Lock Window Position

Lock window positions so they can't move.

Adds a context menu entry for the title bar to "Lock/Unlock Position" (can be disabled).  
Alternatively it's possible to add windows by using the window picker in the configuration.

### Auto Sorter

- **Fixed:** After clicking the Enable/Disable checkbox, the configuration is saved.

### DTR

- **Fixed:** Changed the conditions for the busy state so that it no longer disappears immediately after switching zones.

### Material Allocation

- **Changed:** Tweak reworked, so that the game doesn't fire an extra network packet on opening the window.

### Search the markets

- **Changed:** Added an orange H for HaselTweaks in front of the context menu entry.

## [0.9.8] (2023-02-14)

### DTR

- **Fixed:** Initial values changed to -1 instead of 0, so it will not show empty DTRs after starting the game/enabling the tweak.

## [0.9.7] (2023-02-13)

### DTR

- **Fixed:** Changed the conditions for the instance indicator so that it no longer hides immediately after being displayed.

## [0.9.6] (2023-02-12)

### Search the markets

- **Added:** Support for items in chat.

### DTR

- **Fixed:** Changed the conditions for the busy state so that it no longer disappears immediately after being displayed.

## [0.9.5] (2023-02-04)

Fix for game crash caused by FFXIVClientStructs changes.

## [0.9.4] (2023-02-02)

### Auto Sorter

- **Added:** It's now possible to disable rules.
- **Fixed:** When text commands are unavailable, for example when placing housing furniture, sorting operations will be skipped.
- **Fixed:** The delete button now also work with the right shift key.

### Enhanced Experience Bar

- **Fixed:** In Update 6.3 the current rank and claimed rank were swapped. Updated the code to reflect the change.

## [0.9.3] (2023-01-31)

### Auto Sorter

- **Added:** To prevent accidentally deleting a rule, you must now hold down the shift key to make the button clickable.
- **Changed:** Run rule buttons are now disabled when the Tweak is disabled or rules are queued.
- **Changed:** When a rule is disabled (for example, when the needed window isn't open), the Run button is disabled instead of displaying an error.
- **Changed:** All in-game armoury sorters are now checked for readiness before sorting.
- **Fixed:** Disabling the tweak now clears the queue.

## [0.9.2] (2023-01-30)

### Auto Sorter

- **Added:** To prevent the right saddlebag sorter from running and returning an error a detection was added whether the player is subscribed to the Companion Premium Service.
- **Added:** To prevent the retainer sorter from running and returning an error a detection was added whether the retainer window is open.
- **Changed:** A queue system was implemented, so the game has time between each frame to complete its sorting.
- **Fixed:** The armoury category and its subcategories will now wait for completion of the in-games sorting mechanism before running.
- **Fixed:** A copy-paste mistake has been corrected for the HQ condition for the Japanese client.

## [0.9.1] (2023-01-26)

### Auto Sorter

- **Changed:** It's now possible to add more rules for the same category, allowing to sort with multiple conditions.
- **Added:** A "Run All" button for easy testing.

## [0.9.0] (2023-01-25)

### New Tweak: Auto Sorter

This one replaces "Auto Sort Armoury Chest" and "Auto Sort Inventory", which didn't last long, in a more general Auto Sorter (hence the name). I just couldn't resist. :D  
Since I can't easily convert the configurations, you have to add rules yourself.

## [0.8.2] (2023-01-25)

### DTR

- **Fixed:** DTRs without data will be hidden again.

### Enhanced Experience Bar

- **Fixed:** Messed up signature for PvP Stats.

## [0.8.1] (2023-01-25)

### DTR

- **Fixed:** FPS should properly appear again.

## [0.8.0] (2023-01-25)

Some internal restructuring and performance optimizations.

### New Tweak: Auto Sort Inventory

Just like "Auto Sort Armoury Chest", but for inventory. ([PR](https://github.com/Haselnussbomber/HaselTweaks/pull/22) by [53m1k0l0n](https://github.com/53m1k0l0n). Thanks!)

## [0.7.14] (2023-01-23)

### Refresh Material List

- **Fixed:** Possible crash fix.

### Scrollable Tabs

- **Fixed:** Minions and Mounts windows should be able to scroll out of favorites again.
- **Changed:** The plugin now uses the game's UI collision system to detect which window is being hovered instead of finding a match based on cursor and window position.

## [0.7.13] (2023-01-20)

### Search the markets

- **Fixed:** Searching via Recipe Tree or Materials List doesn't crash the game anymore.

## [0.7.12] (2023-01-19)

### Scrollable Tabs

- **Added:** Support for Island Minion Guide window.

## [0.7.11] (2023-01-14)

### Enhanced Experience Bar

- **Fixed:** The Experience Bar should now update as intended.

## [0.7.10] (2023-01-14)

### Enhanced Experience Bar

- **Fixed:** Signature update, so PvP Series Bar works again.

## [0.7.9] (2023-01-13)

Additional code to automatically re-scan cached hotfix adresses.

## [0.7.8] (2023-01-13)

Preliminary update for Patch 6.3 Hotfix 1.  
Fixed some internal bugs introduced with the last version.

## [0.7.7] (2023-01-12)

Preliminary update for Patch 6.3.

### Enhanced Experience Bar

- **Changed:** Corrects the name of "PvP Season Bar" to "PvP Series Bar".
  - Configuration is automatically updated.

## [0.7.6] (2022-12-09)

### Aether Current Helper

- **Added:** A new config option to disable centering of the distance column, which _might_ help in case the window keeps expanding endlessly to the right.
- **Fixed:** Corrects the Compass Directions (East/West was swapped).

## [0.7.5] (2022-11-03)

### Aether Current Helper

- **Fixed:** Corrects the Dravanian Forelands quest ids.

## [0.7.4] (2022-10-06)

### Aether Current Helper

- **Fixed:** Some AetherCurrent entries link to the wrong quest. Added 3 of 5 as special case in the plugin. (see issue #15)

## [0.7.3] (2022-09-17)

### Aether Current Helper

- **Fixed:** Window expanding infinitely.

## [0.7.2] (2022-09-04)

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

### Tweak reworked: Series Exp Bar -> Enhanced Experience Bar

- **Added:** Sanctuary Bar for the new Island, because why not?
- **Added:** Max Level Override setting.
  - Will switch to the selected bar if your current job is on max level and none of the other settings apply.
  - _Note:_ Sanctuary Bar is not available as Max Level Override, because data is only loaded once you travel to the island. PvP Series data is always available.
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

The experience bar shows series rank and experience instead. A little \* after the rank indicates a claimable reward.

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

### New Tweak: Scrollable Tabs

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

### Auto Sort Armoury Chest

Automatically runs the shared macro in the third slot when the armoury is opened. Use `/isort` in the macro.

### Character Class Switcher

Always equips the gearset with the highest average item level.

### Chat Timestamp Fixer

At least in the german client the game uses the format `[H:mm]` and it bugged me to have a single digit in the early mornings, so I changed it to `[HH:mm]` and added a space afterwards for better visibility.

### DTR

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

[unreleased]: https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev
[33.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v33.0.1...v33.0.2
[33.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v33.0.0...v33.0.1
[33.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.1.1...v33.0.0
[32.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.1.0...v32.1.1
[32.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.0.4...v32.1.0
[32.0.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.0.3...v32.0.4
[32.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.0.2...v32.0.3
[32.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.0.1...v32.0.2
[32.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v32.0.0...v32.0.1
[32.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v31.0.0...v32.0.0
[31.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v30.0.2...v31.0.0
[30.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v30.0.1...v30.0.2
[30.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v30.0.0...v30.0.1
[30.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v29.0.2...v30.0.0
[29.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v29.0.1...v29.0.2
[29.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v29.0.0...v29.0.1
[29.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v28.0.5...v29.0.0
[28.0.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v28.0.4...v28.0.5
[28.0.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v28.0.3...v28.0.4
[28.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v28.0.2...v28.0.3
[28.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v28.0.1...v28.0.2
[28.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v28.0.0...v28.0.1
[28.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v27.0.3...v28.0.0
[27.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v27.0.2...v27.0.3
[27.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v27.0.1...v27.0.2
[27.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v27.0.0...v27.0.1
[27.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v26.1.1...v27.0.0
[26.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v26.1.0...v26.1.1
[26.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v26.0.2...v26.1.0
[26.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v26.0.1...v26.0.2
[26.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v26.0.0...v26.0.1
[26.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.12...v26.0.0
[25.2.12]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.11...v25.2.12
[25.2.11]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.10...v25.2.11
[25.2.10]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.9...v25.2.10
[25.2.9]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.8...v25.2.9
[25.2.8]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.7...v25.2.8
[25.2.7]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.6...v25.2.7
[25.2.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.5...v25.2.6
[25.2.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.4...v25.2.5
[25.2.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.3...v25.2.4
[25.2.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.2...v25.2.3
[25.2.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.1...v25.2.2
[25.2.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.2.0...v25.2.1
[25.2.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.1.2...v25.2.0
[25.1.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.1.1...v25.1.2
[25.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.1.0...v25.1.1
[25.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v25.0.0...v25.1.0
[25.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.1.3...v25.0.0
[24.1.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.1.2...v24.1.3
[24.1.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.1.1...v24.1.2
[24.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.1.0...v24.1.1
[24.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.0.3...v24.1.0
[24.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.0.2...v24.0.3
[24.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.0.1...v24.0.2
[24.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v24.0.0...v24.0.1
[24.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v23.0.2...v24.0.0
[23.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v23.0.1...v23.0.2
[23.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v23.0.0...v23.0.1
[23.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v22.0.1...v23.0.0
[22.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v22.0.0...v22.0.1
[22.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v21.0.0...v22.0.0
[21.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.4.0...v21.0.0
[20.4.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.3.2...v20.4.0
[20.3.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.3.1...v20.3.2
[20.3.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.3.0...v20.3.1
[20.3.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.2.1...v20.3.0
[20.2.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.2.0...v20.2.1
[20.2.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.1.4...v20.2.0
[20.1.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.1.3...v20.1.4
[20.1.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.1.2...v20.1.3
[20.1.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.1.1...v20.1.2
[20.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.1.0...v20.1.1
[20.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.0.3...v20.1.0
[20.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.0.2...v20.0.3
[20.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.0.1...v20.0.2
[20.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v20.0.0...v20.0.1
[20.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.1.0...v20.0.0
[19.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.0.5...v19.1.0
[19.0.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.0.4...v19.0.5
[19.0.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.0.3...v19.0.4
[19.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.0.2...v19.0.3
[19.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.0.1...v19.0.2
[19.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v19.0.0...v19.0.1
[19.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v18.0.4...v19.0.0
[18.0.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v18.0.3...v18.0.4
[18.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v18.0.2...v18.0.3
[18.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v18.0.1...v18.0.2
[18.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v18.0.0...v18.0.1
[18.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v17.0.2...v18.0.0
[17.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v17.0.1...v17.0.2
[17.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v17.0.0...v17.0.1
[17.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.2.2...v17.0.0
[16.2.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.2.1...v16.2.2
[16.2.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.2.0...v16.2.1
[16.2.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.1.3...v16.2.0
[16.1.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.1.2...v16.1.3
[16.1.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.1.1...v16.1.2
[16.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.1.0...v16.1.1
[16.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.0.4...v16.1.0
[16.0.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.0.3...v16.0.4
[16.0.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.0.2...v16.0.3
[16.0.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.0.1...v16.0.2
[16.0.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v16.0.0...v16.0.1
[16.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.3.3...v16.0.0
[15.3.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.3.2...v15.3.3
[15.3.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.3.1...v15.3.2
[15.3.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.3.0...v15.3.1
[15.3.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.2.0...v15.3.0
[15.2.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.1.5...v15.2.0
[15.1.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.1.4...v15.1.5
[15.1.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.1.3...v15.1.4
[15.1.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.1.2...v15.1.3
[15.1.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.1.1...v15.1.2
[15.1.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.1.0...v15.1.1
[15.1.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v15.0.0...v15.1.0
[15.0.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.12.1...v15.0.0
[14.12.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.12.0...v14.12.1
[14.12.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.11.3...v14.12.0
[14.11.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.11.2...v14.11.3
[14.11.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.11.1...v14.11.2
[14.11.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.11.0...v14.11.1
[14.11.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.10.0...v14.11.0
[14.10.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.9.3...v14.10.0
[14.9.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.9.2...v14.9.3
[14.9.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.9.1...v14.9.2
[14.9.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.9.0...v14.9.1
[14.9.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.8.0...v14.9.0
[14.8.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v14.7.0...v14.8.0
[14.7.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.6...v14.7.0
[0.14.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.5...v0.14.6
[0.14.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.4...v0.14.5
[0.14.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.3...v0.14.4
[0.14.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.2...v0.14.3
[0.14.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.1...v0.14.2
[0.14.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.14.0...v0.14.1
[0.14.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.7...v0.14.0
[0.13.7]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.6...v0.13.7
[0.13.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.5...v0.13.6
[0.13.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.4...v0.13.5
[0.13.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.3...v0.13.4
[0.13.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.2...v0.13.3
[0.13.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.1...v0.13.2
[0.13.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.13.0...v0.13.1
[0.13.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.12.1...v0.13.0
[0.12.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.12.0...v0.12.1
[0.12.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.11.2...v0.12.0
[0.11.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.11.1...v0.11.2
[0.11.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.11.0...v0.11.1
[0.11.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.10.0...v0.11.0
[0.10.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.8...v0.10.0
[0.9.8]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.7...v0.9.8
[0.9.7]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.6...v0.9.7
[0.9.6]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.5...v0.9.6
[0.9.5]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.4...v0.9.5
[0.9.4]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.3...v0.9.4
[0.9.3]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.2...v0.9.3
[0.9.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.1...v0.9.2
[0.9.1]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.8.2...v0.9.0
[0.8.2]: https://github.com/Haselnussbomber/HaselTweaks/compare/v0.8.1...v0.8.2
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
