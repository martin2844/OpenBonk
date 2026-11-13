#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using UnityEngine;

// Measures source/proxy skinned vertices while a separate virtual pad moves.
internal sealed class AndroidExternalShieldWalkProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_shield_walk_probe", false)) return;
        var go = new GameObject("External shield walk probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalShieldWalkProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || MyPlayer.Instance.playerRenderer == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null || player.character != (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu")) { Debug.LogError("Shield walk probe INCONCLUSIVE: Pikachu unavailable"); yield break; }
        var actor = player.playerRenderer; var source = actor.renderer;
        bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
        var health = player.inventory.playerHealth; float oldShield = health.shield;
        health.shield = 100f; PlayerHealth.A_Heal?.Invoke(health, 100f, true);
        var mask = source.transform.Find("OpenBonkOutlineMask")?.GetComponent<SkinnedMeshRenderer>();
        var fill = source.transform.Find("OpenBonkOutlineFill")?.GetComponent<SkinnedMeshRenderer>();
        var animator = source.GetComponentInParent<OpenBonkExternalAnimator>();
        var clock = typeof(OpenBonkExternalAnimator).GetField("time", BindingFlags.Instance | BindingFlags.NonPublic);
        if (mask == null || fill == null || animator == null || clock == null) { Debug.LogError("Shield walk probe INCONCLUSIVE: renderer or animator unavailable"); yield break; }
        var sourceMesh = new Mesh(); var maskMesh = new Mesh(); var fillMesh = new Mesh();
        Vector3 origin = player.transform.position; Vector3[] first = null;
        float maxTravel = 0f, maxMotion = 0f, maxError = 0f, lastClock = (float)clock.GetValue(animator);
        int samples = 0; bool active = true;
        Debug.Log("Shield walk probe READY"); deadline = Time.realtimeSinceStartup + 22f;
        while (Time.realtimeSinceStartup < deadline && samples < 16)
        {
            yield return new WaitForSecondsRealtime(.2f);
            float currentClock = (float)clock.GetValue(animator);
            if (Mathf.Abs(currentClock - lastClock) < .0001f) continue;
            lastClock = currentClock;
            yield return new WaitForEndOfFrame();
            source.BakeMesh(sourceMesh); mask.BakeMesh(maskMesh); fill.BakeMesh(fillMesh);
            var sv = sourceMesh.vertices; var mv = maskMesh.vertices; var fv = fillMesh.vertices;
            if (sv.Length == 0 || sv.Length != mv.Length || sv.Length != fv.Length) { active = false; break; }
            if (first == null) first = (Vector3[])sv.Clone();
            for (int i = 0; i < sv.Length; i++)
            {
                Vector3 world = source.transform.TransformPoint(sv[i]);
                maxError = Mathf.Max(maxError, Vector3.Distance(world, mask.transform.TransformPoint(mv[i])), Vector3.Distance(world, fill.transform.TransformPoint(fv[i])));
                maxMotion = Mathf.Max(maxMotion, Vector3.Distance(first[i], sv[i]));
            }
            maxTravel = Mathf.Max(maxTravel, Vector3.Distance(origin, player.transform.position));
            active &= mask.enabled && fill.enabled && mask.gameObject.activeInHierarchy && fill.gameObject.activeInHierarchy;
            samples++;
        }
        bool pass = active && samples >= 8 && maxTravel > .5f && maxMotion > .01f && maxError < .002f;
        Debug.Log($"Shield walk probe {(pass ? "PASS" : "FAIL")}: samples={samples} maxTravel={maxTravel:F4} vertexMotion={maxMotion:F4} proxyError={maxError:F6} active={active}");
        health.shield = oldShield; PlayerHealth.A_Heal?.Invoke(health, 0f, true); EnemyManager.Instance.enabledWaves = waves;
        Destroy(sourceMesh); Destroy(maskMesh); Destroy(fillMesh); Destroy(gameObject);
    }
}
#endif
