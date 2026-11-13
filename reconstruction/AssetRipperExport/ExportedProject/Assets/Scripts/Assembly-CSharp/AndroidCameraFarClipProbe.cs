#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Managers;
using UnityEngine;

// Read-only opt-in observation of the camera created by normal scene loading.
internal sealed class AndroidCameraFarClipProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_camera_far_probe", false)) return;
        var go = new GameObject("Camera far clip probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidCameraFarClipProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
               (PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null || MapController.currentStage == null))
            yield return null;
        yield return new WaitForSecondsRealtime(3f);
        var camera = PlayerCamera.Instance;
        var stage = MapController.currentStage;
        if (camera == null || camera.camera == null || stage == null)
        {
            Debug.LogError("Camera far clip probe: gameplay camera timeout");
            yield break;
        }
        float actual = camera.camera.farClipPlane;
        Debug.Log($"Camera far clip probe {(Mathf.Abs(actual - stage.farClipPlane) < .01f ? "PASS" : "FAIL")}: stage={stage.name}, expected={stage.farClipPlane}, actual={actual}");
    }
}
#endif
