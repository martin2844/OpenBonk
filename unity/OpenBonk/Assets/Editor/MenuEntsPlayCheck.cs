using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MenuEntsPlayCheck
{
    const string Pending = "MenuEntsPlayCheck.pending";
    static MenuEntsPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("menu ents runner").AddComponent<MenuEntsPlayRunner>().Begin(); } }
}
public class MenuEntsPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string message) { if (!value) throw new Exception("MenuEntsPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var root = new GameObject("ents fixture"); root.SetActive(false);
        var controller = root.AddComponent<MenuEnts>(); controller.entPrefab = new GameObject("ent template");
        controller.leftPos = new GameObject("left").transform; controller.leftPos.position = new Vector3(-10, 1, 5);
        controller.rightPos = new GameObject("right").transform; controller.rightPos.position = new Vector3(10, 1, 5);
        yield return null;
        try
        {
            UnityEngine.Random.InitState(201);
            typeof(MenuEnts).GetMethod("Start", Private).Invoke(controller, null);
            var list = (IList)typeof(MenuEnts).GetField("ents", Private).GetValue(controller);
            Check(list.Count >= 1 && list.Count <= 6 && !controller.entPrefab.activeSelf, "spawn count and hidden template");
            var fieldObj = list[0].GetType().GetField("obj"); var fieldDir = list[0].GetType().GetField("dir"); var fieldSpeed = list[0].GetType().GetField("speed");
            foreach (var data in list)
            {
                var obj = (GameObject)fieldObj.GetValue(data); int dir = (int)fieldDir.GetValue(data); float speed = (float)fieldSpeed.GetValue(data);
                Check(obj.activeSelf && obj.transform.position.x >= -10 && obj.transform.position.x <= 10 && obj.transform.position.z >= 5 && obj.transform.position.z <= 55, "spawn position");
                Check((dir == -1 || dir == 1) && speed >= 1.19f && speed <= 2.101f, "direction and scaled speed");
                Check(Vector3.Dot(obj.transform.forward, controller.entPrefab.transform.forward * -dir) > .99f, "orientation");
            }
            var first = list[0]; var target = (GameObject)fieldObj.GetValue(first);
            fieldDir.SetValue(first, 1); target.transform.position = new Vector3(11, 8, 12);
            typeof(MenuEnts).GetMethod("Update", Private).Invoke(controller, null);
            Check(Mathf.Approximately(target.transform.position.x, -10) && target.transform.position.y == 8 && target.transform.position.z == 12, "right-edge wrap");
            fieldDir.SetValue(first, -1); target.transform.position = new Vector3(-11, 8, 12);
            typeof(MenuEnts).GetMethod("Update", Private).Invoke(controller, null);
            Check(Mathf.Approximately(target.transform.position.x, 10) && target.transform.position.y == 8 && target.transform.position.z == 12, "left-edge wrap");
            Debug.Log("MenuEntsPlayCheck passed: spawn, direction, scale, orientation and boundary wrap"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
