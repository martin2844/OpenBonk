using System;
using Assets.Scripts.Managers;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapControllerMainMenuPortCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        if (!MapController.IsMainMenu()) throw new Exception("Android MainMenu not recognized");
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity");
        if (MapController.IsMainMenu()) throw new Exception("GeneratedMap mistaken for menu");
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/FinalBossMap.unity");
        if (MapController.IsMainMenu()) throw new Exception("FinalBossMap mistaken for menu");
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        if (!MapController.IsMainMenu()) throw new Exception("Return to MainMenu not recognized");
        Debug.Log("MapControllerMainMenuPortCheck passed: actual Android scenes and menu return");
    }
}
