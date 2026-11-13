using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AchievementPopupPlayCheck
{
    const string Pending = "AchievementPopupPlayCheck.pending";
    const string Path = "Assets/AchievementPopupCheckTemporary.unity";
    static AchievementPopupPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Path);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        { SessionState.SetBool(Pending, false); new GameObject("achievement runner").AddComponent<AchievementPopupRunner>().Begin(); }
    }
}
public class AchievementPopupTestData : MyAchievement
{
    public string title, requirement;
    public override string GetDisplayName() => title;
    public override string GetUnlockRequirement() => requirement;
}
public class AchievementPopupRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("AchievementPopupPlayCheck: " + text); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var host = new GameObject("achievement popup"); host.SetActive(false);
        var popup = host.AddComponent<AchievementPopup>();
        var content = new GameObject("content", typeof(RectTransform)); content.transform.SetParent(host.transform); popup.content = content.GetComponent<RectTransform>(); popup.content.sizeDelta = new Vector2(300, 100); content.SetActive(false);
        var iconGo = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); iconGo.transform.SetParent(content.transform); popup.icon = iconGo.GetComponent<RawImage>();
        var titleGo = new GameObject("title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); titleGo.transform.SetParent(content.transform); popup.t_title = titleGo.GetComponent<TextMeshProUGUI>();
        var descGo = new GameObject("description", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); descGo.transform.SetParent(content.transform); popup.t_description = descGo.GetComponent<TextMeshProUGUI>();
        typeof(AchievementPopup).GetField("moveTime", Private).SetValue(popup, .1f); typeof(AchievementPopup).GetField("stayTime", Private).SetValue(popup, .12f);
        var a = ScriptableObject.CreateInstance<AchievementPopupTestData>(); a.title = "First"; a.requirement = "First requirement"; a.icon = Texture2D.whiteTexture;
        var b = ScriptableObject.CreateInstance<AchievementPopupTestData>(); b.title = "Second"; b.requirement = "Second requirement"; b.icon = Texture2D.blackTexture;
        host.SetActive(true); yield return null;
        MyAchievements.A_Unlocked?.Invoke(a); MyAchievements.A_Unlocked?.Invoke(b);
        Check(content.activeSelf && popup.icon.texture == a.icon && popup.t_title.text == "First" && popup.t_description.text == "First requirement", "first event and content");
        yield return new WaitForSeconds(.16f); Check(Mathf.Abs(popup.content.anchoredPosition.y) < .5f, "first entry reaches visible position");
        yield return new WaitForSeconds(.3f); Check(content.activeSelf && popup.icon.texture == b.icon && popup.t_title.text == "Second", "second event follows first");
        yield return new WaitForSeconds(.45f); Check(!content.activeSelf, "queue drains and closes");
        Destroy(host); yield return null; MyAchievements.A_Unlocked?.Invoke(a);
        Debug.Log("AchievementPopupPlayCheck PASSED: queued unlock events, text, icon, movement and closure"); EditorApplication.Exit(0);
    }
}
