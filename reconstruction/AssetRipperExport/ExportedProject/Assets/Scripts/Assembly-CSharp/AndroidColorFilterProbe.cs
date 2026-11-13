#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects;
using UnityEngine;

// Opt-in: a controlled trigger around the scene's actual camera detector.
internal sealed class AndroidColorFilterProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_color_filter_probe", false)) return;
        var go = new GameObject("Color filter probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidColorFilterProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || UiManager.Instance == null || UiManager.Instance.colorFilterUi == null || GameObject.Find("ColorFilterDetection") == null)) yield return null;
        var detector = GameObject.Find("ColorFilterDetection");
        var ui = UiManager.Instance != null ? UiManager.Instance.colorFilterUi : null;
        if (detector == null || ui == null) { Debug.LogError("Color filter probe: scene unavailable"); yield break; }
        var volume = new GameObject("Controlled color filter");
        volume.layer = detector.layer;
        volume.transform.position = detector.transform.position;
        var collider = volume.AddComponent<SphereCollider>(); collider.isTrigger = true; collider.radius = 20f;
        var filter = volume.AddComponent<ColorFilter>(); filter.color = new Color(1f, .2f, .1f, 1f);
        int enter = 0, exit = 0;
        ColorFilter.A_FilterEnter += OnEnter; ColorFilter.A_FilterExit += OnExit;
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && enter == 0) yield return null;
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log($"Color filter probe: inside enter={enter}, alpha={ui.group.alpha:F3}, color={ui.i_color.color}, detectorCollider={detector.GetComponent<Collider>() != null}, detectorBody={detector.GetComponent<Rigidbody>() != null}");
        yield return new WaitForSecondsRealtime(6f);
        volume.transform.position = detector.transform.position + Vector3.up * 1000f;
        Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && exit == 0) yield return null;
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log($"Color filter probe: outside exit={exit}, alpha={ui.group.alpha:F3}");
        ColorFilter.A_FilterEnter -= OnEnter; ColorFilter.A_FilterExit -= OnExit;
        Destroy(volume);
        void OnEnter(ColorFilter f) { if (f == filter) enter++; }
        void OnExit(ColorFilter f) { if (f == filter) exit++; }
    }
}
#endif
