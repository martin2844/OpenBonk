using System;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class CampfirePortCheck
{
    static void Require(bool value, string reason) { if (!value) throw new Exception("CampfirePortCheck: " + reason); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; float oldTime = MyTime.time;
        var playerObject = new GameObject("campfire player"); playerObject.SetActive(false);
        var gameObject = new GameObject("campfire manager"); gameObject.SetActive(false);
        var visual = new GameObject("campfire fixture"); visual.SetActive(false);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Campfire.prefab");
            var original = prefab != null ? prefab.GetComponent<Campfire>() : null;
            Require(original != null && original.createFx != null && original.audioSource != null && original.randomSfx != null, "original campfire prefab bindings");
            var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.playerRenderer = playerObject.AddComponent<PlayerRenderer>();
            playerObject.transform.position = new Vector3(10f, 1f, 10f);
            var game = gameObject.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << ground.layer;
            ground.transform.position = new Vector3(10f, -.5f, 14f); ground.transform.localScale = new Vector3(20f, 1f, 20f); Physics.SyncTransforms();
            var inventory = new ItemInventory(); inventory.AddItem(EItem.Campfire, 2); var item = (ItemCampfire)inventory.GetItem(EItem.Campfire);
            item.campfire = visual.AddComponent<Campfire>();
            MyTime.time = 10f; inventory.Tick();
            Require(!visual.activeSelf && item.statModifiers.Count == 0, "moving establishes setup delay");
            MyTime.time = 10.6f; inventory.Tick(); Require(!visual.activeSelf, "delay boundary");
            MyTime.time = 10.61f; inventory.Tick();
            Require(visual.activeSelf && Vector3.Distance(visual.transform.position, new Vector3(10f, 0f, 14f)) < .01f, "campfire placed four units ahead on ground");
            var modifier = item.statModifiers[EStat.HealthRegen].GetModifiers().Single();
            Require(modifier.modifyType == EStatModifyType.Flat && modifier.modification == 2200f, "two stacks grant flat regeneration");
            playerObject.transform.position += Vector3.right * 2f; MyTime.time = 11f; inventory.Tick();
            Require(item.statModifiers[EStat.HealthRegen].GetModifiers().Single().modification == 0f, "movement removes regeneration immediately");
            MyTime.time = 11.61f; inventory.Tick();
            Require(visual.activeSelf && item.statModifiers[EStat.HealthRegen].GetModifiers().Single().modification == 2200f, "stationary again reuses camp");
            item.Cleanup(); inventory.Cleanup(); Require(item.statModifiers[EStat.HealthRegen].GetModifiers().Single().modification == 0f, "cleanup clears regeneration");
            Debug.Log("CampfirePortCheck passed: setup, ground placement, regeneration, movement and cleanup");
        }
        finally
        {
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; MyTime.time = oldTime;
            UnityEngine.Object.DestroyImmediate(visual); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(gameObject); UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
