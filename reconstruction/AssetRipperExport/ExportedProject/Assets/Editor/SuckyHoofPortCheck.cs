using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Utility;
using UnityEngine;

public static class SuckyHoofPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SuckyHoofPortCheck: " + message); }
    static Pickup CreatePickup(string name, EPickup type, Vector3 pos, int layer)
    {
        var go = new GameObject(name); go.SetActive(false); go.layer = layer; go.transform.position = pos;
        var collider = go.AddComponent<SphereCollider>(); var pickup = go.AddComponent<Pickup>(); pickup.collider = collider; pickup.ePickup = type; go.SetActive(true); return pickup;
    }
    public static void Run()
    {
        var playerGo = new GameObject("test player"); playerGo.SetActive(false);
        var stateGo = new GameObject("test state"); stateGo.SetActive(false);
        var prefab = new GameObject("magnet visual"); prefab.SetActive(false);
        Pickup near = null, far = null, gold = null;
        try
        {
            var player = playerGo.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var manager = stateGo.AddComponent<PickupManager>(); PickupManager.Instance = manager; manager.whatIsPickups = 1 << 8;
            var effect = stateGo.AddComponent<EffectManager>(); EffectManager.Instance = effect; effect.magnetFx = prefab;
            near = CreatePickup("near xp", EPickup.Xp, new Vector3(149,0,0), 8);
            far = CreatePickup("far xp", EPickup.Xp, new Vector3(170,0,0), 8);
            gold = CreatePickup("near gold", EPickup.Gold, new Vector3(10,0,0), 8);
            Physics.SyncTransforms(); var item = new ItemSuckyHoof(null);
            var range = typeof(ItemSuckyHoof).GetField("range", BindingFlags.Instance | BindingFlags.NonPublic);
            Check((float)range.GetValue(item) == 100f, "factory initial range");
            item.AddAmount(); MyTime.time = 0f; item.Tick();
            var target = typeof(Pickup).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic);
            Check((Transform)target.GetValue(near) == player.transform && !near.collider.enabled, "near XP follows player");
            Check((Transform)target.GetValue(far) == null && (Transform)target.GetValue(gold) == null && gold.collider.enabled, "range and XP-only filter");
            Check(player.transform.childCount == 1 && player.transform.GetChild(0).position == player.transform.position, "magnet visual attached at player position");
            item.AddAmount(); MyTime.time = 14.9f; item.Tick(); Check((Transform)target.GetValue(far) == null, "original scheduled time retained after stacking");
            MyTime.time = 15f; item.Tick(); Check((Transform)target.GetValue(far) == player.transform, "second stack expands range at deadline");
            var deadline = typeof(ItemSuckyHoof).GetField("nextSuckTime", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(Mathf.Approximately((float)deadline.GetValue(item), 22.5f), "second stack interval is 7.5 seconds");
            Debug.Log("SuckyHoofPortCheck passed: radius, XP filter, timing, stack scaling, follow and visual parenting");
        }
        finally
        {
            MyPlayer.Instance = null; PickupManager.Instance = null; EffectManager.Instance = null; MyTime.time = 0;
            if (near != null) UnityEngine.Object.DestroyImmediate(near.gameObject); if (far != null) UnityEngine.Object.DestroyImmediate(far.gameObject); if (gold != null) UnityEngine.Object.DestroyImmediate(gold.gameObject);
            UnityEngine.Object.DestroyImmediate(playerGo); UnityEngine.Object.DestroyImmediate(stateGo); UnityEngine.Object.DestroyImmediate(prefab);
        }
    }
}
