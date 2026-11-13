using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VersionNumberSceneCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("VersionNumberSceneCheck: " + message); }
    public static void Run()
    {
        var original = SteamManager.A_Initialized;
        try
        {
            foreach (var sceneName in new[] { "MainMenu", "GeneratedMap", "FinalBossMap" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + sceneName + ".unity", OpenSceneMode.Single);
                var components = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<VersionNumber>(true)).ToArray();
                Check(components.Length > 0, sceneName + " version component");
                foreach (var component in components)
                {
                    Check(component.t_version != null, sceneName + " text reference");
                    var start = typeof(VersionNumber).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
                    var destroy = typeof(VersionNumber).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
                    SteamManager.A_Initialized = null;
                    start.Invoke(component, null);
                    Check(component.t_version.text == "version " + Application.version, sceneName + " initial version");
                    component.t_version.text = "stale";
                    SteamManager.A_Initialized?.Invoke();
                    Check(component.t_version.text == "version " + Application.version, sceneName + " initialized refresh");
                    destroy.Invoke(component, null);
                    Check(SteamManager.A_Initialized == null, sceneName + " event cleanup");
                }
            }
            Debug.Log("VersionNumberSceneCheck passed: original scene bindings, version text, refresh and cleanup");
        }
        finally
        {
            SteamManager.A_Initialized = original;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
