using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LateFixedPortPlayCheck
{
    private const string Pending = "LateFixedPortPlayCheck.pending";
    static LateFixedPortPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    private static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        { SessionState.SetBool(Pending, false); new GameObject("late fixed check runner").AddComponent<LateFixedPortPlayRunner>().Begin(); }
    }
}

public class LateFixedTestItem : MicrowaveTestItem
{
    public int ticks;
    public LateFixedTestItem(ItemInventory inventory) : base(inventory) { }
    public override void LateFixedUpdate() { ticks++; }
}

public class LateFixedPortPlayRunner : MonoBehaviour
{
    private static void Check(bool value, string message) { if (!value) throw new Exception("LateFixedPortPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    private IEnumerator Guard()
    {
        var test = Run();
        while (true) { bool more; try { more = test.MoveNext(); } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; }
    }
    private IEnumerator Run()
    {
        var host = new GameObject("late fixed fixture"); host.SetActive(false);
        GameManager.Instance = host.AddComponent<GameManager>();
        var save = host.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var data = host.AddComponent<DataManager>(); DataManager.Instance = data;
        var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        foreach (string name in new[] { "a_hatTophat", "a_hatTophatLong" })
        { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Check(asset != null, "original asset " + name); achievements.Add(name, asset); }
        var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Desert;
        typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
        RunStats.Init(); AchievementTracker.Init(); AchievementTracker.Init();
        var inventory = new ItemInventory(); var item = new LateFixedTestItem(inventory); inventory.items.Add(EItem.Clover, item);
        var dispatcher = new GameObject("late fixed dispatcher"); dispatcher.AddComponent<LateFixedUpdate>();
        RunStats.AddValue(EMyStat.kills, 100000); MyTime.paused = false;
        yield return new WaitForFixedUpdate(); yield return null;
        Check(item.ticks > 0, "Unity fixed update reaches inventory item");
        Check(!MyAchievements.IsAchievementDone("a_hatTophat"), "Desert does not unlock Forest hat");
        map.eMap = EMap.Forest; yield return new WaitForFixedUpdate(); yield return null;
        Check(MyAchievements.IsAchievementDone("a_hatTophat") && !MyAchievements.IsAchievementDone("a_hatTophatLong"), "100000 Forest kills unlock first hat");
        RunStats.AddValue(EMyStat.kills, 200000); yield return new WaitForFixedUpdate(); yield return null;
        Check(MyAchievements.IsAchievementDone("a_hatTophatLong"), "300000 Forest kills unlock second hat");
        MyTime.paused = true; int before = item.ticks; yield return new WaitForFixedUpdate(); yield return null;
        Check(item.ticks == before, "inventory suppresses updates while paused");
        AchievementTracker.Cleanup(); inventory.Cleanup(); RunStats.Cleanup(); MyTime.paused = false;
        Debug.Log("LateFixedPortPlayCheck passed: Unity physics dispatch, item pause, Forest kill thresholds"); EditorApplication.Exit(0);
    }
}
