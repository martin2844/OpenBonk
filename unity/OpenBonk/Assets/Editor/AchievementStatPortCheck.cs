using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using UnityEngine;

public static class AchievementStatPortCheck
{
    public static void Run()
    {
        var root = new GameObject("stat achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var startedField = typeof(MyAchievements).GetField("startedTracking", flags);
        var trackedField = typeof(MyAchievements).GetField("statTrackers", flags);
        var queuedField = typeof(MyAchievements).GetField("queuedStatNames", flags);
        var nextField = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = startedField.GetValue(null), oldTrackers = trackedField.GetValue(null), oldQueue = queuedField.GetValue(null), oldNext = nextField.GetValue(null);
        var early = ScriptableObject.CreateInstance<MyAchievement>(); var first = ScriptableObject.CreateInstance<MyAchievement>(); var second = ScriptableObject.CreateInstance<MyAchievement>(); var meta = ScriptableObject.CreateInstance<MyAchievement>();
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
            early.isEnabled = first.isEnabled = second.isEnabled = meta.isEnabled = true;
            early.internalName = "early"; early.statName = "saved"; early.targetValue = 5;
            first.internalName = "first"; first.statName = "kills"; first.targetValue = 2;
            second.internalName = "second"; second.statName = "kills"; second.targetValue = 5;
            meta.internalName = "meta"; meta.statName = "questsCompleted"; meta.targetValue = 3;
            foreach (var achievement in new[] { early, first, second, meta }) dict.Add(achievement.internalName, achievement);
            data.unsortedAchievements = new List<MyAchievement> { early, first, second, meta };
            save.stats.stats.Add("saved", new MyStat("saved", 6));
            startedField.SetValue(null, false); trackedField.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queuedField.SetValue(null, new HashSet<MyStat>()); nextField.SetValue(null, -1f);
            SaveManager.loaded = false; MyAchievements.Init();
            Require(!early.IsUnlocked(), "tracking waits for loaded saves");
            SaveManager.loaded = true; SaveManager.A_SavesLoaded?.Invoke();
            Require(early.IsUnlocked(), "existing stat unlocks achievement when saves load");
            TrackStats.Init();
            MyStats.SetValueForce("unrelated", 50); Enemy.A_EnemyDied?.Invoke(null, null); MyAchievements.Update();
            Require(!first.IsUnlocked() && !second.IsUnlocked(), "below-threshold stats do not unlock");
            Enemy.A_EnemyDied?.Invoke(null, null); nextField.SetValue(null, -1f); MyAchievements.Update();
            Require(first.IsUnlocked() && !second.IsUnlocked(), "first threshold unlocks only matching achievement");
            for (int i = 0; i < 3; i++) Enemy.A_EnemyDied?.Invoke(null, null);
            nextField.SetValue(null, -1f); MyAchievements.Update();
            Require(second.IsUnlocked(), "later threshold unlocks remaining achievement");
            Require(!meta.IsUnlocked() && MyStats.GetStat("questsCompleted") == 3 && RunStats.achievements.Count == 2, "unlock updates quest total and run unlock list");
            nextField.SetValue(null, -1f); MyAchievements.Update();
            Require(meta.IsUnlocked() && MyStats.GetStat("questsCompleted") == 4, "quest total triggers dependent achievement on subsequent update");
            Require(((Dictionary<string, List<MyAchievement>>)trackedField.GetValue(null)).Count == 0, "finished trackers removed");
            var purchase = ScriptableObject.CreateInstance<ItemData>(); purchase.eItem = EItem.CryptKey; purchase.price = 2; save.progression.silver = 100;
            Require(save.progression.PurchaseUnlockable(purchase) && MyStats.GetStat("unlocksPurchased") == 1, "purchase updates unlock count");
            UnityEngine.Object.DestroyImmediate(purchase);
            var map = ScriptableObject.CreateInstance<MapData>(); map.isEnabled = true; data.maps = new List<MapData> { map };
            var challenge = ScriptableObject.CreateInstance<ChallengeData>(); challenge.isEnabled = true; challenge.internalName = "challenge-test"; challenge.achievementType = Assets.Scripts._Data.Progression.EAchievementType.Challenges; challenge.map = map.eMap; challenge.tier = 2;
            data.unsortedAchievements.Add(challenge); dict.Add(challenge.internalName, challenge); save.progression.menuMeta.SetTierCompletion(map.eMap, challenge.tier);
            Require(MyAchievements.TryUnlock(challenge.internalName) && MyStats.GetStat("challengesCompleted") == 1, "challenge unlock updates completed challenges");
            data.unsortedAchievements.Remove(challenge); UnityEngine.Object.DestroyImmediate(challenge); UnityEngine.Object.DestroyImmediate(map);
            var enemyObject = new GameObject("test enemy"); enemyObject.SetActive(false); var enemy = enemyObject.AddComponent<Enemy>();
            var enemyData = ScriptableObject.CreateInstance<EnemyData>(); enemyData.enemyName = EEnemy.SkeletonDusty;
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
            var enemyFlags = typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic);
            enemyFlags.SetValue(enemy, EEnemyFlag.Elite | EEnemyFlag.Boss); Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, "Ice Wand") { element = EElement.Lightning });
            var oldPlayer = MyPlayer.Instance; var playerObject = new GameObject("test player"); playerObject.SetActive(false); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var characterData = ScriptableObject.CreateInstance<CharacterData>(); characterData.eCharacter = ECharacter.Calcium;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.characterData = characterData;
            enemyData.enemyName = EEnemy.GoblinTank; enemyFlags.SetValue(enemy, EEnemyFlag.StageBoss); Enemy.A_EnemyDied?.Invoke(enemy, null);
            Require(MyStats.GetStat("eliteKills") == 1 && MyStats.GetStat("minibossKills") == 1 && MyStats.GetStat("bossKills") == 1, "enemy flags update elite, miniboss, and boss counts");
            Require(MyStats.GetStat("skeletonKills") == 1 && MyStats.GetStat("goblinKills") == 1 && MyStats.GetStat("skeletonDustyKills") == 1 && MyStats.GetStat("goblinTankKills") == 1 && MyStats.GetStat("ice WandKills") == 1 && MyStats.GetStat("lightningKills") == 1, "enemy family, source and element track kills");
            Require(StatTrackingUtility.GetKeyKillsCharacter(ECharacter.Calcium) == "calciumKills" && MyStats.GetStat("calciumKills") == 1, "character kill key and count");
            MyPlayer.Instance = oldPlayer; UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(characterData);
            UnityEngine.Object.DestroyImmediate(enemyData);
            UnityEngine.Object.DestroyImmediate(enemyObject);
            var damage = new DamageContainer(1f, "test") { crit = true };
            Enemy.A_Damage?.Invoke(null, damage); damage.crit = false; Enemy.A_Damage?.Invoke(null, damage);
            WeaponAttack.A_SpawnedProjectileSuccessfully?.Invoke(null); PlayerHealth.A_Died?.Invoke(); ItemIceCube.A_FreezeEnemy?.Invoke();
            InteractableShrineChallenge.A_Completed?.Invoke(); InteractableMicrowave.A_Exploded?.Invoke(); ItemKevin.A_PunchedByKevin?.Invoke(3);
            Require(MyStats.GetStat("crits") == 1 && RunStats.GetStat(EMyStat.crits) == 1 && MyStats.GetStat("projectilesFired") == 1 && MyStats.GetStat("runs") == 1, "combat events update persistent and run stats");
            Require(MyStats.GetStat("icecubeFreezes") == 1 && MyStats.GetStat("shrineChallenge") == 1 && MyStats.GetStat("microwavesExploded") == 1 && MyStats.GetStat("kevinPunches") == 3, "item and shrine events update statistics");
            var interactObject = new GameObject("test interactions"); interactObject.SetActive(false);
            var pot = interactObject.AddComponent<InteractablePot>(); var magnet = interactObject.AddComponent<InteractableShrineMagnet>();
            int potEvents = 0; Action potBroken = () => potEvents++; TrackStats.A_PotBroken += potBroken;
            DetectInteractables.A_Interacted?.Invoke(pot, false); DetectInteractables.A_Interacted?.Invoke(pot, true); DetectInteractables.A_Interacted?.Invoke(magnet, true);
            Require(MyStats.GetStat("potsBroken") == 1 && MyStats.GetStat("shrineSucc") == 1 && potEvents == 1, "successful interactions update stats and pot notification");
            TrackStats.A_PotBroken -= potBroken;
            var pickup = interactObject.AddComponent<Pickup>(); pickup.ePickup = EPickup.Xp; Pickup.A_PickupTriggered?.Invoke(pickup);
            pickup.ePickup = EPickup.Gold; Pickup.A_PickupTriggered?.Invoke(pickup); pickup.ePickup = EPickup.Silver; Pickup.A_PickupTriggered?.Invoke(pickup);
            pickup.ePickup = EPickup.Health; Pickup.A_PickupTriggered?.Invoke(pickup); pickup.ePickup = EPickup.Nuke; Pickup.A_PickupTriggered?.Invoke(pickup);
            Require(MyStats.GetStat("powerupsUsed") == 2, "powerup count excludes ordinary pickups");
            var mapField = typeof(MapController).GetProperty("currentMap"); var oldMap = mapField.GetValue(null); var desert = ScriptableObject.CreateInstance<MapData>(); desert.eMap = EMap.Desert; mapField.GetSetMethod(true).Invoke(null, new object[] { desert });
            var stormField = typeof(MapEventsDesert).GetField("isActiveStorm", BindingFlags.NonPublic | BindingFlags.Static); object oldStorm = stormField.GetValue(null); float oldStormTime = MapEventsDesert.currentStormStartedAtTime, oldTime = MyTime.stageTimer;
            stormField.SetValue(null, true); MapEventsDesert.currentStormStartedAtTime = 10f; MyTime.stageTimer = 11.9f; ChargeShrine.A_Charged?.Invoke(true);
            MyTime.stageTimer = 12f; ChargeShrine.A_Charged?.Invoke(false);
            Require(MyStats.GetStat("shrineCharge") == 2 && MyStats.GetStat("shrineChargeSandstorm") == 1, "desert storm shrine requires two elapsed seconds");
            stormField.SetValue(null, oldStorm); MapEventsDesert.currentStormStartedAtTime = oldStormTime; MyTime.stageTimer = oldTime; mapField.GetSetMethod(true).Invoke(null, new object[] { oldMap });
            UnityEngine.Object.DestroyImmediate(desert); UnityEngine.Object.DestroyImmediate(interactObject);
            TrackStats.Cleanup(); MyAchievements.Cleanup(); Debug.Log("AchievementStatPortCheck passed");
        }
        finally
        {
            startedField.SetValue(null, oldStarted); trackedField.SetValue(null, oldTrackers); queuedField.SetValue(null, oldQueue); nextField.SetValue(null, oldNext);
            SaveManager.loaded = oldLoaded; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); DataManager.Instance = oldData;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(early); UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); UnityEngine.Object.DestroyImmediate(meta);
        }
    }
    static void Require(bool value, string name) { if (!value) throw new Exception("AchievementStatPortCheck failed: " + name); }
}
