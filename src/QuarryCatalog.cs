using System;
using System.Collections.Generic;

namespace DadsHunting;

internal sealed class Quarry
{
    internal readonly string Name;
    internal readonly string Prefab;
    internal readonly string Biome;

    internal Quarry(string name, string prefab, string biome)
    {
        Name = name;
        Prefab = prefab;
        Biome = biome;
    }
}

internal static class QuarryCatalog
{
    internal static readonly Quarry[] All =
    {
        new("Boar", "Boar", "Meadows"),
        new("Deer", "Deer", "Meadows / Black Forest"),
        new("Bear", "Bjorn", "Black Forest"),
        new("Wolf", "Wolf", "Mountains"),
        new("Lox", "Lox", "Plains"),
        new("Hare", "Hare", "Mistlands"),
        new("Asksvin", "Asksvin", "Ashlands"),
        new("Moose", "Moose", "Deep North"),
        new("Moose Calf", "Moose_calf", "Deep North"),
        new("Seal", "Seal", "Deep North"),
        new("Baby Seal", "Seal_Pup", "Deep North"),
        new("Crow", "Crow", "Various"),
        new("Gull / Seagull", "Seagal", "Coastal areas"),
        new("Ash Crow", "AshCrow", "Ashlands"),
        new("Neck", "Neck", "Meadows"),
        new("Drake", "Hatchling", "Mountains"),
        new("Volture", "Volture", "Ashlands"),
        new("Bonemaw Serpent", "BonemawSerpent", "Ashlands"),
        new("Sea Serpent", "Serpent", "Ocean")
    };

    internal static Quarry? Find(string prefab)
    {
        foreach (Quarry quarry in All)
            if (string.Equals(quarry.Prefab, prefab, StringComparison.Ordinal)) return quarry;
        return null;
    }

    internal static bool Unlocked(Quarry quarry)
    {
        if (Game.instance == null || ZNetScene.instance == null) return false;
        var profile = Game.instance.GetPlayerProfile();
        var prefab = ZNetScene.instance.GetPrefab(quarry.Prefab);
        if (profile == null || prefab == null || profile.m_playerStats.Length == 0) return false;
        var character = prefab.GetComponent<Character>();
        if (character != null)
        {
            Dictionary<string, float> kills = profile.m_playerStats[0].m_enemyStats[0];
            if (kills.TryGetValue(character.m_name, out float count) && count > 0f) return true;
        }
        return Player.m_localPlayer != null &&
            Player.m_localPlayer.m_customData.ContainsKey("DadsHunting.Killed." + quarry.Prefab);
    }
}
