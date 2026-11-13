using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class TimedEffectPoolPlayCheck
{
    const string Pending = "TimedEffectPoolPlayCheck.pending";
    static TimedEffectPoolPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("timed effect check").AddComponent<TimedEffectPoolPlayRunner>().Begin();
        }
    }
}
public class TimedEffectPoolPlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var host = new GameObject("pool manager"); host.SetActive(false);
        var manager = host.AddComponent<PoolManager>();
        var factory = typeof(PoolManager).GetMethod("CreateObjectPoolTimeout", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var name in new[] { "LightningStrikeFx", "RocketExplosion", "CritEffect", "IceExplosionFx", "BulletHit", "SnekEffect" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
            if (prefab == null) { Debug.LogError("TimedEffectPoolPlayCheck missing " + name); EditorApplication.Exit(1); yield break; }
            var pool = (ObjectPool<GameObject>)factory.Invoke(manager, new object[] { prefab, 2, .5f });
            GameObject first = null;
            for (int i = 0; i < 2; i++)
            {
                var obj = pool.Get();
                if (obj == null || (i == 1 && obj != first)) { Debug.LogError(name + " not reused"); EditorApplication.Exit(1); yield break; }
                if (i == 0) first = obj;
                yield return null;
                int active = 0, expected = 0;
                var systems = obj.GetComponentsInChildren<ParticleSystem>(true);
                var subEmitters = new HashSet<ParticleSystem>();
                foreach (var parent in systems)
                {
                    var sub = parent.subEmitters;
                    if (!sub.enabled) continue;
                    for (int j = 0; j < sub.subEmittersCount; j++)
                    {
                        var child = sub.GetSubEmitterSystem(j);
                        if (child != null) subEmitters.Add(child);
                    }
                }
                foreach (var ps in systems)
                    if (ps.main.playOnAwake && ps.gameObject.activeInHierarchy && !subEmitters.Contains(ps)) {
                        expected++; if (ps.isPlaying) active++;
                        Debug.Log($"TimedEffectPoolPlayCheck system={ps.name} use={i} playing={ps.isPlaying} stopped={ps.isStopped} time={ps.time} duration={ps.main.duration}");
                    }
                Debug.Log($"TimedEffectPoolPlayCheck {name} use={i} playing={active}/{expected}");
                if (expected == 0 || active != expected) { Debug.LogError(name + " particle playback failed on use " + i); EditorApplication.Exit(1); yield break; }
                if (name == "LightningStrikeFx")
                {
                    yield return new WaitForSeconds(.15f);
                    foreach (var child in subEmitters)
                    {
                        Debug.Log($"TimedEffectPoolPlayCheck subemitter={child.name} use={i} particles={child.particleCount} playing={child.isPlaying}");
                        if (child.name == "Lightning" && child.particleCount == 0)
                        { Debug.LogError("Lightning subemitter produced no particles on use " + i); EditorApplication.Exit(1); yield break; }
                    }
                }
                MyTime.time += 1f;
                yield return null;
                if (pool.CountActive != 0 || pool.CountInactive != 1) { Debug.LogError(name + " return failed"); EditorApplication.Exit(1); yield break; }
            }
        }
        Debug.Log("TimedEffectPoolPlayCheck PASS: six original prefabs, two pool uses each");
        EditorApplication.Exit(0);
    }
}
