#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Research-only build variant. This does not remove Rewired code from IL2CPP.
// It removes the desktop manager components from the build's scene copy while
// retaining MyInputManager, ControllerShaker and the shared GameObject.
public sealed class AndroidRewiredSceneProbe : IProcessSceneWithReport
{
    public int callbackOrder => 1000;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || report.summary.platform != BuildTarget.Android || (Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_SCENE") != "1" && Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_CODE") != "1")) return;
        int enabled = EnableStandaloneInput(scene);
        if (enabled != 0) Debug.Log("Enabled Android standalone input modules=" + enabled + " on " + scene.name);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var manager in root.GetComponentsInChildren<AlwaysManager>(true))
        {
            if (manager.rewiredManager == null) throw new BuildFailedException("Rewired scene probe: manager GameObject missing");
            var target = manager.rewiredManager;
            var rewired = target.GetComponent<Rewired.InputManager>();
            var store = target.GetComponent<UserDataStore_FileCustom>();
            var input = target.GetComponent<MyInputManager>();
            var shaker = target.GetComponent<ControllerShaker>();
            if (rewired == null || store == null || input == null || shaker == null) throw new BuildFailedException("Rewired scene probe: unexpected manager layout");
            input.userDataStore = null;
            UnityEngine.Object.DestroyImmediate(store);
            UnityEngine.Object.DestroyImmediate(rewired);
            Debug.Log("Rewired scene probe: removed Rewired manager and store; retained Android input and vibration on " + scene.name);
        }
    }

    internal static int EnableStandaloneInput(Scene scene)
    {
        int enabled = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var events in root.GetComponentsInChildren<EventSystem>(true))
        foreach (var module in events.GetComponents<StandaloneInputModule>())
        {
            if (module.enabled) continue;
            module.enabled = true;
            enabled++;
        }
        return enabled;
    }
}
#endif
