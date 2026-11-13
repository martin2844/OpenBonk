#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Opt-in menu material comparison; gameplay and normal menu state do not create this object.
internal sealed class AndroidMicrowaveMenuProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_microwave_menu_probe", false)) return;
        var go = new GameObject("Microwave menu probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidMicrowaveMenuProbe>();
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(4f);
        var microwave = FindObjectOfType<InteractableMicrowave>();
        if (microwave == null || microwave.meshRenderer == null) { Debug.LogError("Microwave menu probe: no active microwave renderer"); yield break; }
        Log("natural", microwave);
        yield return new WaitForSecondsRealtime(4f);
        var original = microwave.meshRenderer.sharedMaterial;
        microwave.meshRenderer.sharedMaterial = microwave.matEpic;
        Log("forcedEpic", microwave);
        yield return new WaitForSecondsRealtime(8f);
        microwave.meshRenderer.sharedMaterial = original;
    }
    static void Log(string stage, InteractableMicrowave microwave)
    {
        var material = microwave.meshRenderer.sharedMaterial; var shader = material != null ? material.shader : null;
        var texture = material != null && material.HasProperty("_AlbedoMap") ? material.GetTexture("_AlbedoMap") : null;
        Debug.Log($"Microwave menu probe: stage={stage}, actualRarity={microwave.rarity}, material={(material != null ? material.name : "none")}, shader={(shader != null ? shader.name : "none")}, supported={(shader != null && shader.isSupported)}, texture={(texture != null ? texture.name : "none")}, enabled={microwave.meshRenderer.enabled}");
    }
}
#endif
