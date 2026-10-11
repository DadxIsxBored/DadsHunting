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
    public const string Version = "1.0.5";
    private const string SelectionKey = "DadsHunting.SelectedQuarry";
    private Harmony? _harmony;
    private ConfigEntry<float> _detectionRange = null!;
    private ConfigEntry<float> _circleDiameter = null!;
    private float _nextScan;
    private Player? _player;
    private string _selection = "";
    private ZDOID _targetId = ZDOID.None;
    private readonly Dictionary<ZDOID, Vector3> _completed = new();
    private readonly List<ZDOID> _releaseCompleted = new();
    private readonly List<ZDO> _nearObjects = new();
    private readonly List<ZDO> _farObjects = new();
    private static readonly System.Reflection.FieldInfo? PinUpdateRequired =
        AccessTools.Field(typeof(Minimap), "m_pinUpdateRequired");
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
        float circleRadius2 = _circleDiameter.Value * _circleDiameter.Value * 0.25f;
        ZDOMan? zdoMan = ZDOMan.instance;
        ZNetScene? scene = ZNetScene.instance;
        if (zdoMan == null || scene == null || ZoneSystem.instance == null)
        {
            ClearTarget();
            return;
        }

        _nearObjects.Clear();
        _farObjects.Clear();
        zdoMan.FindSectorObjects(ZoneSystem.GetZone(origin), SimulationDistance.OriginalDistance,
            _nearObjects, _farObjects);
        int prefabHash = _selection.GetStableHashCode();
        _releaseCompleted.Clear();
        foreach (KeyValuePair<ZDOID, Vector3> completed in _completed)
        {
            ZDO? previous = zdoMan.GetZDO(completed.Key);
            if (previous == null || !TryGetLivePosition(previous, scene, prefabHash, out Vector3 position) ||
                DistanceXZ(origin, position) > circleRadius2)
                _releaseCompleted.Add(completed.Key);
        }
        foreach (ZDOID id in _releaseCompleted) _completed.Remove(id);

        ZDO? target = _targetId == ZDOID.None ? null : zdoMan.GetZDO(_targetId);
        Vector3 targetPosition = default;
        if (target != null && (!TryGetLivePosition(target, scene, prefabHash, out targetPosition) ||
            DistanceXZ(origin, targetPosition) > range2)) target = null;
        if (target == null) ClearTarget();

        if (target == null)
        {
            float nearest = range2;
            foreach (ZDO candidate in _nearObjects)
            {
                if (_completed.ContainsKey(candidate.m_uid) ||
                    !TryGetLivePosition(candidate, scene, prefabHash, out Vector3 position)) continue;
                float distance = DistanceXZ(origin, position);
                if (distance > nearest) continue;
                nearest = distance;
                target = candidate;
                targetPosition = position;
            }
            if (target != null)
            {
                _targetId = target.m_uid;
                _enteredAt = -1f;
            }
        }

        if (target == null) return;
        _circleCenter = targetPosition;
        if (_pin == null || _pinMap != map)
        {
            ClearPin();
            _pinMap = map;
            _pin = map.AddPin(_circleCenter, Minimap.PinType.EventArea, "", false, false, 0L);
        }
        _pin.m_pos = _circleCenter;
        _pin.m_worldSize = _circleDiameter.Value;
        PinUpdateRequired?.SetValue(map, true);
        if (DistanceXZ(origin, _circleCenter) <= circleRadius2)
        {
            if (_enteredAt < 0f) _enteredAt = Time.time;
        }
        if (_enteredAt >= 0f && Time.time - _enteredAt >= 30f)
        {
            _completed[_targetId] = _circleCenter;
            ClearTarget();
        }
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x, z = a.z - b.z;
        return x * x + z * z;
    }

    private static bool TryGetLivePosition(ZDO zdo, ZNetScene scene, int prefabHash, out Vector3 position)
    {
        position = default;
        if (!zdo.IsValid() || zdo.GetPrefab() != prefabHash || zdo.GetBool(ZDOVars.s_dead)) return false;
        ZNetView? view = scene.FindInstance(zdo);
        if (view == null || !view.IsValid()) return false;
        Character? character = view.GetComponent<Character>();
        if (character != null && character.GetHealth() <= 0f) return false;
        position = view.transform.position;
        return true;
    }

    private void ClearTarget()
    {
        ClearPin();
        _targetId = ZDOID.None;
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

[HarmonyPatch(typeof(InventoryGui), "Update")]
internal static class HuntingInventoryClosePatch
{
    private static bool Prefix(InventoryGui __instance)
    {
        HuntingMenu? menu = __instance.GetComponent<HuntingMenu>();
        if (menu == null || !menu.IsOpen) return true;
        if (!ZInput.GetButtonDown("JoyButtonB") && !ZInput.GetKeyDown(KeyCode.Escape)) return true;
        menu.Close();
        return false;
    }
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
