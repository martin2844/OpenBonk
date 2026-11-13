#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Opt-in, read-only observer of the real character-selection model.
internal sealed class AndroidCharacterSpinProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_character_spin_probe", false)) return;
        var go = new GameObject("Character spin probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidCharacterSpinProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        CharacterSpin spin = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            foreach (var candidate in FindObjectsOfType<CharacterSpin>())
                if (candidate.isActiveAndEnabled && candidate.playerRenderer != null) { spin = candidate; break; }
            if (spin != null) break;
            yield return null;
        }
        if (spin == null) { Debug.LogError("Character spin probe: selection timeout"); yield break; }
        Quaternion last = spin.playerRenderer.rotation;
        Debug.Log("Character spin probe ready: model=" + spin.playerRenderer.name + ", rotation=" + last.eulerAngles);
        while (spin != null && spin.isActiveAndEnabled && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForSecondsRealtime(.25f);
            Quaternion current = spin.playerRenderer.rotation;
            float delta = Quaternion.Angle(last, current);
            if (delta >= 5f) { Debug.Log("Character spin probe moved: degrees=" + delta + ", rotation=" + current.eulerAngles); last = current; }
        }
    }
}
#endif
