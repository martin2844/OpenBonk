using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers;
using UnityEngine;

public static class ChallengeCryptLimitCheck
{
    public static void Run()
    {
        var managerObject = new GameObject("crypt limit manager");
        var playerObject = new GameObject("crypt limit player");
        var manager = managerObject.AddComponent<GameManager>();
        var player = playerObject.AddComponent<MyPlayer>();
        var challenge = ScriptableObject.CreateInstance<ChallengeData>();
        var modifier = ScriptableObject.CreateInstance<ChallengeModifierCryptEscape>();
        var oldManager = GameManager.Instance;
        var oldPlayer = MyPlayer.Instance;
        var oldDied = PlayerHealth.A_Died;
        float oldTimer = MyTime.cryptTimer;
        try
        {
            GameManager.Instance = manager;
            MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
            var health = player.inventory.playerHealth;
            int deaths = 0;
            PlayerHealth.A_Died = () => deaths++;
            challenge.targetValue = 60;
            modifier.Init(challenge);
            var index = typeof(GameManager).GetProperty("cryptIndex").GetSetMethod(true);
            health.hp = 10;
            index.Invoke(manager, new object[] { -1 }); MyTime.cryptTimer = 60f; modifier.Tick();
            Check(health.hp == 10, "outside first crypt before entry");
            index.Invoke(manager, new object[] { 0 }); MyTime.cryptTimer = 59.99f; modifier.Tick();
            Check(health.hp == 10, "first crypt before deadline");
            MyTime.cryptTimer = 60f; modifier.Tick();
            Check(health.hp == 0 && deaths == 1, "first crypt at deadline kills once");
            modifier.Tick(); Check(deaths == 1, "dead player is not killed repeatedly");
            health.hp = 10; index.Invoke(manager, new object[] { 1 }); MyTime.cryptTimer = 100f; modifier.Tick();
            Check(health.hp == 10, "later crypt is not subject to first crypt deadline");
            Debug.Log("ChallengeCryptLimitCheck passed: pre-entry, deadline boundary, one death, later crypt");
        }
        finally
        {
            GameManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; PlayerHealth.A_Died = oldDied; MyTime.cryptTimer = oldTimer;
            UnityEngine.Object.DestroyImmediate(managerObject); UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(challenge); UnityEngine.Object.DestroyImmediate(modifier);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception("ChallengeCryptLimitCheck failed: " + message); }
}
