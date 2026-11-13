#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Opt-in test only: reveal the original menu panel without changing progression.
internal sealed class AndroidLeaderboardUnavailableProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_leaderboard_unavailable_probe", false)) return;
        var root = new GameObject("leaderboard unavailable probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidLeaderboardUnavailableProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 90f;
        MainMenu menu = null;
        while (Time.realtimeSinceStartup < deadline && ((menu = FindObjectOfType<MainMenu>(true)) == null || menu.leaderboards == null || !SaveManager.loaded)) yield return null;
        if (menu == null || menu.leaderboards == null) { Debug.LogError("Leaderboard unavailable probe: menu unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        menu.leaderboards.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        var ui = menu.leaderboards.GetComponentInChildren<LeaderboardUiNew>(true);
        if (ui == null) { Debug.LogError("Leaderboard unavailable probe: UI unavailable"); yield break; }
        int active = 0; string status = null, rank = null, score = null;
        foreach (var entry in ui.GetComponentsInChildren<LeaderboardEntryUi>(true))
        {
            if (!entry.gameObject.activeSelf) continue;
            active++; status = entry.playerName != null ? entry.playerName.text : null;
            rank = entry.rank != null ? entry.rank.text : null; score = entry.score != null ? entry.score.text : null;
        }
        bool pass = active == 1 && !string.IsNullOrEmpty(status) && string.IsNullOrEmpty(rank) && string.IsNullOrEmpty(score) && (ui.t_reset == null || string.IsNullOrEmpty(ui.t_reset.text));
        Debug.Log($"Leaderboard unavailable probe {(pass ? "PASS" : "FAIL")}: active={active}, status={status}, rank={rank}, score={score}, reset={(ui.t_reset != null ? ui.t_reset.text : "null")}");
    }
}
#endif
