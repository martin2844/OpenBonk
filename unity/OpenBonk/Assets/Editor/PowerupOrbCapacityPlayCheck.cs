using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class PowerupOrbCapacityPlayCheck
{
    const string Pending = "PowerupOrbCapacityPlayCheck.pending";
    static PowerupOrbCapacityPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("powerup capacity test").AddComponent<PowerupOrbCapacityPlayRunner>().Begin(); } }
}
public class PowerupOrbCapacityPlayRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception("PowerupOrbCapacityPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        int originalMax = PickupManager.maxPowerupsOnMap; PickupManager.maxPowerupsOnMap = 2;
        var state = new GameObject("powerup test state"); state.SetActive(false);
        var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 0;
        var manager = state.AddComponent<PickupManager>(); PickupManager.Instance = manager; manager.whatIsPickups = 0;
        var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        var effects = state.AddComponent<EffectManager>(); EffectManager.Instance = effects;
        effects.pickupOrbFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PickupOrb.prefab");
        var powerupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GenericPowerup.prefab");
        Check(effects.pickupOrbFx && powerupPrefab, "original prefabs");
        var spawned = new List<GameObject>();
        pool.powerupPool = new ObjectPool<GameObject>(() => { var go = Instantiate(powerupPrefab); spawned.Add(go); return go; }, go => go.SetActive(true), go => go.SetActive(false));
        var count = typeof(PickupManager).GetField("numPowerupsOnMap", BindingFlags.NonPublic | BindingFlags.Instance);
        effects.SpawnPickupOrb(EPickup.Haste, new Vector3(0, 5, 0));
        effects.SpawnPickupOrb(EPickup.Rage, new Vector3(10, 5, 0));
        Check((int)count.GetValue(manager) == 2 && !manager.CanSpawnPowerup(), "flying orbs reserve both slots immediately");
        var orbs = FindObjectsByType<PickupOrb>(FindObjectsSortMode.None); Check(orbs.Length == 2, "two original orb instances");
        typeof(PickupOrb).GetMethod("OnCollisionEnter", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(orbs[0], new object[] { null });
        Check((int)count.GetValue(manager) == 2 && !manager.CanSpawnPowerup(), "landing transfers existing slot without double-counting");
        Check(spawned.Count == 1, "original generic pickup spawned");
        var pickup = spawned[0].GetComponent<Pickup>(); Check(pickup != null, "generic prefab pickup");
        manager.PickupTriggered(pickup);
        Check((int)count.GetValue(manager) == 1 && manager.CanSpawnPowerup(), "collection frees one slot");
        typeof(PickupOrb).GetMethod("Timeout", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(orbs[1], null);
        Check((int)count.GetValue(manager) == 1, "original timeout retains the reserved slot");
        PickupManager.maxPowerupsOnMap = originalMax;
        yield return null; Debug.Log("PowerupOrbCapacityPlayCheck passed: original prefabs, in-flight limit, landing, collection and timeout"); EditorApplication.Exit(0);
    }
}
