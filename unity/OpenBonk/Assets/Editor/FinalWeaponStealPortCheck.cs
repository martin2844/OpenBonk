using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class FinalWeaponStealPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool c, string m) { if (!c) throw new Exception("FinalWeaponStealPortCheck: " + m); }
    static void Set(object o, string f, object v) => o.GetType().GetField(f, Private).SetValue(o, v);
    static T Get<T>(object o, string f) => (T)o.GetType().GetField(f, Private).GetValue(o);
    static void Call(object o, string m) => o.GetType().GetMethod(m, Private).Invoke(o, null);
    static WeaponBase Add(WeaponInventory inventory, string name) {
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/" + name + ".asset"); Check(data != null, name + " asset");
        var weapon = (WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); weapon.weaponData = data; weapon.Enable(); inventory.weapons.Add(data.eWeapon, weapon); return weapon;
    }
    public static void Run()
    {
        var root = new GameObject("steal fixture"); root.SetActive(false); var fight = root.AddComponent<FinalFightController>();
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var inventory = new WeaponInventory(); player.inventory.weaponInventory = inventory;
        var first = Add(inventory, "WeaponSword"); var second = Add(inventory, "WeaponShotgun"); var third = Add(inventory, "WeaponPoisonFlask");
        var enemy = new GameObject("boss").AddComponent<Enemy>(); fight.boss = enemy;
        int toggles = 0; Action<WeaponBase> onToggle = _ => toggles++; WeaponInventory.A_WeaponToggled += onToggle;
        try {
            Set(fight, "numWeaponsToTake", 3); Set(fight, "takeWeaponAtTime", 4f); Set(fight, "weaponTakeInterval", 3f);
            MyTime.time = 3.99f; Call(fight, "TakeWeapons"); Check(first.enabled && second.enabled && third.enabled && toggles == 0, "no early theft");
            MyTime.time = 4f; Call(fight, "TakeWeapons"); Check(first.enabled && second.enabled && !third.enabled && toggles == 1, "last weapon taken first");
            MyTime.time = 6.99f; Call(fight, "TakeWeapons"); Check(second.enabled && toggles == 1, "three-second interval");
            MyTime.time = 7f; Call(fight, "TakeWeapons"); Check(first.enabled && !second.enabled && !third.enabled && toggles == 2, "second weapon taken");
            MyTime.time = 10f; Call(fight, "TakeWeapons"); Check(first.enabled && toggles == 2 && Get<int>(fight, "numWeaponsTaken") == 3, "first weapon protected and attempt counted");
            Call(fight, "PylonsDone"); Check(second.enabled && !third.enabled && toggles == 3, "first disabled weapon returned");
            MyTime.time = 100f; Call(fight, "TakeWeapons"); Check(second.enabled && toggles == 3, "theft stops after original count");
            Call(fight, "PylonsDone"); Check(third.enabled && toggles == 4, "next disabled weapon returned");
            Call(fight, "PylonsDone"); Check(toggles == 4, "no extra return");
            Debug.Log("FinalWeaponStealPortCheck passed: timing, inventory order, protected first weapon and pylon returns");
        } finally { WeaponInventory.A_WeaponToggled -= onToggle; MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(enemy.gameObject); UnityEngine.Object.DestroyImmediate(root); }
    }
}
