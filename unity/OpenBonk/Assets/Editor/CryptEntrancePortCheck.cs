using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;

public static class CryptEntrancePortCheck
{
    public static void Run()
    {
        var root = new GameObject("crypt entrance check"); root.SetActive(false);
        MyPlayer oldPlayer = MyPlayer.Instance;
        ItemInventory inventory = null;
        try
        {
            MyPlayer.Instance = root.AddComponent<MyPlayer>();
            MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            inventory = new ItemInventory(); MyPlayer.Instance.inventory.itemInventory = inventory;
            var crypt = root.AddComponent<InteractableCrypt>();
            var open = typeof(InteractableCrypt).GetMethod("IsOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(crypt.CanInteract() && !(bool)open.Invoke(crypt, null), "no keys");
            var key = ItemFactory.CreateItem(EItem.CryptKey, inventory); key.amount = 3; inventory.items.Add(EItem.CryptKey, key);
            Check(!(bool)open.Invoke(crypt, null), "three keys");
            key.amount = 4; Check((bool)open.Invoke(crypt, null), "four keys");
            key.amount = 5; Check((bool)open.Invoke(crypt, null), "five keys");
            typeof(InteractableCrypt).GetField("isDone", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(crypt, true);
            Check(!crypt.CanInteract() && !crypt.Interact(), "completed entrance cannot be reused");
            Debug.Log("CryptEntrancePortCheck passed");
        }
        finally { inventory?.Cleanup(); MyPlayer.Instance = oldPlayer; UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Check(bool value, string name) { if (!value) throw new Exception("CryptEntrancePortCheck failed: " + name); }
}
