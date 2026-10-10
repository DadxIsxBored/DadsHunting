# Changelog

## 1.0.4

- Find selected quarry through Valheim's nearby spawned object records and track it by ZDO ID.
- Verify the spawned record has a live game object, move the native map circle with its current position, and remove it 30 seconds after entry even if the quarry moves.

## 1.0.3

- Reset a completed quarry when the player leaves its marked circle, allowing the radar to mark it again while it remains within detection range.

## 1.0.2

- Replaced the custom popup with a native Skills dialog clone, including its frame, scrolling area, heading style, and close button.
- Replaced the outlined target with a filled gold hunting arrow sized for the inventory header.
- Expanded the header to give the added emblem room and used native dialog dimensions for the quarry list.

## 1.0.1

- Removed the square behind the inventory target emblem and aligned it with the native header icons.
- Moved the quarry menu to the inventory overlay so it draws above the crafting area.

## 1.0.0

- Added a target emblem and biome-grouped quarry menu to the inventory header.
- Added the 19 specified animal prefab identifiers.
- Used saved character kill records to unlock quarry where Valheim stores those records.
- Added tracking for qualifying destructible animals killed while the mod is installed.
- Added configurable detection range and raid-area circle diameter.
- Added a temporary map circle that disappears 30 seconds after entry.
- Added Thunderstore package and original icon artwork.
