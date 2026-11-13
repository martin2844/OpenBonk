using System;
using System.Collections;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ObjectImpactPlayCheck
{
    const string Pending = "ObjectImpactPlayCheck.pending";
    static ObjectImpactPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("impact test runner").AddComponent<ObjectImpactPlayRunner>().Begin(); } }
}
public class ObjectImpactSmokeMarker : MonoBehaviour { public static int count; void Awake() { count++; } }
public class ObjectImpactCollisionProbe : MonoBehaviour { public static int count; public static float speed; void OnCollisionEnter(Collision collision) { count++; speed = collision.relativeVelocity.magnitude; } }
public class ObjectImpactPlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ObjectImpactPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    static void Hit(Rigidbody body, float speed)
    {
        body.position = new Vector3(-3f, 0, 0); body.velocity = Vector3.zero; Physics.SyncTransforms(); Physics.Simulate(.02f);
        body.position = new Vector3(-1.05f, 0, 0); body.rotation = Quaternion.identity; body.velocity = new Vector3(speed, 0, 0); body.angularVelocity = Vector3.zero;
        Physics.SyncTransforms(); for (int i=0; i<20; i++) Physics.Simulate(.02f);
    }
    IEnumerator Run()
    {
        yield return null;
        try
        {
            Physics.simulationMode = SimulationMode.Script; MyTime.time = 10f;
            var managerObject = new GameObject("effect manager"); managerObject.SetActive(false); var manager = managerObject.AddComponent<EffectManager>(); EffectManager.Instance = manager;
            manager.smokeHit = new GameObject("smoke marker"); manager.smokeHit.AddComponent<ObjectImpactSmokeMarker>(); int baseline = ObjectImpactSmokeMarker.count;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "wall"; wall.transform.position = Vector3.zero;
            Physics.IgnoreLayerCollision(0, 0, false);
            var moving = GameObject.CreatePrimitive(PrimitiveType.Cube); moving.transform.localScale = Vector3.one * .2f;
            var body = moving.AddComponent<Rigidbody>(); body.useGravity = false; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            moving.AddComponent<SmokeAndShakeObject>(); moving.AddComponent<ObjectImpactCollisionProbe>(); int impacts = 0; Action<float> onImpact = _ => impacts++; SmokeAndShakeObject.A_Impact += onImpact;
            try
            {
                Hit(body, 5f); Check(ObjectImpactSmokeMarker.count == baseline, "slow impacts ignored");
                Hit(body, 20f); Debug.Log($"Impact diagnostic collisions={ObjectImpactCollisionProbe.count}, speed={ObjectImpactCollisionProbe.speed}, smoke={ObjectImpactSmokeMarker.count}, baseline={baseline}, x={body.position.x}"); Check(ObjectImpactCollisionProbe.speed >= 10f && ObjectImpactSmokeMarker.count == baseline + 1, "fast impact spawns smoke");
                Hit(body, 20f); Check(ObjectImpactSmokeMarker.count == baseline + 1, "cooldown suppresses repeat");
                MyTime.time += .5f; Hit(body, 20f); Check(ObjectImpactSmokeMarker.count == baseline + 2, "cooldown boundary permits another hit");
                wall.layer = LayerMask.NameToLayer("Player"); Physics.IgnoreLayerCollision(0, wall.layer, false); MyTime.time += 1f; int collisions = ObjectImpactCollisionProbe.count; Hit(body, 20f); Check(ObjectImpactCollisionProbe.count > collisions && ObjectImpactSmokeMarker.count == baseline + 2, "player-layer collision ignored");
                Check(impacts == 0, "original collision path does not emit shake event");
            }
            finally { SmokeAndShakeObject.A_Impact -= onImpact; Physics.simulationMode = SimulationMode.FixedUpdate; EffectManager.Instance = null; }
            Debug.Log("ObjectImpactPlayCheck passed: real collision, speed, cooldown, player layer and smoke"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
