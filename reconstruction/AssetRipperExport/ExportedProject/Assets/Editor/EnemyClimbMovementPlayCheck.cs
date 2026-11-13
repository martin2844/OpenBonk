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
public static class EnemyClimbMovementPlayCheck
{
    const string Pending = "EnemyClimbMovementPlayCheck.pending";
    static EnemyClimbMovementPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("climb movement runner").AddComponent<EnemyClimbMovementPlayRunner>().Begin(); } }
}
public class EnemyClimbMovementPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("EnemyClimbMovementPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var state = new GameObject("game state"); state.SetActive(false); var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << 8; game.whatIsGroundAndObjects = (1 << 8) | (1 << 12);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.layer = 8; floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(30, 1, 30);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer = 12; wall.transform.position = new Vector3(1.4f, .5f, 0); wall.transform.localScale = new Vector3(.3f, 1f, 3f);
        var target = new GameObject("target"); target.transform.position = new Vector3(10, .5f, 0); var targetBody = target.AddComponent<Rigidbody>(); targetBody.isKinematic = true;
        var actor = new GameObject("enemy"); actor.SetActive(false); actor.layer = 6; actor.transform.position = new Vector3(0, .5f, 0);
        var enemy = actor.AddComponent<Enemy>(); var movement = actor.AddComponent<EnemyMovementRb>(); var body = actor.AddComponent<Rigidbody>(); var collider = actor.AddComponent<CapsuleCollider>();
        collider.radius = .4f; collider.height = 1f; body.constraints = RigidbodyConstraints.FreezeRotation; enemy.rb = body; enemy.collider = collider; enemy.enemyMovement = movement; movement.rb = body; movement.enemy = enemy;
        var data = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data }); typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 1f }); typeof(Enemy).GetProperty("target").GetSetMethod(true).Invoke(enemy, new object[] { targetBody }); enemy.hp = 100f;
        typeof(EnemyMovementRb).GetField("nextGetSpeedTime", Private).SetValue(movement, 10000f); typeof(EnemyMovementRb).GetField("storedSpeed", Private).SetValue(movement, 2f);
        MyTime.paused = false; MyTime.time = 10f; MyTime.fixedDeltaTime = Time.fixedDeltaTime; Physics.SyncTransforms(); actor.SetActive(true);
        float maxHeight = actor.transform.position.y; bool climbingVelocity = false;
        for (int i = 0; i < 100; i++)
        {
            MyTime.time += Time.fixedDeltaTime; movement.MyFixedUpdate(); climbingVelocity |= body.velocity.y > 7f; yield return new WaitForFixedUpdate(); maxHeight = Mathf.Max(maxHeight, body.position.y);
            if (body.position.x > 2.5f) break;
        }
        Check(climbingVelocity && maxHeight > 1.1f && body.position.x > 1.8f, "enemy must rise and pass the wall; position=" + body.position + ", maxHeight=" + maxHeight);
        wall.tag = "Ignore"; body.position = new Vector3(0, .5f, 0); body.velocity = Vector3.zero; Physics.SyncTransforms();
        typeof(EnemyMovementRb).GetField("isClimbing", Private).SetValue(movement, false);
        typeof(EnemyMovementRb).GetField("nextClimbCheckTime", Private).SetValue(movement, 0f);
        float ignoredMaxHeight = body.position.y;
        for (int i = 0; i < 100; i++)
        {
            MyTime.time += Time.fixedDeltaTime; movement.MyFixedUpdate(); yield return new WaitForFixedUpdate(); ignoredMaxHeight = Mathf.Max(ignoredMaxHeight, body.position.y);
        }
        Check(body.position.x < 1.1f && ignoredMaxHeight < 1f, "ignored obstacle must block instead of triggering climb; position=" + body.position + ", maxHeight=" + ignoredMaxHeight);
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("EnemyClimbMovementPlayCheck passed: movement update, live Rigidbody traversal and ignored-wall control; maxHeight=" + maxHeight + ", ignoredPosition=" + body.position); EditorApplication.Exit(0);
    }
}
