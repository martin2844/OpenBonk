#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Coffee.UIExtensions;
using UnityEngine;

// Explicitly enabled only in disposable Android test runs.
internal sealed class AndroidMenuParticlesProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_menu_particles_probe", false)) return;
        var root = new GameObject("Menu particles probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidMenuParticlesProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && (MenuParticles.Instance == null || MenuParticles.Instance.coinAttractor == null)) yield return null;
        if (MenuParticles.Instance == null || MenuParticles.Instance.coinAttractor == null) { Debug.LogError("Menu particles probe FAIL: menu unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(8f);
        var manager = MenuParticles.Instance;
        if (manager == null || manager.cointEffectParent == null) { Debug.LogError("Menu particles probe FAIL: parent unavailable"); yield break; }
        int before = manager.cointEffectParent.transform.childCount;
        Vector3 position = manager.coinAttractor.transform.position;
        manager.CoinEffect(position);
        Transform effect = manager.cointEffectParent.transform.childCount > before ? manager.cointEffectParent.transform.GetChild(manager.cointEffectParent.transform.childCount - 1) : null;
        if (effect == null) { Debug.LogError("Menu particles probe FAIL: effect not spawned"); yield break; }
        var ui = effect.GetComponent<UIParticle>();
        var particles = effect.GetComponentInChildren<ParticleSystem>();
        var destroy = effect.GetComponent<DestroyObject>();
        Debug.Log($"Menu particles probe spawn: ui={ui != null}, particles={particles != null}, destroy={destroy != null}, callback={(destroy != null && destroy.OnDestroy != null)}");
        yield return new WaitForSecondsRealtime(.6f);
        int count = particles != null ? particles.particleCount : -1;
        Debug.Log($"Menu particles probe emission: alive={effect != null}, particleCount={count}, playing={(particles != null && particles.isPlaying)}");
        yield return new WaitForSecondsRealtime(3f);
        Debug.Log($"Menu particles probe cleanup: destroyed={effect == null}, parentChildren={manager.cointEffectParent.transform.childCount}, initialChildren={before}");
    }
}
#endif
