using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ChallengeHierarchyCheck
{
    static string PathOf(Transform t) { string path = t.name; while (t.parent != null) { t = t.parent; path = t.name + "/" + path; } return path; }
    public static void Run() {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var ui = UnityEngine.Object.FindAnyObjectByType<ChallengesUi>(FindObjectsInactive.Include);
        var map = UnityEngine.Object.FindAnyObjectByType<MapSelectionUi>(FindObjectsInactive.Include);
        var stats = UnityEngine.Object.FindAnyObjectByType<MapStatsWindow>(FindObjectsInactive.Include);
        Debug.Log("CHALLENGE HIERARCHY ui=" + PathOf(ui.transform) + " map=" + PathOf(map.transform) + " stats=" + PathOf(stats.transform) + " template=" + PathOf(ui.challengeButtonPrefab.transform) + " group=" + PathOf(ui.challengesSelectionGroup.transform));
    }
}
