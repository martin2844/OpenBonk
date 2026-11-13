using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

public static class AmbienceAndLevelPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("AmbienceAndLevelPortCheck: " + message); }
    static void Invoke(object o, string name) => o.GetType().GetMethod(name, P).Invoke(o, null);
    public static void Run()
    {
        var root = new GameObject("ambience and level check"); root.SetActive(false);
        var map = ScriptableObject.CreateInstance<MapData>();
        var mapClip = AudioClip.Create("map", 44100, 1, 44100, false);
        var dungeonClip = AudioClip.Create("dungeon", 44100, 1, 44100, false);
        var mapProperty = typeof(MapController).GetProperty("currentMap"); var oldMap = mapProperty.GetValue(null);
        var oldStarted = GameManager.A_DungeonStarted; var oldEnded = GameManager.A_DungeonEnded;
        var names = (HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        try
        {
            map.ambience = mapClip; mapProperty.GetSetMethod(true).Invoke(null, new object[] { map });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var ambience = root.AddComponent<AmbienceController>(); ambience.audioSource = root.AddComponent<AudioSource>(); ambience.dungeonAmbience = dungeonClip;
            Invoke(ambience, "Awake"); Invoke(ambience, "Start"); Check(ambience.audioSource.clip == mapClip, "map clip at start");
            GameManager.A_DungeonStarted?.Invoke(); Check(ambience.audioSource.clip == dungeonClip, "crypt clip on event");
            GameManager.A_DungeonEnded?.Invoke(); Check(ambience.audioSource.clip == mapClip, "map clip after event");
            typeof(GameManager).GetProperty("isCrypt").GetSetMethod(true).Invoke(game, new object[] { true }); Invoke(ambience, "Start"); Check(ambience.audioSource.clip == dungeonClip, "starting in crypt");
            Invoke(ambience, "OnDestroy"); Check(GameManager.A_DungeonStarted == oldStarted && GameManager.A_DungeonEnded == oldEnded, "event cleanup");
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.playerXp = new PlayerXp { xp = 11 }; inv.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); inv.playerHealth.hp = 100;
            XpUtility.Init(); var pending = typeof(PlayerInventory).GetField("pendingXp", P);
            inv.AddLevel(); int expected = XpUtility.XpToNextLevelTotal(11); Check((int)pending.GetValue(inv) == expected, "full current-level XP queued");
            inv.playerHealth.hp = 0; inv.AddLevel(); Check((int)pending.GetValue(inv) == expected, "dead player cannot receive XP");
            inv.playerHealth.hp = 100; names.Add("no_xp"); inv.AddLevel(); Check((int)pending.GetValue(inv) == expected, "no-XP challenge gate"); names.Remove("no_xp");
            Debug.Log("AmbienceAndLevelPortCheck passed: map/crypt events, initial crypt, cleanup and queued XP gates");
        }
        finally
        {
            names.Remove("no_xp"); GameManager.A_DungeonStarted = oldStarted; GameManager.A_DungeonEnded = oldEnded; GameManager.Instance = null; MyPlayer.Instance = null;
            mapProperty.GetSetMethod(true).Invoke(null, new[] { oldMap }); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(mapClip); UnityEngine.Object.DestroyImmediate(dungeonClip);
        }
    }
}
