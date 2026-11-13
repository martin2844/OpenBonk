#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Inventory__Items__Pickups.Pickups;
using UnityEngine;

// Explicitly enabled disposable run; exercise the game's actual XP object pool.
internal sealed class AndroidXpEchoReuseProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_xp_echo_reuse_probe", false)) return;
        var root = new GameObject("XP echo reuse probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidXpEchoReuseProbe>();
    }
    static bool Close(Color a, Color b) => Vector4.Distance(a, b) < .001f;
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || PoolManager.Instance == null || PoolManager.Instance.xpPool == null)) yield return null;
        if (PoolManager.Instance == null || PoolManager.Instance.xpPool == null) { Debug.LogError("XP echo reuse probe FAIL: pool unavailable"); yield break; }
        var pool = PoolManager.Instance.xpPool;
        var obj = pool.Get();
        var pickup = obj != null ? obj.GetComponent<Pickup>() : null;
        var visuals = obj != null ? obj.GetComponent<XpVisuals>() : null;
        if (pickup == null || visuals == null || visuals.ps == null) { Debug.LogError("XP echo reuse probe FAIL: prefab components missing"); yield break; }
        pickup.Set(Vector3.up * 500f, 1, 100f);
        visuals.SetEchoXp();
        pickup.AddValue(1);
        bool echoRetained = Close(visuals.ps.main.startColor.color, visuals.c_echo);
        pool.Release(obj);
        var reused = pool.Get();
        var reusedPickup = reused.GetComponent<Pickup>();
        var reusedVisuals = reused.GetComponent<XpVisuals>();
        reusedPickup.Set(Vector3.up * 500f, 1, 100f);
        bool normal = Close(reusedVisuals.ps.main.startColor.color, reusedVisuals.c_low);
        reusedPickup.AddValue(9);
        bool medium = Close(reusedVisuals.ps.main.startColor.color, reusedVisuals.c_mid);
        bool sameObject = reused == obj;
        pool.Release(reused);
        Debug.Log($"XP echo reuse probe {(sameObject && echoRetained && normal && medium ? "PASS" : "FAIL")}: sameObject={sameObject}, activeEcho={echoRetained}, normalAfterReuse={normal}, medium={medium}");
    }
}
#endif
