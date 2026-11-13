#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using UnityEngine;

// Explicit opt-in diagnostic for a disposable Android session.
internal sealed class AndroidControllerMapProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_controller_map_probe", false)) return;
        var go = new GameObject("Android controller map probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidControllerMapProbe>();
        Debug.Log("Controller map probe READY");
    }

    private void Update()
    {
        for (int i = 0; i < 20; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                Debug.Log($"Controller map probe: Unity joystick button {i}; devices={string.Join(",", Input.GetJoystickNames())}");
    }
}
#endif
