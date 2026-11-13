using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class FinalSwarmSilverPortCheck
{
    private sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    private const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Check(bool value, string message) { if (!value) throw new Exception("FinalSwarmSilverPortCheck: " + message); }
    private static void Invoke(object obj, string method) { obj.GetType().GetMethod(method, P).Invoke(obj, null); }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<FinalSwarmSilverUi>(FindObjectsInactive.Include);
        Check(original != null && original.coin != null && original.t_silverMultiplier != null && original.t_difficulty != null && original.progressBar != null && original.outlineGlow != null && original.contentParent != null, "original UI references");
        var copy = UnityEngine.Object.Instantiate(original.gameObject); var ui = copy.GetComponent<FinalSwarmSilverUi>();
        Check(ui.contentParent.transform.IsChildOf(copy.transform), "content belongs to cloned hierarchy");
        var root = new GameObject("final swarm silver fixture"); root.SetActive(false);
        var previousPlayer = MyPlayer.Instance; float oldTime = MyTime.time, oldSwarm = MyTime.finalSwarmTimer;
        try
        {
            LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
            LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.statInventory = new StatInventory();
            ui.contentParent.SetActive(false); Invoke(ui, "Start");
            SummonerController.A_FinalSwarmStarted?.Invoke(); Check(ui.contentParent.activeSelf && ui.contentParent.transform.localScale == Vector3.zero, "swarm opens content at zero scale");
            foreach (var test in new[] { (time: 0f, multiplier: 1f), (time: 60f, multiplier: 2.75f), (time: 120f, multiplier: 4.5f), (time: 180f, multiplier: 6.25f), (time: 240f, multiplier: 8f), (time: 300f, multiplier: 8f) })
            {
                MyTime.time += 2f; MyTime.finalSwarmTimer = test.time; Invoke(ui, "UpdateMultiplier");
                Check(player.inventory.statInventory.movingStats.TryGetValue("Swarm Silver Multiplier", out var modifier) && modifier.stat == EStat.SilverIncreaseMultiplier && modifier.modifyType == EStatModifyType.Multiplication && Mathf.Abs(modifier.modification - test.multiplier) < .001f, "multiplier at " + test.time);
            }
            Check(ui.t_silverMultiplier.text == "8x", "original multiplier display");
            Invoke(ui, "Update"); Check(Mathf.Approximately(ui.progressBar.transform.localScale.x, 1f), "progress bar capped");
            SummonerController.A_FinalSwarmStopped?.Invoke(); Check(!ui.contentParent.activeSelf, "stop hides content");
            MyPlayer.A_PlayerInventoryInitialized?.Invoke(player.inventory); Check(!player.inventory.statInventory.movingStats.ContainsKey("Swarm Silver Multiplier"), "inventory initialization removes modifier");
            Invoke(ui, "OnDestroy"); ui.contentParent.SetActive(false); SummonerController.A_FinalSwarmStarted?.Invoke(); Check(!ui.contentParent.activeSelf, "cleanup unsubscribes");
            Debug.Log("FinalSwarmSilverPortCheck passed: original UI, 1x–8x timing, stat multiplier and lifecycle");
        }
        finally
        {
            Invoke(ui, "OnDestroy"); MyPlayer.Instance = previousPlayer; MyTime.time = oldTime; MyTime.finalSwarmTimer = oldSwarm;
            UnityEngine.Object.DestroyImmediate(copy); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
