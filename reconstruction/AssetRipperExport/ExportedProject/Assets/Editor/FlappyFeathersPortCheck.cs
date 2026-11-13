using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEngine;

public static class FlappyFeathersPortCheck
{
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("FlappyFeathersPortCheck: " + reason); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldSave = SaveManager.Instance;
        var playerObj = new GameObject("feathers player"); var managers = new GameObject("feathers managers"); managers.SetActive(false);
        PlayerInventory inv = null; CharacterData character = null;
        try
        {
            var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = managers.AddComponent<GameManager>();
            var save = managers.AddComponent<SaveManager>(); save.config = new ConfigSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            character = ScriptableObject.CreateInstance<CharacterData>(); character.statModifiers = new List<StatModifier>(); inv.characterData = character;
            inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory(); inv.playerStats = new PlayerStatsNew(inv, true);
            var body = playerObj.AddComponent<Rigidbody>(); body.useGravity = false;
            var movement = playerObj.AddComponent<PlayerMovement>(); movement.orientation = playerObj.transform; movement.grounded = true;
            typeof(PlayerMovement).GetField("rb", Private).SetValue(movement, body); typeof(PlayerMovement).GetField("x", Private).SetValue(movement, 1f); typeof(PlayerMovement).GetField("y", Private).SetValue(movement, 1f); typeof(PlayerMovement).GetField("climbCancel", Private).SetValue(movement, 5);
            inv.itemInventory.AddItem(EItem.Feathers, 2); inv.playerStats.Tick();
            Require(Mathf.Abs(inv.playerStats.GetStat(EStat.JumpHeight) - 9.1f) < .001f && inv.playerStats.GetStat(EStat.ExtraJumps) == 2f, "two stacks update jump stats");
            var item = (ItemFlappyFeathers)inv.itemInventory.GetItem(EItem.Feathers); Require(item != null, "item created");
            movement.Jump(); float component = 4.4f / Mathf.Sqrt(2f);
            Require(Mathf.Abs(body.velocity.x - component) < .001f && Mathf.Abs(body.velocity.z - component) < .001f, "real jump gains normalized directional boost");
            inv.itemInventory.RemoveItem(EItem.Feathers); inv.itemInventory.RemoveItem(EItem.Feathers); inv.playerStats.Tick();
            Require(Mathf.Abs(inv.playerStats.GetStat(EStat.JumpHeight) - 7f) < .001f && inv.playerStats.GetStat(EStat.ExtraJumps) == 0f, "last removal restores stats");
            typeof(PlayerMovement).GetField("readyToJump", Private).SetValue(movement, true); body.velocity = Vector3.zero; movement.Jump();
            Require(Mathf.Abs(body.velocity.x) < .001f && Mathf.Abs(body.velocity.z) < .001f, "last removal unsubscribes boost");
            Debug.Log("FlappyFeathersPortCheck passed: real jump event, normalized boost and inventory stats");
        }
        finally
        {
            inv?.playerStats?.OnDestroy(); inv?.statusEffects?.OnDestroy(); inv?.itemInventory?.Cleanup();
            UnityEngine.Object.DestroyImmediate(character); UnityEngine.Object.DestroyImmediate(playerObj); UnityEngine.Object.DestroyImmediate(managers);
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
        }
    }
}
