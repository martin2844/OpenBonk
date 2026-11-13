using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEditor;
using UnityEngine;

public static class ItemChangeEffectPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string msg) { if (!ok) throw new Exception("ItemChangeEffectPortCheck: " + msg); }
    static ChestItem[] Displays() => UnityEngine.Object.FindObjectsOfType<ChestItem>();
    public static void Run() {
        var root = new GameObject("item change fixture"); root.SetActive(false); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(2,3,4);
        var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
        var data = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset"); var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChestItem.prefab");
        Require(data != null && prefab != null, "original assets");
        typeof(DataManager).GetField("itemData", P).SetValue(dataManager, new Dictionary<EItem,ItemData> { [EItem.Clover] = data });
        var effectManager = root.AddComponent<EffectManager>(); effectManager.chestPickup = prefab; typeof(EffectManager).GetMethod("Awake", P).Invoke(effectManager, null);
        var inventory = new ItemInventory();
        try {
            inventory.AddItem(EItem.Clover); var displays = Displays(); Require(displays.Length == 1, "added event creates display"); var gain = displays[0];
            Require(gain.icon.texture == data.icon && gain.t_amountText.text == "1" && gain.t_amountText.color == Color.white && gain.audioSource.clip == gain.clipPositive, "gain icon, amount, color and audio");
            Require(Vector3.Distance(gain.transform.position, player.transform.position + player.transform.up * 1.5f) < .001f, "gain position");
            inventory.RemoveItem(EItem.Clover, false); Require(Displays().Length == 1, "silent removal has no display");
            inventory.AddItem(EItem.Clover); inventory.RemoveItem(EItem.Clover, true); displays = Displays();
            var loss = displays.Single(x => x.t_amountText.text == "-1"); Require(displays.Length == 3 && loss.icon.texture == data.icon && loss.t_amountText.color == Color.red && loss.audioSource.clip == loss.clipNegative, "loss icon, amount, color and audio");
            typeof(EffectManager).GetMethod("OnDestroy", P).Invoke(effectManager, null); inventory.AddItem(EItem.Clover); Require(Displays().Length == 3, "cleanup unsubscribes");
            Debug.Log("ItemChangeEffectPortCheck passed: real inventory events, original prefab, positive/negative and silent removal");
        } finally {
            inventory.Cleanup(); foreach (var display in Displays()) UnityEngine.Object.DestroyImmediate(display.gameObject);
            EffectManager.Instance = null; DataManager.Instance = null; MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
