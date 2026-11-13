#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

// Observes the normal reset UI and scene transition; does not inject input or change settings.
internal sealed class AndroidQuickResetProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_quick_reset_probe", false)) return;
        var go = new GameObject("Quick reset probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidQuickResetProbe>();
    }
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Debug.Log("Quick reset probe: loaded scene=" + scene.name); }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var ui = FindObjectOfType<ResetRunUi>();
        if (ui == null) { Debug.LogError("Quick reset probe: UI unavailable"); yield break; }
        var holdingField = typeof(ResetRunUi).GetField("holding", BindingFlags.Instance | BindingFlags.NonPublic);
        Debug.Log($"Quick reset probe: ready scene={SceneManager.GetActiveScene().name}, setting={SaveManager.Instance.config.cfGameSettings.quick_reset}, holdTime={SaveManager.Instance.config.cfGameSettings.quick_reset_time}");
        bool last = false; deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline && ui != null)
        {
            bool holding = (bool)holdingField.GetValue(ui);
            if (holding != last) { last = holding; Debug.Log($"Quick reset probe: holding={holding}, progress={ui.progressBar.fillAmount:F3}, visible={ui.content.gameObject.activeInHierarchy}"); }
            yield return null;
        }
        Debug.Log("Quick reset probe: initial UI destroyed=" + (ui == null));
    }
}
#endif
