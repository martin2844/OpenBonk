using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Camera;
using MapArrow = Assets.Scripts.Camera.MinimapArrow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MinimapArrowPlayCheck
{
    const string Pending = "MinimapArrowPlayCheck.pending";
    const string TestScene = "Assets/MinimapArrowCheckTemporary.unity";
    static MinimapArrowPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var originalScene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        MinimapCamera original = null;
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<MinimapCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (candidate.arrowPrefab != null) { original = candidate; break; }
        if (original == null) throw new Exception("Original minimap arrow prefab missing");
        var test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var root = new GameObject("minimap test"); SceneManager.MoveGameObjectToScene(root, test);
        var map = root.AddComponent<MinimapCamera>();
        var prefab = UnityEngine.Object.Instantiate(original.arrowPrefab.gameObject); SceneManager.MoveGameObjectToScene(prefab, test); prefab.SetActive(false); map.arrowPrefab = prefab.transform;
        if (original.enemyIconPrefab == null) throw new Exception("Original minimap enemy icon prefab missing");
        var enemyPrefab = UnityEngine.Object.Instantiate(original.enemyIconPrefab); SceneManager.MoveGameObjectToScene(enemyPrefab, test); enemyPrefab.SetActive(false); map.enemyIconPrefab = enemyPrefab;
        EditorSceneManager.SetActiveScene(test); EditorSceneManager.CloseScene(originalScene, true); EditorSceneManager.SaveScene(test, TestScene);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("minimap check runner").AddComponent<MinimapArrowPlayRunner>().StartCheck(); } }
}
public class MinimapArrowPlayRunner : MonoBehaviour
{
    public void StartCheck() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        MapArrow arrow = null; Material material = null;
        try
        {
            var map = UnityEngine.Object.FindAnyObjectByType<MinimapCamera>();
            var player = new GameObject("player"); player.SetActive(false); MyPlayer.Instance = player.AddComponent<MyPlayer>();
            MyPlayer.Instance.playerInput = player.AddComponent<PlayerInput>();
            MyPlayer.Instance.playerMovement = player.AddComponent<PlayerMovement>();
            MyPlayer.Instance.playerMovement.orientation = new GameObject("player orientation").transform;
            MyPlayer.Instance.playerMovement.orientation.rotation = Quaternion.Euler(0, 35, 0);
            MyPlayer.Instance.playerInput.cameraRotation = new Vector3(0, 60, 0);
            map.minimapCamera = new GameObject("camera").AddComponent<Camera>(); map.minimapCamera.orthographicSize = 20f;
            map.playerIcon = new GameObject("player icon").transform;
            float angle = -1f; Action<float> rotationListener = value => angle = value; MinimapCamera.A_RotationUpdated += rotationListener;
            typeof(MinimapCamera).GetMethod("StaticOrientation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
            Require(Quaternion.Angle(map.transform.rotation, Quaternion.Euler(90, 0, 0)) < .01f && angle == 0, "static map orientation");
            typeof(MinimapCamera).GetMethod("DynamicOrientation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
            Require(Quaternion.Angle(map.transform.rotation, Quaternion.Euler(90, 60, 0)) < .01f && Mathf.Abs(angle - 35) < .01f, "dynamic map orientation and event");
            MinimapCamera.A_RotationUpdated -= rotationListener;
            map.transform.rotation = Quaternion.identity;
            var target = new GameObject("target").transform;
            map.AddArrow(target, Color.red);
            var dict = (Dictionary<Transform, MapArrow>)typeof(MinimapCamera).GetField("arrowDict", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
            Require(dict.TryGetValue(target, out arrow) && arrow.target == target && arrow.gameObject.activeSelf && arrow.transform.parent == null, "marker creation");
            material = arrow.arrowRenderer.material;
            Require(material.GetColor("_Color") == Color.red, "marker color");
            var update = typeof(MinimapCamera).GetMethod("UpdateArrows", BindingFlags.Instance | BindingFlags.NonPublic);
            target.position = Vector3.right * 100f;
            update.Invoke(map, null);
            Require(Vector3.Distance(arrow.transform.position, map.transform.position + new Vector3(16f, -20f, 0f)) < .01f, "far target marker position");
            Require(Quaternion.Angle(arrow.transform.rotation, Quaternion.Euler(0, 90, 0)) < .01f, "far target marker rotation");
            target.position = Vector3.right * 5f; update.Invoke(map, null);
            Require(!arrow.gameObject.activeSelf, "near target hides marker");
            target.position = Vector3.forward * 100f; update.Invoke(map, null); update.Invoke(map, null);
            Require(arrow.gameObject.activeSelf && Vector3.Distance(arrow.transform.position, map.transform.position + new Vector3(0, -20, 16)) < .01f, "far target reactivates and moves marker");
            map.RemoveArrow(target);
            Require(dict.Count == 0, "marker removed from dictionary");
            map.RemoveArrow(target);
            var crypt = new GameObject("crypt").AddComponent<InteractableCrypt>();
            crypt.transform.position = Vector3.right * 10f;
            typeof(MinimapCamera).GetMethod("TryFindBossSpawner", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
            typeof(MinimapCamera).GetMethod("TrySpotBossSpawner", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
            Require(dict.TryGetValue(crypt.transform, out var cryptArrow) && cryptArrow.arrowRenderer.material.GetColor("_Color") == Color.red, "nearby crypt gets boss marker");
            typeof(MinimapCamera).GetMethod("OnBossSpawnerInteract", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
            Require(dict.Count == 0, "boss marker removed on interaction");
            typeof(MinimapCamera).GetMethod("OnBossSpawnerCompleted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, new object[] { true });
            Require(dict.TryGetValue(crypt.transform, out var portalArrow) && Mathf.Abs(portalArrow.arrowRenderer.material.GetColor("_Color").g - 162f / 255f) < .001f, "portal marker added");
            map.RemoveArrow(crypt.transform);
            var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false);
            var enemy = enemyObject.AddComponent<Enemy>();
            enemy.renderer = new GameObject("enemy render").AddComponent<MeshRenderer>(); enemy.renderer.transform.position = new Vector3(12, 3, 7);
            var iconMaterial = map.enemyIconPrefab.GetComponent<MeshRenderer>().sharedMaterial;
            map.AddEnemyMinimapIcon(enemy, iconMaterial, 2f);
            var icons = (Dictionary<Enemy, GameObject>)typeof(MinimapCamera).GetField("enemyIconDictionary", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
            Require(icons.TryGetValue(enemy, out var enemyIcon) && enemyIcon.activeSelf && enemyIcon.transform.parent == null && enemyIcon.GetComponent<MeshRenderer>().sharedMaterial == iconMaterial, "enemy icon created");
            Require(Vector3.Distance(enemyIcon.transform.localScale, map.enemyIconPrefab.transform.localScale * 2f) < .001f, "enemy icon scale");
            typeof(MinimapCamera).GetMethod("UpdateEnemyMinimapIcons", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, null);
            Require(enemyIcon.transform.position == enemy.renderer.transform.position, "enemy icon follows renderer");
            typeof(MinimapCamera).GetMethod("OnEnemySpawn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, new object[] { enemy });
            Require(dict.ContainsKey(enemy.transform), "target of interest arrow created");
            typeof(MinimapCamera).GetMethod("OnEnemyDied", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(map, new object[] { enemy });
            Require(!dict.ContainsKey(enemy.transform) && icons.Count == 0, "enemy release removes icon and arrow");
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return null;
        yield return null;
        try { Require(arrow == null && material == null, "marker and material destroyed"); Debug.Log("MinimapArrowPlayCheck passed"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("MinimapArrowPlayCheck failed: " + message); }
}
