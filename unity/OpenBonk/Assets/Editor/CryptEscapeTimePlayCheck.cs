using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.UI.HUD.Chatbox;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CryptEscapeTimePlayCheck
{
    const string Pending = "CryptEscapeTimePlayCheck.pending";
    static CryptEscapeTimePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("crypt escape runner").AddComponent<CryptEscapeTimeRunner>().Begin(); } }
}
public class CryptEscapeTimeRunner : MonoBehaviour
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception("CryptEscapeTimePlayCheck: " + message); }
    public void Begin()
    {
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Check(TimerUtility.TimerToString(0f) == "0:00<size=75%>.00</size>", "zero timer");
            Check(TimerUtility.TimerToString(70.25f) == "1:10<size=75%>.25</size>", "minutes, seconds and centiseconds");
            Check(TimerUtility.TimerToString(3661.98f) == "1:01<size=75%>.98</size>", "minutes wrap after an hour");
            var root = new GameObject("inactive test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            SaveManager.loaded = false; save.stats = new StatsSaveFile();
            var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui;
            var exit = root.AddComponent<InteractableCryptLeave>();
            var feedHost = new GameObject("feed host"); feedHost.SetActive(false);
            var feed = feedHost.AddComponent<ServerFeed>();
            feed.content = new GameObject("feed content", typeof(RectTransform)).transform;
            feed.serverFeedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ServerFeedPrefab.prefab");
            Check(feed.serverFeedPrefab != null, "original feed prefab"); ui.feed = feed; feedHost.SetActive(true);
            var show = typeof(InteractableCryptLeave).GetMethod("ShowTime", PrivateInstance);
            var entriesField = typeof(ServerFeed).GetField("activePrefabs", PrivateInstance);
            Func<ServerFeedPrefab> latest = () => { var entries = (List<ServerFeedPrefab>)entriesField.GetValue(feed); return entries[entries.Count - 1]; };
            MyTime.cryptTimer = 70.25f; show.Invoke(exit, null);
            Check(save.stats.times[ESpeedrunTime.Crypt] == 70.25f, "first run records best");
            Check(latest().t_info.text == "[PB] Escape time: <color=#FFE300FF>1:10<size=75%>.25</size></color>!", "first-record feed");
            Check((float)typeof(ServerFeedPrefab).GetField("startFadeTime", PrivateInstance).GetValue(latest()) == 8.5f, "original display duration");
            MyTime.cryptTimer = 69f; show.Invoke(exit, null);
            Check(save.stats.times[ESpeedrunTime.Crypt] == 69f && latest().t_info.text == "[PB] Escape time: <color=#FFE300FF>1:09<size=75%>.00</size></color>! (<color=#24ff36>-1.25</color>)", "improved record and negative delta");
            MyTime.cryptTimer = 72f; show.Invoke(exit, null);
            Check(save.stats.times[ESpeedrunTime.Crypt] == 69f && latest().t_info.text == "Escape time: 1:12<size=75%>.00</size>! (<color=#ff3224>+3.00</color>)", "slower run preserves record and positive delta");
            MyTime.cryptTimer = 69f; show.Invoke(exit, null);
            Check(latest().t_info.text.Contains("(<color=#ff3224>+0.00</color>)") && !latest().t_info.text.StartsWith("[PB]"), "equal time is not a new record");
            Debug.Log("CryptEscapeTimePlayCheck passed: timer format, first/improved/equal/slower records and original feed prefab");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
