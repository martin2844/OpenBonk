using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class CampfireHealthIntegrationCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("CampfireHealthIntegrationCheck: " + message); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldEffect = EffectManager.Instance;
        float oldTime = MyTime.time, oldFixedDelta = MyTime.fixedDeltaTime; bool oldPaused = MyTime.paused;
        var root = new GameObject("campfire integration player"); root.SetActive(false);
        var managers = new GameObject("campfire integration managers"); managers.SetActive(false);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PlayerInventory inv = null; CharacterData character = null; Campfire fire = null;
        try
        {
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.playerRenderer = root.AddComponent<PlayerRenderer>(); root.transform.position = new Vector3(10, 1, 10);
            var game = managers.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << ground.layer;
            var effects = managers.AddComponent<EffectManager>(); EffectManager.Instance = effects;
            effects.campfire = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Campfire.prefab");
            Require(effects.campfire != null, "original prefab");
            ground.transform.position = new Vector3(10, -.5f, 14); ground.transform.localScale = new Vector3(20, 1, 20); Physics.SyncTransforms();
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            character = ScriptableObject.CreateInstance<CharacterData>(); character.statModifiers = new List<StatModifier>(); inv.characterData = character;
            inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory();
            inv.playerStats = new PlayerStatsNew(inv, true); inv.playerHealth = new PlayerHealth(inv.playerStats); inv.playerHealth.hp = 50;
            MyTime.paused = false; MyTime.fixedDeltaTime = .02f; MyTime.time = 10f;
            inv.itemInventory.AddItem(EItem.Campfire); var item = (ItemCampfire)inv.itemInventory.GetItem(EItem.Campfire);
            inv.itemInventory.Tick(); MyTime.time = 10.61f; inv.itemInventory.Tick(); fire = item.campfire;
            Require(fire != null && fire.gameObject.activeSelf, "actual prefab spawned");
            inv.playerStats.Tick();
            Require(Mathf.Abs(inv.playerStats.GetStat(EStat.HealthRegen) - 1110f) < .001f, "bonus reaches computed stats");
            for (int i = 0; i < 50; i++) { MyTime.time += .02f; inv.playerHealth.Tick(); }
            Require(inv.playerHealth.hp >= 67 && inv.playerHealth.hp <= 69, "one second of enhanced regeneration heals player");
            int healedHp = inv.playerHealth.hp;
            root.transform.position += Vector3.right * 2f; inv.itemInventory.Tick(); inv.playerStats.Tick();
            Require(Mathf.Abs(inv.playerStats.GetStat(EStat.HealthRegen) - 10f) < .001f, "moving restores base regeneration");
            for (int i = 0; i < 50; i++) { MyTime.time += .02f; inv.playerHealth.Tick(); }
            Require(inv.playerHealth.hp == healedHp, "enhanced healing stops after leaving camp");
            Debug.Log("CampfireHealthIntegrationCheck passed: actual prefab, inventory, stats and player health");
        }
        finally
        {
            inv?.playerHealth?.OnDestroy(); inv?.playerStats?.OnDestroy(); inv?.statusEffects?.OnDestroy(); inv?.itemInventory?.Cleanup();
            if (fire != null) { if (fire.createFx != null && fire.createFx.transform.parent == null) UnityEngine.Object.DestroyImmediate(fire.createFx.gameObject); UnityEngine.Object.DestroyImmediate(fire.gameObject); }
            UnityEngine.Object.DestroyImmediate(character); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(managers); UnityEngine.Object.DestroyImmediate(root);
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; EffectManager.Instance = oldEffect; MyTime.time = oldTime; MyTime.fixedDeltaTime = oldFixedDelta; MyTime.paused = oldPaused;
        }
    }
}
