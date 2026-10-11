# DadsHunting

DadsHunting adds a target emblem beside the PvP swords in Valheim's inventory header. Select one previously killed quarry from the **Select your Quarry** menu. The list is grouped by biome and contains the 19 prefab names specified for this mod. Each row shows the in-game trophy icon, or a feather for birds, and the selected row uses Valheim's red checked-map-pin mark.

Valheim's saved character kill statistics unlock ordinary creatures, including kills made before installing DadsHunting. Birds and other qualifying objects without a `Character` kill statistic unlock after a qualifying kill made while DadsHunting is installed; earlier kills of those objects cannot be recovered from the game's saved statistics. The selection is saved in the character's existing custom data.

The radar inspects Valheim's nearby spawned object records for the selected quarry's prefab and checks that each record has a live game object. When a selected quarry is within **Detection Range** (50 meters by default), a temporary native raid area circle appears on the minimap at the live object's position and moves with it. **Circle Diameter** defaults to 25 meters. Entering the circle starts a 30 second timer; the circle then disappears. The same creature can be marked again after the player leaves its marked circle. Only one quarry type can be selected at a time. The marker is not saved to the world map.

Build with `dotnet build DadsHunting.csproj -c Release`. Install `bin/Release/net48/DadsHunting.dll` under a `BepInEx/plugins/DadsHunting/` folder. The project targets Valheim 1.0.12 and BepInEx 5.4.23.5 using the same local references as the adjacent Dads mods. Settings are in `BepInEx/config/com.dadisbored.dadshunting.cfg`.
