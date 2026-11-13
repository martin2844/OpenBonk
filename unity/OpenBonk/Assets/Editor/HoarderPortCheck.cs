using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class HoarderPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception("HoarderPortCheck: " + message); }
    static float Field(PassiveAbilityHoarder p, string name) => (float)typeof(PassiveAbilityHoarder).GetField(name, P).GetValue(p);
    public static void Run()
    {
        var root = new GameObject("Hoarder test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm;
        typeof(GameManager).GetField("isPlaying", P).SetValue(gm, true); gm.whatIsGroundAndObjects = 1;
        player.playerMovement = root.AddComponent<PlayerMovement>(); PlayerMovement.Instance = player.playerMovement; var body = root.AddComponent<Rigidbody>(); typeof(PlayerMovement).GetField("rb", P).SetValue(player.playerMovement, body);
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); player.inventory.playerHealth.hp = 100;
        var effect = root.AddComponent<EffectManager>(); EffectManager.Instance = effect; effect.openChestNormal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/OpenChest.prefab"); Require(effect.openChestNormal != null, "original chest prefab");
        var map = ScriptableObject.CreateInstance<MapData>(); map.mapType = EMapType.ProceduralMesh; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
        MapInfo.mapBoundsLower = new Vector3(-100f, 0f, -100f); MapInfo.mapBoundsUpper = new Vector3(100f, 0f, 100f);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.localScale = new Vector3(200f, 1f, 200f); floor.transform.position = Vector3.down * .5f; Physics.SyncTransforms();
        var p = new PassiveAbilityHoarder(); var tick = typeof(PassiveAbilityHoarder).GetMethod("ChestTick", P);
        var names = (HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        GameObject chest = null;
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = p.GetDescription(new LocalizedString("PassiveAbilities", "HOARDER_DESC"));
            Require(description.Contains("60") && description.Contains("120") && !description.Contains("{"), "localized chest intervals: " + description);
            p.Init(); MyTime.paused = false; MyTime.deltaTime = .2f; MyTime.fixedDeltaTime = .02f;
            tick.Invoke(p, null); Require(Mathf.Abs(Field(p, "currentTimer") - 59.98f) < .001f, "countdown uses physics time, not frame time");
            p.Init(); MyTime.paused = true; MyTime.deltaTime = 20f; MyTime.fixedDeltaTime = 20f; tick.Invoke(p, null); Require(Field(p, "currentTimer") == 60f, "paused timer");
            MyTime.paused = false; names.Add("no_items"); tick.Invoke(p, null); names.Remove("no_items"); Require(Field(p, "currentTimer") == 60f, "no-items challenge");
            MyTime.time = 10f; MyTime.fixedDeltaTime = 59f; tick.Invoke(p, null); Require(Field(p, "currentTimer") == 1f, "initial countdown");
            MyTime.fixedDeltaTime = 1f; tick.Invoke(p, null); chest = GameObject.Find(effect.openChestNormal.name + "(Clone)");
            Require(chest != null && Mathf.Abs(chest.transform.position.y) < .001f, "chest placed on ground");
            Require(Field(p, "spawnIntervalSeconds") == 62.5f && Field(p, "currentTimer") == 62.5f, "interval increases after chest");
            Vector3 facing = player.transform.position - chest.transform.position; facing.y = 0f; Require(Vector3.Dot(chest.transform.forward, facing.normalized) > .999f, "chest faces player");
            Debug.Log("HoarderPortCheck passed: challenge and pause gates, chest countdown, ground placement and facing");
        }
        finally { names.Remove("no_items"); if (chest != null) UnityEngine.Object.DestroyImmediate(chest); UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; PlayerMovement.Instance = null; GameManager.Instance = null; EffectManager.Instance = null; MyTime.paused = false; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { null }); }
    }
}
