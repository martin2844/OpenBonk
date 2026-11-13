using System;
using System.Collections;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Other;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MenuVisualsPlayCheck
{
    const string Pending = "MenuVisualsPlayCheck.pending";
    static MenuVisualsPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("menu visuals runner").AddComponent<MenuVisualsPlayRunner>().Begin(); } }
}

public class MenuVisualsPlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("MenuVisualsPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var root = new GameObject("menu visuals fixture"); root.SetActive(false);
        var visuals = root.AddComponent<MenuVisuals>();
        visuals.terrain = Terrain.CreateTerrainGameObject(new UnityEngine.TerrainData()).GetComponent<Terrain>();
        visuals.sunLight = new GameObject("sun").AddComponent<Light>();
        visuals.camera = new GameObject("camera anchor").transform; visuals.camera.position = new Vector3(1, 2, 3);
        visuals.general = new GameObject("general"); visuals.dungeon = new GameObject("dungeon");
        visuals.forestBg = new GameObject("forest"); visuals.desertBg = new GameObject("desert");
        visuals.ambience = new GameObject("ambience").AddComponent<AudioSource>();
        var forest = ScriptableObject.CreateInstance<StageData>(); forest.fogIntensity = .02f; forest.lightIntensity = .8f;
        var desert = ScriptableObject.CreateInstance<StageData>(); desert.fogIntensity = .01f; desert.lightIntensity = 1.2f;
        var particle = new GameObject("particle prefab"); desert.particles = particle;
        var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest;
        visuals.defaultMapVisuals = map; visuals.defaultStageVisuals = forest;
        root.SetActive(true); yield return null;
        try
        {
            Check(visuals.forestBg.activeSelf && !visuals.desertBg.activeSelf && visuals.general.activeSelf && !visuals.dungeon.activeSelf, "forest startup");
            Check(Mathf.Abs(RenderSettings.fogDensity - .007f) < .00001f && Mathf.Abs(visuals.sunLight.intensity - .8f) < .0001f, "forest fog and light");
            map.eMap = EMap.Desert;
            MapSelectionUi.A_RunConfigChanged?.Invoke(new RunConfig { mapData = map, stageData = desert });
            Check(!visuals.forestBg.activeSelf && visuals.desertBg.activeSelf && visuals.general.activeSelf && !visuals.dungeon.activeSelf, "desert backgrounds");
            Check(Mathf.Abs(RenderSettings.fogDensity - .0048f) < .00001f && Mathf.Abs(visuals.sunLight.intensity - 1.2f) < .0001f, "desert fog and light");
            Check(visuals.particles != null && visuals.particles.transform.parent == visuals.camera && visuals.particles.transform.position == visuals.camera.position, "particles follow camera");
            var currentParticle = visuals.particles;
            MapSelectionUi.A_RunConfigChanged?.Invoke(new RunConfig { mapData = map, stageData = desert });
            Check(visuals.particles == currentParticle, "same stage does not recreate particles");
            var grave = ScriptableObject.CreateInstance<StageData>(); grave.fogIntensity = .03f; map.eMap = EMap.Graveyard;
            MapSelectionUi.A_RunConfigChanged?.Invoke(new RunConfig { mapData = map, stageData = grave });
            Check(!visuals.forestBg.activeSelf && !visuals.desertBg.activeSelf && !visuals.general.activeSelf && visuals.dungeon.activeSelf && Mathf.Abs(visuals.ambience.volume - .45f) < .0001f, "graveyard scenery and audio");
            DestroyImmediate(root);
            Check(MapSelectionUi.A_RunConfigChanged == null, "event cleanup");
            Debug.Log("MenuVisualsPlayCheck passed: forest, desert and graveyard transitions, fog, light, particles and cleanup");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
