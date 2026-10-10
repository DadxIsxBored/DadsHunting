using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DadsHunting;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("valheim.exe")]
public sealed class DadsHuntingPlugin : BaseUnityPlugin
{
    public const string Guid = "com.dadisbored.dadshunting";
    public const string Name = "DadsHunting";
    public const string Version = "1.0.1";
    private const string SelectionKey = "DadsHunting.SelectedQuarry";
    private Harmony? _harmony;
    private ConfigEntry<float> _detectionRange = null!;
    private ConfigEntry<float> _circleDiameter = null!;
    private float _nextScan;
    private Player? _player;
    private string _selection = "";
    private Component? _target;
    private readonly HashSet<Component> _completed = new();
    private Minimap.PinData? _pin;
    private Minimap? _pinMap;
    private Vector3 _circleCenter;
    private float _enteredAt = -1f;

    internal static DadsHuntingPlugin? Instance { get; private set; }
    internal string Selection => _selection;

    private void Awake()
    {
        Instance = this;
        _detectionRange = Config.Bind("Radar", "Detection Range", 50f,
            new ConfigDescription("Distance in meters at which selected quarry appears on the map.", new AcceptableValueRange<float>(1f, 1000f)));
        _circleDiameter = Config.Bind("Radar", "Circle Diameter", 25f,
            new ConfigDescription("Diameter in meters of the hunting area.", new AcceptableValueRange<float>(2f, 500f)));
        _harmony = new Harmony(Guid);
        _harmony.PatchAll(typeof(DadsHuntingPlugin).Assembly);
    }

    internal void Select(Quarry? quarry)
    {
        string next = quarry?.Prefab ?? "";
        if (_selection == next) return;
        _selection = next;
        _completed.Clear();
        ClearTarget();
        if (_player != null) _player.m_customData[SelectionKey] = _selection;
    }

    private void Update()
    {
        Player? player = Player.m_localPlayer;
        if (player != _player)
        {
            ClearTarget();
            _completed.Clear();
            _player = player;
            _selection = player != null && player.m_customData.TryGetValue(SelectionKey, out string saved) &&
                QuarryCatalog.Find(saved) != null ? saved : "";
        }
        if (player == null || Minimap.instance == null || string.IsNullOrEmpty(_selection))
        {
            ClearTarget();
            return;
        }
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + 0.5f;
        Scan(player, Minimap.instance);
    }

    private void Scan(Player player, Minimap map)
    {
        Quarry? selected = QuarryCatalog.Find(_selection);
        if (selected == null || !QuarryCatalog.Unlocked(selected))
        {
            ClearTarget();
            return;
        }
        Vector3 origin = player.transform.position;
        float range2 = _detectionRange.Value * _detectionRange.Value;
        var characters = Character.GetAllCharacters();
        _completed.RemoveWhere(component => component == null ||
            DistanceXZ(origin, component.transform.position) > range2);

        if (_target != null && (!Alive(_target) ||
            DistanceXZ(origin, _target.transform.position) > range2 ||
            Utils.GetPrefabName(_target.gameObject) != _selection)) ClearTarget();

        if (_target == null)
        {
            float nearest = range2;
            foreach (Character character in characters)
            {
                if (character == null || _completed.Contains(character) || character.GetHealth() <= 0f ||
                    Utils.GetPrefabName(character.gameObject) != _selection) continue;
                float distance = DistanceXZ(origin, character.transform.position);
                if (distance > nearest) continue;
                nearest = distance;
                _target = character;
            }
            foreach (IMonoUpdater updater in RandomFlyingBird.Instances)
            {
                if (updater is not RandomFlyingBird bird || bird == null || _completed.Contains(bird) ||
                    Utils.GetPrefabName(bird.gameObject) != _selection) continue;
                float distance = DistanceXZ(origin, bird.transform.position);
                if (distance > nearest) continue;
                nearest = distance;
                _target = bird;
            }
            if (_target != null)
            {
                _circleCenter = _target.transform.position;
                _enteredAt = -1f;
            }
        }

        if (_target == null) return;
        if (_pin == null || _pinMap != map)
        {
            ClearPin();
            _pinMap = map;
            _pin = map.AddPin(_circleCenter, Minimap.PinType.EventArea, "", false, false, 0L);
        }
        _pin.m_pos = _circleCenter;
        _pin.m_worldSize = _circleDiameter.Value;
        if (DistanceXZ(origin, _circleCenter) <= _circleDiameter.Value * _circleDiameter.Value * 0.25f)
        {
            if (_enteredAt < 0f) _enteredAt = Time.time;
            if (Time.time - _enteredAt >= 30f)
            {
                _completed.Add(_target);
                ClearTarget();
            }
        }
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x, z = a.z - b.z;
        return x * x + z * z;
    }

    private static bool Alive(Component component) => component is not Character character || character.GetHealth() > 0f;

    private void ClearTarget()
    {
        ClearPin();
        _target = null;
        _enteredAt = -1f;
    }

    private void ClearPin()
    {
        if (_pin != null && _pinMap != null) _pinMap.RemovePin(_pin);
        _pin = null;
        _pinMap = null;
    }

    private void OnDestroy()
    {
        ClearTarget();
        _harmony?.UnpatchSelf();
        if (Instance == this) Instance = null;
    }
}

[HarmonyPatch(typeof(InventoryGui), "Awake")]
internal static class HuntingInventoryPatch
{
    private static void Postfix(InventoryGui __instance) => __instance.gameObject.AddComponent<HuntingMenu>();
}

[HarmonyPatch(typeof(Destructible), "Destroy", new[] { typeof(HitData) })]
internal static class HuntingDestructiblePatch
{
    private static void Prefix(Destructible __instance, HitData hit)
    {
        if (hit?.GetAttacker() != Player.m_localPlayer || Player.m_localPlayer == null) return;
        string prefab = Utils.GetPrefabName(__instance.gameObject);
        if (QuarryCatalog.Find(prefab) == null) return;
        Player.m_localPlayer.m_customData["DadsHunting.Killed." + prefab] = "1";
    }
}
