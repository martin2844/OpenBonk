using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ChallengeShrinePlayCheck
{
    const string Pending = "ChallengeShrinePlayCheck.pending";
    static ChallengeShrinePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("challenge shrine test runner").AddComponent<ChallengeShrinePlayRunner>().Begin(); } }
}
public class ChallengeShrinePlayRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("ChallengeShrinePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        InteractableShrineChallenge shrine = null; GameObject root = null, enemyRoot = null;
        int completions = 0; Action completed = () => completions++;
        try
        {
            root = new GameObject("challenge shrine"); root.SetActive(false); shrine = root.AddComponent<InteractableShrineChallenge>(); root.SetActive(true);
            Require(shrine.CanInteract() && shrine.ShowInDebug() && shrine.GetDebugName() == "Challenges" && shrine.isItemSource, "initial interaction and debug metadata");
            enemyRoot = new GameObject("inactive enemies"); enemyRoot.SetActive(false);
            Enemy CreateEnemy(string name) { var go = new GameObject(name); go.transform.SetParent(enemyRoot.transform); return go.AddComponent<Enemy>(); }
            var first = CreateEnemy("first"); var second = CreateEnemy("second"); var other = CreateEnemy("other");
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(InteractableShrineChallenge).GetField("done", flags).SetValue(shrine, true);
            typeof(InteractableShrineChallenge).GetField("enemies", flags).SetValue(shrine, new HashSet<Enemy> { first, second });
            InteractableShrineChallenge.A_Completed += completed;
            Require(!shrine.CanInteract(), "started shrine cannot be reused");
            Enemy.A_EnemyReleasedFromPool?.Invoke(other); Require(completions == 0, "unrelated enemy does not complete while challenge enemies remain");
            Enemy.A_EnemyReleasedFromPool?.Invoke(first); Require(completions == 0, "first challenge enemy does not complete");
            Enemy.A_EnemyReleasedFromPool?.Invoke(second); Require(completions == 1, "last challenge enemy completes shrine");
            Enemy.A_EnemyReleasedFromPool?.Invoke(second); Require(completions == 1, "completion emitted only once");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(shrine == null, "completed component is destroyed");
            Enemy.A_EnemyReleasedFromPool?.Invoke(null); Require(completions == 1, "destroyed shrine unsubscribes");
            InteractableShrineChallenge.A_Completed -= completed;
            Destroy(root); Destroy(enemyRoot); Debug.Log("ChallengeShrinePlayCheck PASS"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
