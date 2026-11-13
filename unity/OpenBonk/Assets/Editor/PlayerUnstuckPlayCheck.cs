using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerUnstuckPlayCheck
{
    const string Pending = "PlayerUnstuckPlayCheck.pending";
    static PlayerUnstuckPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("unstuck test runner").AddComponent<PlayerUnstuckPlayRunner>().Begin(); } }
}
public class PlayerUnstuckPlayRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception("PlayerUnstuckPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var go = new GameObject("player"); go.transform.position = new Vector3(0, 10, 0);
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 2; rb.useGravity = true;
            var movement = go.AddComponent<PlayerMovement>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerMovement).GetField("rb", flags).SetValue(movement, rb);
            typeof(PlayerMovement).GetField("playerHeight", flags).SetValue(movement, 2f);
            typeof(PlayerMovement).GetField("<playerRadius>k__BackingField", flags).SetValue(movement, .5f);
            var timer = typeof(PlayerMovement).GetField("stuckTimer", flags);
            var check = typeof(PlayerMovement).GetMethod("CheckStuck", flags);
            MyTime.fixedDeltaTime = .25f;
            for (int i = 0; i < 4; i++) check.Invoke(movement, null);
            Check(Mathf.Approximately((float)timer.GetValue(movement), 1f), "no impulse at one-second boundary");
            check.Invoke(movement, null); Check((float)timer.GetValue(movement) == 0f, "timeout resets");
            Physics.Simulate(.02f); Check(rb.velocity.y > 30f, "upward force applied to actual rigidbody");
            rb.velocity = Vector3.right; timer.SetValue(movement, .75f); check.Invoke(movement, null); Check((float)timer.GetValue(movement) == 0f, "movement resets timer");
            rb.velocity = Vector3.zero; rb.useGravity = false; timer.SetValue(movement, .75f); check.Invoke(movement, null); Check((float)timer.GetValue(movement) == 0f, "gravity disabled resets timer");
            rb.useGravity = true; rb.isKinematic = true; timer.SetValue(movement, .75f); check.Invoke(movement, null); Check((float)timer.GetValue(movement) == 0f, "kinematic resets timer");
            rb.isKinematic = false; go.transform.position = new Vector3(0, 1f, 0); rb.position = go.transform.position; rb.velocity = Vector3.zero;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(10, 1, 10);
            movement.whatIsGround = 1; movement.whatIsGroundOnly = 1; Physics.SyncTransforms();
            Check(movement.IsTouchingGround(), "synthetic ground is detected");
            timer.SetValue(movement, .75f); check.Invoke(movement, null); Check((float)timer.GetValue(movement) == 0f, "ground contact resets timer");
            Debug.Log("PlayerUnstuckPlayCheck passed: airborne timeout, physical upward force, movement, ground, kinematic and gravity gates"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
