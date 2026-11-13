#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Observes scene transitions only when explicitly enabled by a test intent.
internal sealed class AndroidEventSystemProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_event_system_probe", false)) return;
        var go = new GameObject("Event system observation");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidEventSystemProbe>();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(Observe(scene));

    IEnumerator Observe(Scene scene)
    {
        yield return null;
        Report(scene, "one-frame");
        yield return new WaitForSecondsRealtime(2f);
        Report(scene, "two-seconds");
    }

    static void Report(Scene scene, string phase)
    {
        var all = FindObjectsOfType<EventSystem>(true);
        var active = all.Where(e => e.isActiveAndEnabled).ToArray();
        var current = EventSystem.current;
        string details = string.Join(";", active.Select(e => e.gameObject.name + "@" + e.gameObject.scene.name));
        Debug.Log($"Event system probe: loaded={scene.name}, activeScene={SceneManager.GetActiveScene().name}, phase={phase}, active={active.Length}, current={(current != null ? current.gameObject.name + "@" + current.gameObject.scene.name : "none")}, systems={details}");
    }
}
#endif
