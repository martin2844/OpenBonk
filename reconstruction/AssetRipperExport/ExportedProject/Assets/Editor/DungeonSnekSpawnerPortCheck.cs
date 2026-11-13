using System;
using System.Reflection;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DungeonSnekSpawnerPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("DungeonSnekSpawnerPortCheck: " + message); }
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GeneratedMap.unity", OpenSceneMode.Single);
        DungeonSnekSpawner spawner = null;
        foreach (var root in scene.GetRootGameObjects()) { spawner = root.GetComponentInChildren<DungeonSnekSpawner>(true); if (spawner != null) break; }
        Require(spawner != null && spawner.snek != null && spawner.chest != null, "original scene wiring");
        var go = new GameObject("test save manager"); go.SetActive(false);
        var save = go.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var start = typeof(DungeonSnekSpawner).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (bool unlocked in new[] { false, true }) foreach (bool random in new[] { false, true })
        {
            save.progression.achievements.Clear(); if (unlocked) save.progression.achievements.Add("a_snek");
            RsgController.isCurrentMapRandomSeed = random;
            start.Invoke(spawner, null);
            bool expected = !unlocked && random;
            Require(spawner.snek.activeSelf == expected && spawner.chest.activeSelf != expected, "wrong selection for unlocked=" + unlocked + " random=" + random);
        }
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log("DungeonSnekSpawnerPortCheck passed: original scene and all achievement/seed combinations");
    }
}
