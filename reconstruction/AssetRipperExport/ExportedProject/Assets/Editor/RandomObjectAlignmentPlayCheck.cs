using System;
using System.Reflection;
using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RandomObjectAlignmentPlayCheck
{
    const string Pending = "RandomObjectAlignmentPlayCheck.pending";
    static RandomObjectAlignmentPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("placement check").AddComponent<RandomObjectAlignmentRunner>().Run();
    }
}

public class RandomObjectAlignmentRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public void Run()
    {
        try
        {
            var state = new GameObject("inactive managers"); state.SetActive(false);
            var manager = state.AddComponent<GameManager>(); GameManager.Instance = manager;
            var save = state.AddComponent<SaveManager>(); save.config = new ConfigSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            typeof(MapController).GetField("inventory", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            int ground = LayerMask.NameToLayer("Ground"); Require(ground >= 0, "Ground layer");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.layer = ground;
            floor.transform.position = new Vector3(0, -1, 0); floor.transform.localScale = new Vector3(100, 2, 100); floor.transform.rotation = Quaternion.Euler(0, 0, 20);
            manager.whatIsGroundAndObjects = 1 << ground;
            var prototype = GameObject.CreatePrimitive(PrimitiveType.Cube); prototype.name = "alignment prototype";
            prototype.transform.position = new Vector3(500, 500, 500); prototype.isStatic = true;
            var placer = new GameObject("placer").AddComponent<RandomObjectPlacer>(); placer.center = Vector3.zero; placer.size = Vector3.zero;
            var config = new RandomMapObject { amount = 2, maxAmount = 0, checkRadius = .1f, scaleMin = 2, scaleMax = 2,
                maxSlopeAngle = 45, prefabs = new[] { prototype }, randomRotationVector = Vector3.zero, alignWithNormal = true };
            Physics.SyncTransforms(); placer.Generate(new[] { config }, 1f);
            var parent = GameObject.Find("RandomObjectsParent0");
            Require(parent != null && parent.transform.childCount == 2, "static grouping");
            var placed = parent.transform.GetChild(0);
            var expectedNormal = floor.transform.up;
            Require(Vector3.Dot(placed.forward.normalized, expectedNormal) > .999f, "forward aligned to slope normal");
            Require(Vector3.Distance(placed.localScale, Vector3.one * 2f) < .001f, "configured scale");
            Require(placed.GetComponent<Renderer>() != null && placed.gameObject.isStatic, "static renderer retained");
            Require(placed.GetComponent<Renderer>().isPartOfStaticBatch, "renderer entered static batch");
            floor.SetActive(false); Physics.SyncTransforms();
            var modConfig = new OpenBonkMapObjects.DecorativeObject { amount = 2, checkRadius = .1f, scaleMin = 2, scaleMax = 2,
                maxSlopeAngle = 45, prefabs = new[] { prototype }, randomRotationVector = Vector3.zero, alignWithNormal = true };
            placer.Generate(new RandomMapObject[] { modConfig }, 1f);
            Require(GameObject.Find("RandomObjectsParent1") == null, "no static group when there is no valid ground");
            var clones = GameObject.FindObjectsOfType<Transform>(); int count = 0;
            foreach (var t in clones) if (t.name == "alignment prototype(Clone)") count++;
            Require(count == 2, "invalid spawn must not instantiate an object");
            placer.Generate(new[] { config }, 1f);
            var originalParent = GameObject.Find("RandomObjectsParent1");
            Require(originalParent != null && originalParent.transform.childCount == 2, "original invalid placement behavior retained");
            Require(Vector3.Distance(originalParent.transform.GetChild(0).position, Assets.Scripts.Game.Spawning.SpawnPositions.INVALID_POS) < .001f, "original sentinel position retained");
            Debug.Log("RandomObjectAlignmentPlayCheck passed: slope alignment, scale and static grouping");
            Debug.Log("RandomObjectAlignmentPlayCheck passed: invalid mod placement skipped; original sentinel behavior retained");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
