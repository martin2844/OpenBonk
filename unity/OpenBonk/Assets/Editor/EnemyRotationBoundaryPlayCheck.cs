using System;
using System.Collections;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EnemyRotationBoundaryPlayCheck
{
    const string Pending = "EnemyRotationBoundaryPlayCheck.pending";
    static EnemyRotationBoundaryPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("rotation boundary runner").AddComponent<EnemyRotationBoundaryPlayRunner>().Begin();
        }
    }
}

public class EnemyRotationBoundaryPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard()
    {
        var test = Run();
        while (true)
        {
            bool more;
            try { more = test.MoveNext(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            if (!more) yield break;
            yield return test.Current;
        }
    }
    IEnumerator Run()
    {
        var gameObject = new GameObject("game state"); gameObject.SetActive(false);
        GameManager.Instance = gameObject.AddComponent<GameManager>();
        var targetObject = new GameObject("target");
        var target = targetObject.AddComponent<Rigidbody>(); target.isKinematic = true;
        var actor = new GameObject("enemy"); actor.SetActive(false);
        var enemy = actor.AddComponent<Enemy>();
        var movement = actor.AddComponent<EnemyMovementRb>();
        var body = actor.AddComponent<Rigidbody>(); body.useGravity = false;
        var data = ScriptableObject.CreateInstance<EnemyData>();
        enemy.rb = body; enemy.enemyMovement = movement; enemy.hp = 100f;
        movement.enemy = enemy; movement.rb = body;
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        typeof(Enemy).GetProperty("target").GetSetMethod(true).Invoke(enemy, new object[] { target });
        typeof(EnemyMovementRb).GetField("nextStepTime", Private).SetValue(movement, 10000f);
        typeof(EnemyMovementRb).GetField("nextClimbCheckTime", Private).SetValue(movement, 10000f);
        typeof(EnemyMovementRb).GetField("nextGroundedCheckTime", Private).SetValue(movement, 10000f);
        MyTime.time = 1f; MyTime.fixedDeltaTime = Time.fixedDeltaTime;
        actor.SetActive(true);
        yield return null;

        float[,] cases = { { 0f, 7.9f, 0f }, { 0f, 8.1f, 1f }, { 359f, 1f, 1f }, { 1f, 359f, 1f } };
        for (int i = 0; i < cases.GetLength(0); i++)
        {
            body.rotation = Quaternion.Euler(0f, cases[i, 0], 0f);
            body.angularVelocity = Vector3.zero;
            var start = body.rotation;
            typeof(EnemyMovementRb).GetField("desiredRotation", Private).SetValue(movement, Quaternion.Euler(0f, cases[i, 1], 0f));
            movement.MyFixedUpdate();
            yield return new WaitForFixedUpdate();
            bool moved = Quaternion.Angle(start, body.rotation) > 0.01f;
            if (moved != (cases[i, 2] != 0f)) throw new Exception("Rotation case " + i + " failed; start=" + cases[i, 0] + ", target=" + cases[i, 1] + ", actual=" + body.rotation.eulerAngles.y);
        }
        Debug.Log("EnemyRotationBoundaryPlayCheck passed");
        EditorApplication.Exit(0);
    }
}
