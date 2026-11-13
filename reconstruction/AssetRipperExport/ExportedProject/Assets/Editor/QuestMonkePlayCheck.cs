using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class QuestMonkePlayCheck
{
    const string Pending = "QuestMonkePlayCheck.pending";
    static QuestMonkePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("quest monke check").AddComponent<QuestMonkePlayRunner>().Begin();
        }
    }
}

public class QuestMonkePlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(QuestMonke m, string name) => typeof(QuestMonke).GetMethod(name, Private).Invoke(m, null);
    static void Set(QuestMonke m, string name, object value) => typeof(QuestMonke).GetField(name, Private).SetValue(m, value);
    static T Get<T>(QuestMonke m, string name) => (T)typeof(QuestMonke).GetField(name, Private).GetValue(m);
    static void Require(bool condition, string message) { if (!condition) throw new Exception("QuestMonkePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MonkeCage.prefab");
            var source = prefab.GetComponentInChildren<QuestMonke>(true);
            Require(source != null && source.animator != null && source.rb != null && source.dissapearFx != null, "original prefab wiring");
            var obj = Instantiate(source.gameObject); obj.SetActive(false);
            foreach (var script in obj.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            var m = obj.GetComponent<QuestMonke>(); var body = m.rb; obj.transform.position = Vector3.zero; obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true); body.useGravity = false; MyTime.time = 100f; MyTime.paused = false;
            Call(m, "Start");
            Require(Mathf.Approximately(Get<float>(m, "deadTime"), 220f) && Vector3.Distance(Get<Vector3>(m, "runDir"), Vector3.forward) < .001f, "initial direction and lifetime");
            var managerObject = new GameObject("controlled game manager"); managerObject.SetActive(false);
            var manager = managerObject.AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsGroundAndObjects = 1 << 8;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer = 8; wall.transform.position = new Vector3(0, .5f, 2f);
            wall.transform.localScale = new Vector3(4, 4, .2f); Physics.SyncTransforms();
            body.isKinematic = false; body.velocity = Vector3.zero; Call(m, "TryClimb");
            Require(Mathf.Abs(body.velocity.y - 8f) < .001f && Get<bool>(m, "isClimbingWall"), "wall cast and climb velocity");
            wall.SetActive(false); Set(m, "isWalking", true); Set(m, "isClimbingWall", false); MapInfo.mapBoundsLower = new Vector3(0, -100, 0);
            body.isKinematic = false; body.velocity = new Vector3(0, 3f, 0); Call(m, "FixedUpdate");
            Require(Mathf.Abs(body.velocity.z - 12f) < .001f && Mathf.Abs(body.velocity.y - 3f) < .001f, "run speed and vertical velocity");
            MyTime.paused = true; Call(m, "FixedUpdate"); Require(body.isKinematic, "pause freezes body");
            MyTime.paused = false; Call(m, "FixedUpdate"); Require(!body.isKinematic && Mathf.Abs(body.velocity.z - 12f) < .001f, "unpause restores running");
            var effect = m.dissapearFx; MyTime.time = 221f; Call(m, "FixedUpdate");
            Require(effect.activeSelf && effect.transform.parent == null, "expiry detaches disappearance effect");
            Debug.Log("QuestMonkePlayCheck passed: original prefab, initialization, wall climb, running, pause and expiry effect");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
