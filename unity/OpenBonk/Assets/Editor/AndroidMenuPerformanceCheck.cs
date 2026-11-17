using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class AndroidMenuPerformanceCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        var menu = UnityEngine.Object.FindObjectOfType<MainMenu>();
        if (menu == null || menu.menuCamera == null || menu.menuCamera.camera == null)
            throw new Exception("Main menu camera is missing");
        var volume = menu.menuCamera.camera.GetComponent<PostProcessVolume>();
        if (volume == null || volume.sharedProfile == null ||
            !volume.sharedProfile.TryGetSettings(out DepthOfField shared) || !shared.active || !shared.enabled.value)
            throw new Exception("Expected enabled menu depth of field is missing");
        var method = typeof(MainMenu).Assembly.GetType("AndroidPerformanceSettings")?.GetMethod(
            "DisableMenuDepthOfField", BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null || !(bool)method.Invoke(null, new object[] { menu.menuCamera.camera }))
            throw new Exception("Could not configure the menu volume");
        if (!volume.profile.TryGetSettings(out DepthOfField runtime) || runtime.enabled.value || !runtime.enabled.overrideState)
            throw new Exception("Menu runtime profile still enables depth of field");
        if (!shared.enabled.value || shared == runtime)
            throw new Exception("The shared profile was changed");
        if ((bool)method.Invoke(null, new object[] { null })) throw new Exception("Null camera must be ignored");
        Debug.Log("AndroidMenuPerformanceCheck PASS");
    }
}
