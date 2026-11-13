#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

internal sealed class AndroidHandheldVibrationProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_handheld_vibration_probe", false)) return;
        var go = new GameObject("Handheld vibration probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidHandheldVibrationProbe>();
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(5f);
        bool available = AndroidControllerVibration.Available();
        Debug.Log("Handheld vibration probe available=" + available);
        if (available) AndroidControllerVibration.Shake(.4f, .6f);
    }
}
#endif
