#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;

// Observes an ordinary player death, without changing health or combat.
internal sealed class AndroidDeathAnimationProbe : MonoBehaviour
{
    bool died;
    Animator observedAnimator;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_death_animation_probe", false)) return;
        var go = new GameObject("Death animation probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidDeathAnimationProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || MyPlayer.Instance.playerRenderer == null || MyPlayer.Instance.playerRenderer.animator == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        if (MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null || MyPlayer.Instance.playerRenderer.animator == null) { Debug.LogError("Death animation probe: player unavailable"); yield break; }
        observedAnimator = MyPlayer.Instance.playerRenderer.animator;
        PlayerHealth.A_Died += OnDeath;
        Debug.Log("Death animation probe: waiting speed=" + observedAnimator.speed);
        deadline = Time.realtimeSinceStartup + 150f;
        while (!died && Time.realtimeSinceStartup < deadline) yield return null;
        if (died) { yield return null; Debug.Log("Death animation probe: following frame speed=" + (observedAnimator != null ? observedAnimator.speed.ToString("F2") : "<destroyed>")); }
        else Debug.Log("Death animation probe: no death during observation");
        PlayerHealth.A_Died -= OnDeath;
    }
    void OnDeath() { died = true; Debug.Log("Death animation probe: death event speed=" + (observedAnimator != null ? observedAnimator.speed.ToString("F2") : "<destroyed>")); }
    void OnDestroy() { PlayerHealth.A_Died -= OnDeath; }
}
#endif
