using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LeaderboardUnavailableSceneCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("LeaderboardUnavailableSceneCheck: " + message); }
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var leaderboards = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LeaderboardUiNew>(true)).ToArray();
        Check(leaderboards.Length > 0, "original leaderboard UI exists");
        foreach (var leaderboard in leaderboards)
        {
            var entries = leaderboard.GetComponentsInChildren<LeaderboardEntryUi>(true);
            Check(entries.Any(x => x.gameObject.activeSelf && x.score != null && !string.IsNullOrEmpty(x.score.text)), "serialized score fixture exists");
            typeof(LeaderboardUiNew).GetMethod("ShowUnavailable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(leaderboard, null);
            var visible = entries.Where(x => x.gameObject.activeSelf).ToArray();
            Check(visible.Length == 1 && visible[0].playerName != null && !string.IsNullOrEmpty(visible[0].playerName.text), "one status row");
            Check(visible[0].rank.text == "" && visible[0].score.text == "", "sample rank and score cleared");
            Check(!visible[0].playerIcon.enabled && !visible[0].characterIcon.enabled, "sample avatars hidden");
            Check(leaderboard.t_reset == null || leaderboard.t_reset.text == "", "sample timer cleared");
            Debug.Log($"LeaderboardUnavailableSceneCheck: entries={entries.Length}, status={visible[0].playerName.text}");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("LeaderboardUnavailableSceneCheck passed: original scene, fake rows cleared and unavailable status shown");
    }
}
