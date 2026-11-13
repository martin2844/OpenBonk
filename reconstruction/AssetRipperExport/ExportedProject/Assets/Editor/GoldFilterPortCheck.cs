using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public static class GoldFilterPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Call(object o, string n) => o.GetType().GetMethod(n, Private).Invoke(o, null);
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static float Remaining(GoldFilterUI f) => (float)typeof(GoldFilterUI).GetField("timeLeft", Private).GetValue(f);
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldEvents = Pickup.A_PickupTriggered;
        float oldTime = MyTime.time, oldDelta = MyTime.deltaTime;
        var root = new GameObject("gold filter fixture"); root.SetActive(false);
        try
        {
            Pickup.A_PickupTriggered = null;
            GameManager.Instance = root.AddComponent<GameManager>();
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.PowerupBoostMultiplier] = 2f }; player.inventory.playerStats = stats;
            var filter = root.AddComponent<GoldFilterUI>(); filter.canvas = root.AddComponent<CanvasGroup>(); filter.canvas.alpha = 0f;
            var image = new GameObject("image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); image.transform.SetParent(root.transform); filter.goldenUi = image.GetComponent<RawImage>();
            filter.lines1 = new GameObject("lines1").transform; filter.lines1.SetParent(root.transform);
            filter.lines2 = new GameObject("lines2").transform; filter.lines2.SetParent(root.transform);
            var pickup = root.AddComponent<Pickup>(); Call(filter, "Awake"); pickup.ePickup = EPickup.Gold; Pickup.A_PickupTriggered(pickup); Require(Remaining(filter) == 0f, "unrelated pickup");
            pickup.ePickup = EPickup.Stonks; Pickup.A_PickupTriggered(pickup); Require(Mathf.Abs(Remaining(filter) - 30f) < .001f, "boosted duration");
            MyTime.time = 4f; MyTime.deltaTime = .1f; Call(filter, "Update");
            Require(Mathf.Abs(Remaining(filter) - 29.9f) < .001f && Mathf.Abs(filter.canvas.alpha - .5f) < .001f, "countdown and fade in");
            Require(Mathf.Abs(filter.goldenUi.color.a - .15f) < .001f, "gold color oscillation");
            Require(Mathf.Abs(Mathf.DeltaAngle(filter.lines1.localEulerAngles.z, 2f)) < .01f && Mathf.Abs(Mathf.DeltaAngle(filter.lines2.localEulerAngles.z, -2f)) < .01f, "opposing rotation");
            typeof(GoldFilterUI).GetField("timeLeft", Private).SetValue(filter, .6f); MyTime.deltaTime = .1f; Call(filter, "Update"); Require(Mathf.Abs(filter.canvas.alpha - .5f) < .001f, "last-second fade");
            MyTime.deltaTime = 1f; Call(filter, "Update"); Require(Remaining(filter) == 0f && filter.canvas.alpha == 0f, "expiry");
            Call(filter, "OnDestroy"); Require(Pickup.A_PickupTriggered == null, "unsubscription");
            Debug.Log("GoldFilterPortCheck passed: event, boosted duration, fade, color and line rotation");
        }
        finally { CallCleanup(root); UnityEngine.Object.DestroyImmediate(root); GameManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; Pickup.A_PickupTriggered = oldEvents; MyTime.time = oldTime; MyTime.deltaTime = oldDelta; }
    }
    static void CallCleanup(GameObject root) { var f = root.GetComponent<GoldFilterUI>(); if (f != null) Call(f, "OnDestroy"); }
}
