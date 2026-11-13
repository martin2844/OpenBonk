using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class FloatingPortCheck
{
    static void Near(float actual, float expected, string message)
    {
        if (Mathf.Abs(actual - expected) > .0001f) throw new Exception(message + ": " + actual + " expected " + expected);
    }

    public static void Run()
    {
        var root = new GameObject("Floating test"); root.SetActive(false);
        root.transform.position = Vector3.up;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 20 };
        var passive = new PassiveAbilityFloating(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        player.playerInput = root.AddComponent<PlayerInput>();
        player.playerMovement = root.AddComponent<PlayerMovement>();
        var body = root.AddComponent<Rigidbody>();
        typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player.playerMovement, body);
        typeof(PlayerMovement).GetField("playerHeight", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player.playerMovement, 2f);
        player.playerMovement.whatIsGround = 1;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(0f, -.5f, 0f); Physics.SyncTransforms();
        try
        {
            float baseline = inventory.playerStats.GetRawStat(EStat.DamageMultiplier);
            MyTime.time = 0f; passive.Tick(); inventory.playerStats.Tick(); Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseline, "grounded");
            root.transform.position = Vector3.up * 10f; Physics.SyncTransforms();
            MyTime.time = .25f; passive.Tick(); inventory.playerStats.Tick(); Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseline, "cooldown");
            MyTime.time = .5f; passive.Tick(); inventory.playerStats.Tick(); Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseline + .2f, "airborne level 20");
            inventory.playerXp.level = 30; MyTime.time = 1f; passive.Tick(); inventory.playerStats.Tick(); Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseline + .3f, "airborne level 30");
            root.transform.position = Vector3.up; Physics.SyncTransforms(); MyTime.time = 1.5f; passive.Tick(); inventory.playerStats.Tick(); Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseline, "landed");
            Debug.Log("FloatingPortCheck passed: grounded, airborne scaling, cooldown and landing");
        }
        finally { inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(floor); MyPlayer.Instance = null; }
    }
}
