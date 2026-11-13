#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Opt-in test of the existing debug setting against the actual generated scene.
internal sealed class AndroidEnemyInfoProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_enemy_info_probe", false)) return;
        var go = new GameObject("Enemy info probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidEnemyInfoProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        EnemyInformationUi ui = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            ui = FindObjectOfType<EnemyInformationUi>(true);
            if (ui != null && GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && SaveManager.Instance != null && SaveManager.Instance.config != null) break;
            yield return null;
        }
        if (ui == null || SaveManager.Instance == null || SaveManager.Instance.config == null) { Debug.LogError("Enemy info probe: scene unavailable"); yield break; }
        int previous = SaveManager.Instance.config.cfGameSettings.debug_enemy_scaling;
        SaveManager.Instance.config.cfGameSettings.debug_enemy_scaling = 1;
        CurrentSettings.A_SettingUpdated?.Invoke("debug_enemy_scaling", previous, 1);
        yield return new WaitForSecondsRealtime(3f);
        Debug.Log($"Enemy info probe: parent={(ui.parent != null && ui.parent.activeInHierarchy)}, count={(ui.t_enemies != null ? ui.t_enemies.text : "none")}, hp={(ui.scalingEntryHp != null && ui.scalingEntryHp.t_text != null ? ui.scalingEntryHp.t_text.text : "none")}");
    }
}
#endif
