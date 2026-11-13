#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable visual isolation of the original dropped-chest particle renderers.
internal sealed class AndroidChestRayProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chest_ray_probe", false)) return;
        var root = new GameObject("chest ray probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidChestRayProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EffectManager.Instance == null || EffectManager.Instance.openChestNormal == null || Camera.main == null)) yield return null;
        if (EffectManager.Instance == null || EffectManager.Instance.openChestNormal == null || Camera.main == null) { Debug.LogError("Chest ray probe INCONCLUSIVE: scene unavailable"); yield break; }
        if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
        var cam = Camera.main;
        var chest = Instantiate(EffectManager.Instance.openChestNormal, cam.transform.position + cam.transform.forward * 6f + cam.transform.up * -1f, Quaternion.LookRotation(-cam.transform.forward));
        foreach (var collider in chest.GetComponentsInChildren<Collider>()) collider.enabled = false;
        yield return new WaitForSecondsRealtime(3f);
        foreach (var particle in chest.GetComponentsInChildren<ParticleSystem>()) Debug.Log($"Chest ray probe particles: {particle.name}, count={particle.particleCount}");
        var rays = chest.GetComponentsInChildren<ParticleSystemRenderer>();
        foreach (var ray in rays) Debug.Log($"Chest ray probe renderer: {ray.name}, material={ray.sharedMaterial?.name}, shader={ray.sharedMaterial?.shader?.name}");
        float oldScale = Time.timeScale; Time.timeScale = 0f;
        Debug.Log("Chest ray probe BASELINE"); yield return new WaitForSecondsRealtime(12f);
        foreach (var ray in rays) ray.enabled = false;
        Debug.Log("Chest ray probe NO_RAYS"); yield return new WaitForSecondsRealtime(12f);
        foreach (var ray in rays) ray.enabled = true;
        Debug.Log("Chest ray probe RESTORED"); yield return new WaitForSecondsRealtime(5f);
        Time.timeScale = oldScale; Destroy(chest); Destroy(gameObject);
    }
}
#endif
