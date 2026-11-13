using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class PowerupDropPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    sealed class FixedRandom : System.Random
    {
        public double value; public int index;
        public override double NextDouble() => value;
        public override int Next(int min, int max) => min + index;
    }
    static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, Private).Invoke(o, args);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Private).SetValue(o, v);
    static T Get<T>(object o, string n) => (T)o.GetType().GetField(n, Private).GetValue(o);
    static void Require(bool value, string message) { if (!value) throw new Exception("PowerupDropPortCheck: " + message); }
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldRandom = global::Utility.MyRandom.random; float oldTime = MyTime.time; int oldCap = PickupManager.maxPowerupsOnMap;
        var root = new GameObject("powerup test"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.PowerupChance] = 1.5f }; player.inventory.playerStats = stats;
            var pickup = root.AddComponent<PickupManager>(); var enemy = root.AddComponent<Enemy>();
            Set(enemy, "basePowerupDropChance", .02f); Set(pickup, "lastPowerupAtTime", 10f);
            MyTime.time = 9f; Require((float)Call(pickup, "GetPowerupTimeMultiplier") == 0f, "negative elapsed time clamps to zero");
            MyTime.time = 10f; Require((float)Call(pickup, "GetPowerupTimeMultiplier") == 0f, "zero chance immediately after a drop");
            MyTime.time = 70f; Require(Mathf.Abs((float)Call(pickup, "GetPowerupDropChance", enemy) - .03f) < .0001f, "half-period chance scales with enemy and stat");
            MyTime.time = 129f; Require(Mathf.Abs((float)Call(pickup, "GetPowerupTimeMultiplier") - 119f / 60f) < .0001f, "multiplier just below time cap");
            MyTime.time = 130f; Require((float)Call(pickup, "GetPowerupTimeMultiplier") == 2f, "multiplier reaches cap at 120 seconds");
            MyTime.time = 500f; Require(Mathf.Abs((float)Call(pickup, "GetPowerupDropChance", enemy) - .06f) < .0001f, "chance caps after 120 seconds");
            PickupManager.maxPowerupsOnMap = 2; Set(pickup, "numPowerupsOnMap", 0);
            Require(pickup.CanSpawnPowerup(), "empty capacity permits a drop"); pickup.CountAdd();
            Require(pickup.CanSpawnPowerup(), "one below capacity permits a drop"); pickup.CountAdd();
            Require(!pickup.CanSpawnPowerup(), "capacity blocks a drop"); pickup.PowerupCountRemove();
            Require(pickup.CanSpawnPowerup(), "removing a powerup reopens capacity");
            var expected = new[] { EPickup.Haste, EPickup.Health, EPickup.Magnet, EPickup.Nuke, EPickup.Rage, EPickup.Shield, EPickup.Stonks, EPickup.Time };
            var random = new FixedRandom(); global::Utility.MyRandom.random = random;
            for (int i = 0; i < expected.Length; i++) { random.index = i; Require((EPickup)Call(pickup, "GetRandomPowerup") == expected[i], "original powerup order " + i); }
            Set(manager, "gameTimer", 450f); Call(pickup, "SlowUpdate"); Require(Mathf.Abs(Get<float>(pickup, "stackRadius") - 4.25f) < .001f, "stack radius grows over run");
            Set(manager, "gameTimer", 1200f); Call(pickup, "SlowUpdate"); Require(Mathf.Abs(Get<float>(pickup, "stackRadius") - 8f) < .001f, "stack radius cap");
            var echo = new ItemEchoShard(null); echo.AddAmount(); Require(Mathf.Abs(echo.chance - .12f) < .0001f, "echo chance per item");
            random.value = .119; Require(echo.GetExtraShards() == 1, "echo succeeds below chance"); random.value = .12; Require(echo.GetExtraShards() == 0, "echo fails at boundary");
            Debug.Log("PowerupDropPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); GameManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; global::Utility.MyRandom.random = oldRandom; MyTime.time = oldTime; PickupManager.maxPowerupsOnMap = oldCap; }
    }
}
