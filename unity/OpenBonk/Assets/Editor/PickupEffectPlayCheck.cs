using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PickupEffectPlayCheck
{
    const string Pending = "PickupEffectPlayCheck.pending";
    static PickupEffectPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("pickup effect runner").AddComponent<PickupEffectRunner>().Begin(); } }
}
public class PickupEffectRunner : MonoBehaviour
{
    static void Require(bool v, string message) { if (!v) throw new Exception("PickupEffectPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        PoolManager pool = null;
        try
        {
            var state = new GameObject("state"); state.SetActive(false);
            var gm = state.AddComponent<GameManager>(); GameManager.Instance = gm;
            var player = new GameObject("player").AddComponent<MyPlayer>(); player.transform.position = new Vector3(2,3,4);
            pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.pickupEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PickupEffect.prefab");
            pool.goldBurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GoldBurst.prefab");
            Require(pool.pickupEffectPrefab && pool.goldBurstPrefab, "original prefabs");
            typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null);
            var manager = state.AddComponent<EffectManager>(); EffectManager.Instance = manager;
            MyTime.time = 100f;
            var point = new Vector3(7, 8, 9); manager.GoldBurstEffect(point);
            Require(pool.goldBurstPool.CountActive == 1, "gold burst checked out");
            var returner = FindObjectOfType<ReturnToPool>(); Require(returner != null && returner.transform.position == point, "gold burst placed at death position");
            manager.PickupEffect(); Require(pool.pickupeffectPool.CountActive == 1, "pickup effect checked out");
            var flash = FindObjectOfType<PickupEffect>(); Require(flash != null && flash.transform.parent == player.transform && flash.transform.position == player.transform.position, "pickup flash follows player");
            MyTime.time = 101.1f;
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(.5f);
        try
        {
            Require(pool.goldBurstPool.CountInactive == 1, "gold burst returned after one game second");
            Require(pool.pickupeffectPool.CountInactive == 1, "pickup flash returned after delay");
            Debug.Log("PickupEffectPlayCheck passed: original effects spawn, follow and return to pools"); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
