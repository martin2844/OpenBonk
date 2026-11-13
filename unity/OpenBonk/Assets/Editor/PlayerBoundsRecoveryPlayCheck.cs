using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerBoundsRecoveryPlayCheck
{
    const string Pending = "PlayerBoundsRecoveryPlayCheck.pending";
    static PlayerBoundsRecoveryPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("bounds recovery runner").AddComponent<PlayerBoundsRecoveryPlayRunner>().Begin(); } }
}
public class PlayerBoundsRecoveryPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("PlayerBoundsRecoveryPlayCheck: " + reason); }
    static void Set(PlayerMovement p, string field, object value) => typeof(PlayerMovement).GetField(field, Private).SetValue(p, value);
    static T Get<T>(PlayerMovement p, string field) => (T)typeof(PlayerMovement).GetField(field, Private).GetValue(p);
    static void Call(PlayerMovement p, string method) => typeof(PlayerMovement).GetMethod(method, Private).Invoke(p, null);
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "valid floor"; floor.layer = 8; floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(6, 1, 6);
        var ignored = GameObject.CreatePrimitive(PrimitiveType.Cube); ignored.name = "ignored floor"; ignored.layer = 8; ignored.tag = "Ignore"; ignored.transform.position = new Vector3(15, -.5f, 0); ignored.transform.localScale = new Vector3(6, 1, 6);
        var actor = new GameObject("bounds recovery player"); actor.SetActive(false); actor.layer = LayerMask.NameToLayer("Player"); actor.transform.position = new Vector3(0, 2, 0);
        var body = actor.AddComponent<Rigidbody>(); body.constraints = RigidbodyConstraints.FreezeRotation;
        actor.AddComponent<CapsuleCollider>(); var movement = actor.AddComponent<PlayerMovement>(); movement.whatIsGround = 1 << 8; Set(movement, "rb", body);
        MyTime.time = 10f; Physics.simulationMode = SimulationMode.FixedUpdate; Physics.gravity = new Vector3(0, -9.81f, 0); MapInfo.mapBoundsLower = new Vector3(-20, -5, -20); actor.SetActive(true); Physics.SyncTransforms();
        for (int i = 0; i < 100 && !movement.grounded; i++) { MyTime.time += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
        Check(movement.grounded && Get<GameObject>(movement, "groundedObject") == floor, "must actually collide with valid floor; pos=" + body.position + " grounded=" + movement.grounded + " object=" + Get<GameObject>(movement, "groundedObject"));
        body.velocity = Vector3.zero; Call(movement, "VerifyPosition"); Vector3 safe = movement.lastGroundedPosition;
        Check(Vector3.Distance(safe, body.position) < .01f && safe.y > .3f, "record actual grounded rigidbody position; safe=" + safe + " body=" + body.position);
        movement.grounded = false; body.position = new Vector3(15, 2, 0); body.velocity = Vector3.zero; Physics.SyncTransforms();
        for (int i = 0; i < 100 && !movement.grounded; i++) { MyTime.time += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
        Check(movement.grounded && Get<GameObject>(movement, "groundedObject") == ignored, "must actually collide with Ignore-tagged floor");
        Call(movement, "VerifyPosition"); Check(Vector3.Distance(safe, movement.lastGroundedPosition) < .01f, "Ignore floor does not replace safe location");
        movement.grounded = false; body.useGravity = false; body.position = new Vector3(10, -15, 0); body.velocity = new Vector3(1, -5, 2); Physics.SyncTransforms();
        Call(movement, "VerifyPosition"); yield return new WaitForFixedUpdate();
        Check(body.position.y <= -15 && body.position.x > 9, "threshold is strict, not inclusive");
        body.position = new Vector3(10, -15.01f, 0); body.velocity = new Vector3(1, -40, 2); Set(movement, "lastVelocity", body.velocity); Set(movement, "fallSpeed", -40f); Set(movement, "leftGroundAtTime", 0f);
        int landed = 0; float speed = 0; Action<float> callback = v => { landed++; speed = v; }; PlayerMovement.A_Landed += callback;
        Call(movement, "VerifyPosition"); PlayerMovement.A_Landed -= callback; yield return new WaitForFixedUpdate();
        Check(Vector3.Distance(body.position, safe) < .02f && body.velocity.sqrMagnitude < .0001f, "below boundary returns to saved position and clears velocity");
        Check(landed == 1 && Mathf.Abs(speed - 40f) < .001f, "recovery uses normal landing callback");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("PlayerBoundsRecoveryPlayCheck passed: real collisions, Ignore floor, strict boundary, Rigidbody recovery and landing event; safe=" + safe); EditorApplication.Exit(0);
    }
}
