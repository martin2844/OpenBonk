#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class AndroidStandaloneInputBuildCheck
{
    public static void Run()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var go = new GameObject("input", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
            var module = go.GetComponent<StandaloneInputModule>();
            module.enabled = false;
            if (AndroidRewiredSceneProbe.EnableStandaloneInput(scene) != 1 || !module.enabled)
                throw new Exception("Disabled standalone input was not enabled.");
            if (AndroidRewiredSceneProbe.EnableStandaloneInput(scene) != 0)
                throw new Exception("Already enabled input should remain enabled.");
            Debug.Log("AndroidStandaloneInputBuildCheck PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
