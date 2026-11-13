using System;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class EnemyLogThresholdPortCheck
{
    public static void Run()
    {
        var previous = DataManager.Instance;
        var previousSave = SaveManager.Instance;
        var root = new GameObject("enemy log threshold test");
        var manager = root.AddComponent<DataManager>();
        var save = root.AddComponent<SaveManager>();
        var synthetic = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            DataManager.Instance = manager;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            save.stats = new StatsSaveFile();
            var data = (Dictionary<EEnemy, EnemyData>)typeof(DataManager).GetField("enemyData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            synthetic.enemyName = EEnemy.Test;
            synthetic.creditCost = 2.5f;
            data.Add(EEnemy.Test, synthetic);
            int[] tiers = { -1, 0, 1, 2, 3 };
            int[] expected = { 1, 1, 40, 400, 4000 };
            for (int i = 0; i < tiers.Length; i++)
                if (LogUtility.GetNumChallengeKills(EEnemy.Test, tiers[i]) != expected[i])
                    throw new Exception("Incorrect threshold at tier " + tiers[i]);
            if (LogUtility.GetNumChallengeKills(EEnemy.Test2, 1) != 0)
                throw new Exception("Missing enemy should have no threshold");
            var goblin = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/GoblinWeak.asset");
            if (goblin == null || goblin.creditCost <= 0) throw new Exception("Goblin data unavailable");
            data[goblin.enemyName] = goblin;
            if (LogUtility.GetNumChallengeKills(goblin.enemyName, 1) != Mathf.CeilToInt(100f / goblin.creditCost))
                throw new Exception("Goblin threshold differs");
            var entry = new EnemyLog { kills = 0, currentChallengeTier = 0 };
            save.stats.enemyLogs[EEnemy.Test] = entry;
            if (LogUtility.GetNumMaxEntries() != Enum.GetValues(typeof(EEnemy)).Length || LogUtility.GetNumUnlockedEntries() != 0 || LogUtility.HasAnyUnclaimedReward())
                throw new Exception("Initial log totals differ");
            if (LogUtility.IsEntryUnlocked(EEnemy.Test) || LogUtility.HasUnclaimedReward(EEnemy.Test) || LogUtility.GetReward(EEnemy.Test) != 100)
                throw new Exception("Initial log state differs");
            entry.kills = 1;
            LogUtility.GetChallengeProgress(EEnemy.Test, out float progress, out int claimed, out int kills, out int threshold);
            if (!LogUtility.IsEntryUnlocked(EEnemy.Test) || !LogUtility.HasUnclaimedReward(EEnemy.Test) || progress != 1f || claimed != 0 || kills != 1 || threshold != 1)
                throw new Exception("First challenge progress differs");
            if (LogUtility.GetNumUnlockedEntries() != 1 || !LogUtility.HasAnyUnclaimedReward())
                throw new Exception("Unlocked totals or reward scan differ");
            entry.currentChallengeTier = 1; entry.kills = 20;
            LogUtility.GetChallengeProgress(EEnemy.Test, out progress, out claimed, out kills, out threshold);
            if (progress != .5f || claimed != 1 || kills != 20 || threshold != 40 || LogUtility.GetReward(EEnemy.Test) != 200 || LogUtility.HasUnclaimedReward(EEnemy.Test))
                throw new Exception("Second challenge progress differs");
            var buttonObject = new GameObject("log button");
            buttonObject.transform.SetParent(root.transform);
            var button = buttonObject.AddComponent<MyButtonLog>();
            button.eEnemy = EEnemy.Test;
            button.claimAlert = new GameObject("claim alert");
            button.claimAlert.transform.SetParent(buttonObject.transform);
            button.greenLoggedIcon = new GameObject("completed icon");
            button.greenLoggedIcon.transform.SetParent(buttonObject.transform);
            button.t_enemyName = new GameObject("name").AddComponent<TMPro.TextMeshProUGUI>();
            button.t_enemyName.transform.SetParent(buttonObject.transform);
            button.background = new GameObject("background").AddComponent<UnityEngine.UI.Image>();
            button.background.transform.SetParent(buttonObject.transform);
            button.defaultColor = Color.white; button.hoverColor = Color.red;
            button.SetEnemy(EEnemy.Test2, 4);
            if (button.t_enemyName.text != "004 ------" || button.claimAlert.activeSelf || button.greenLoggedIcon.activeSelf)
                throw new Exception("Locked enemy button differs");
            button.SetEnemy(EEnemy.Test, 17);
            if (button.t_enemyName.text != "017 Test" || button.claimAlert.activeSelf || button.greenLoggedIcon.activeSelf)
                throw new Exception("Unlocked enemy button differs");
            EEnemy selected = EEnemy.Test2;
            Action<EEnemy> onSelected = enemy => selected = enemy;
            MyButtonLog.A_EnemySelected += onSelected;
            try
            {
                button.StartHover();
                if (selected != EEnemy.Test || button.background.color != Color.red) throw new Exception("Hover selection differs");
                button.StopHover();
                if (button.background.color != Color.white) throw new Exception("Hover exit differs");
            }
            finally { MyButtonLog.A_EnemySelected -= onSelected; }
            entry.kills = 50;
            button.SetEnemy(EEnemy.Test, 17);
            if (!button.claimAlert.activeSelf) throw new Exception("Claim alert missing");
            entry.currentChallengeTier = 6;
            button.SetEnemy(EEnemy.Test, 17);
            if (!button.greenLoggedIcon.activeSelf || button.claimAlert.activeSelf) throw new Exception("Completed icon differs");
            entry.currentChallengeTier = 1; entry.kills = 20;
            int claims = 0;
            Action onClaimed = () => claims++;
            MyButtonLog.A_ClaimedReward += onClaimed;
            try
            {
                button.Claim();
                if (claims != 0 || entry.currentChallengeTier != 1) throw new Exception("Premature claim succeeded");
                entry.kills = 50;
                button.Claim();
                if (claims != 1 || entry.currentChallengeTier != 2 || button.claimAlert.activeSelf) throw new Exception("Claim did not advance or clear alert");
                button.Claim();
                if (claims != 1 || entry.currentChallengeTier != 2) throw new Exception("Duplicate claim succeeded");
                entry.currentChallengeTier = 0; entry.kills = 500;
                button.claimAlert.SetActive(true);
                button.Claim();
                if (claims != 2 || entry.currentChallengeTier != 1 || !button.claimAlert.activeSelf) throw new Exception("Remaining reward alert cleared too early");
            }
            finally { MyButtonLog.A_ClaimedReward -= onClaimed; }
            entry.currentChallengeTier = 6; entry.kills = int.MaxValue;
            if (!LogUtility.HasClaimedAllRewards(EEnemy.Test) || LogUtility.HasUnclaimedReward(EEnemy.Test))
                throw new Exception("Final challenge boundary differs");
            var displayObject = new GameObject("log display"); displayObject.transform.SetParent(root.transform);
            var display = displayObject.AddComponent<LogsDisplayEnemy>();
            TMPro.TextMeshProUGUI NewText(string name) { var o = new GameObject(name); o.transform.SetParent(displayObject.transform); return o.AddComponent<TMPro.TextMeshProUGUI>(); }
            UnityEngine.UI.RawImage NewImage(string name) { var o = new GameObject(name); o.transform.SetParent(displayObject.transform); return o.AddComponent<UnityEngine.UI.RawImage>(); }
            display.t_enemyName = NewText("name"); display.t_enemyDescription = NewText("description");
            display.t_enemyStats = NewText("stats"); display.t_enemyMaps = NewText("maps");
            display.t_killsCounter = NewText("kills"); display.t_challengeCounter = NewText("challenge"); display.t_reward = NewText("reward");
            display.enemyRenderer = NewImage("renderer"); display.barProgress = NewImage("bar"); display.i_rewardCoin = NewImage("coin");
            display.dotColorComplete = Color.green; display.dotColorIncomplete = Color.gray;
            display.challengeProgress = new List<UnityEngine.UI.RawImage> { NewImage("dot 1"), NewImage("dot 2") };
            var awake = typeof(LogsDisplayEnemy).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            var destroy = typeof(LogsDisplayEnemy).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(display, null);
            try
            {
                MyButtonLog.A_EnemySelected?.Invoke(EEnemy.Test2);
                if (display.t_enemyName.text != "------" || display.t_killsCounter.text != "Kills: 0" || display.i_rewardCoin.gameObject.activeSelf || display.barProgress.transform.localScale.x != 0f)
                    throw new Exception("Locked display differs");
                entry.currentChallengeTier = 1; entry.kills = 20;
                MyButtonLog.A_EnemySelected?.Invoke(EEnemy.Test);
                if (display.t_killsCounter.text != "Kills: 20" || display.t_challengeCounter.text != "20/40" || display.t_reward.text != "200" || !display.i_rewardCoin.gameObject.activeSelf || display.barProgress.transform.localScale.x != .5f || display.challengeProgress[0].color != Color.green || display.challengeProgress[1].color != Color.gray)
                    throw new Exception("Unlocked display differs");
                entry.currentChallengeTier = 6; entry.kills = int.MaxValue;
                MyButtonLog.A_ClaimedReward?.Invoke();
                if (display.t_reward.text != "" || display.i_rewardCoin.gameObject.activeSelf || display.challengeProgress[1].color != Color.green)
                    throw new Exception("Completed display differs");
            }
            finally { destroy.Invoke(display, null); }
            Debug.Log("EnemyLogThresholdPortCheck passed: thresholds, progress, totals, claim, completion and original Goblin asset");
        }
        finally
        {
            DataManager.Instance = previous;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousSave });
            UnityEngine.Object.DestroyImmediate(synthetic);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
