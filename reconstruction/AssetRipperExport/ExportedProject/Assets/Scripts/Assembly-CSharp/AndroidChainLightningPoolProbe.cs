#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in controlled check of the real pool when game time is paused.
internal sealed class AndroidChainLightningPoolProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chain_pool_probe", false)) return;
        var go = new GameObject("Chain lightning pool probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidChainLightningPoolProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (PoolManager.Instance == null || PoolManager.Instance.chainLightningPool == null)) yield return null;
        if (PoolManager.Instance == null || PoolManager.Instance.chainLightningPool == null) { Debug.LogError("Chain pool probe timeout"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        var pool = PoolManager.Instance.chainLightningPool;
        bool wasPaused = MyTime.paused;
        MyTime.paused = true;
        var points = new List<Vector3> { Vector3.zero, Vector3.one };
        var longer = new List<Vector3> { Vector3.one, Vector3.right * 2f, Vector3.up * 3f };
        GameObject first = null;
        bool passed = true;
        for (int i = 0; i < 3; i++)
        {
            int activeBefore = pool.CountActive;
            var obj = pool.Get();
            if (obj == null || obj.GetComponent<ChainLightning>() == null) { passed = false; break; }
            if (i == 0) first = obj; else if (obj != first) passed = false;
            var chain = obj.GetComponent<ChainLightning>();
            var expected = i == 1 ? longer : points;
            chain.Set(expected);
            bool positionsMatch = chain.lineRenderer.positionCount == expected.Count;
            for (int p = 0; p < expected.Count && positionsMatch; p++) positionsMatch = chain.lineRenderer.GetPosition(p) == expected[p];
            if (!positionsMatch) passed = false;
            yield return new WaitForSecondsRealtime(.5f);
            bool returned = pool.CountActive == activeBefore && !obj.activeSelf;
            Debug.Log($"Chain pool probe use={i} returned={returned} reused={(obj == first)} active={pool.CountActive} inactive={pool.CountInactive}");
            if (!returned) { passed = false; break; }
        }
        MyTime.paused = wasPaused;
        Debug.Log("Chain pool probe " + (passed ? "PASS" : "FAIL"));
    }
}
#endif
