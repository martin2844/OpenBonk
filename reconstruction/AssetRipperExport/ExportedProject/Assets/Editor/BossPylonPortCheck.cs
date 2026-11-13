using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public static class BossPylonPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception("BossPylonPortCheck: " + message); }
    static void Call(object o, string m) => o.GetType().GetMethod(m, Private).Invoke(o, null);
    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    static void Near(float a, float b, string m) => Check(Mathf.Abs(a-b) < .001f, m + ": " + a);
    public static void Run()
    {
        var root = new GameObject("pylon fixture"); var p = root.AddComponent<BossPylon>(); root.transform.position = new Vector3(20, 5, 30);
        p.zoneRenderer = new GameObject("zone").AddComponent<MeshRenderer>(); p.zoneRenderer.transform.SetParent(root.transform);
        p.circleParent = new GameObject("canvas").AddComponent<CanvasGroup>(); p.circleParent.transform.SetParent(root.transform);
        p.circleProgress = new GameObject("progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>(); p.circleProgress.transform.SetParent(root.transform);
        p.audioStart = root.AddComponent<AudioSource>(); p.audioLoop = root.AddComponent<AudioSource>(); p.audioComplete = root.AddComponent<AudioSource>(); p.audioAbort = root.AddComponent<AudioSource>();
        p.minimapIcon = new GameObject("icon"); p.minimapIcon.transform.SetParent(root.transform); p.altarIcon = new GameObject("altar"); p.altarIcon.transform.SetParent(root.transform);
        p.laser = new GameObject("laser").AddComponent<LineRenderer>(); p.laser.transform.SetParent(root.transform);
        p.moveFx = new GameObject("move fx").AddComponent<ParticleSystem>(); p.moveFx.transform.SetParent(root.transform);
        p.finishParticles = new GameObject("finish fx"); p.finishParticles.AddComponent<ParticleSystem>();
        var enemyObject = new GameObject("boss"); var enemy = enemyObject.AddComponent<Enemy>();
        var playerObject = new GameObject("player"); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); var items = new ItemInventory(); player.inventory.itemInventory = items;
        int events = 0; Action<BossPylon> handler = x => { if (x == p) events++; }; BossPylon.A_Charged += handler;
        try {
            p.Set(enemy); Check(p.laser.gameObject.activeSelf && p.minimapIcon.activeSelf, "pylon activated"); Near(root.transform.position.y, -9f, "buried starting height");
            MyTime.deltaTime = 1f; Call(p, "MoveUpdate"); Near(root.transform.position.y, -2f, "surface midpoint"); Call(p, "MoveUpdate"); Near(root.transform.position.y, 5f, "surface completion");
            Call(p, "OnTriggerEnter"); Near(Get<float>(p, "currentChargeTime"), 5f, "base charge duration"); Call(p, "ChargeUpdate"); Near(Get<float>(p, "chargeProgress"), .2f, "charge progress");
            Call(p, "OnTriggerExit"); MyTime.deltaTime = .25f; Call(p, "ChargeUpdate"); Near(Get<float>(p, "chargeProgress"), 0f, "charge decay");
            var wrench = new ItemWrench(items); wrench.amount = 10; items.items.Add(EItem.Wrench, wrench); Call(p, "OnTriggerEnter"); Near(Get<float>(p, "currentChargeTime"), 4.5f, "ten wrench charge duration");
            MyTime.deltaTime = 4.5f; Call(p, "ChargeUpdate"); Check(events == 1 && Get<bool>(p, "completed"), "charge emits completion"); Check(!p.laser.gameObject.activeSelf && !p.minimapIcon.activeSelf && !p.altarIcon.activeSelf, "completed pylon hides markers and laser");
            Call(p, "ChargeUpdate"); Check(events == 1, "completion emitted once");
            Debug.Log("BossPylonPortCheck passed: pylon surface, charge, decay, Wrench adjustment and completion event");
        } finally { BossPylon.A_Charged -= handler; items.Cleanup(); MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(p.moveFx.gameObject); UnityEngine.Object.DestroyImmediate(p.finishParticles); UnityEngine.Object.DestroyImmediate(root); }
    }
}
