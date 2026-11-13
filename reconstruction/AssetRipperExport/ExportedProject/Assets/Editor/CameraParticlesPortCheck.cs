using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEngine;

public static class CameraParticlesPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("CameraParticlesPortCheck: " + message); }
    public static void Run()
    {
        string[] names = { "ForestParticles", "Forest2Particles", "Forest3Particles", "DesertParticles", "DungeonParticles", "LavaParticles" };
        var oldPlayer = MyPlayer.Instance;
        var playerObject = new GameObject("test player"); playerObject.SetActive(false);
        var player = playerObject.AddComponent<MyPlayer>();
        try
        {
            foreach (var name in names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
                Require(prefab != null, name + " missing prefab");
                var instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    var component = instance.GetComponentInChildren<CameraParticles>(true);
                    Require(component != null, name + " missing controller");
                    var update = typeof(CameraParticles).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                    component.transform.position = new Vector3(2, 3, 4); MyPlayer.Instance = null;
                    update.Invoke(component, null); Require(component.transform.position == new Vector3(2, 3, 4), name + " moved without player");
                    MyPlayer.Instance = player; player.transform.position = new Vector3(-11, 7, 23);
                    update.Invoke(component, null); Require(component.transform.position == player.transform.position, name + " did not follow player");
                    player.transform.position = new Vector3(19, 8, -6);
                    update.Invoke(component, null); Require(component.transform.position == player.transform.position, name + " did not update after player moved");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            int stagesWithParticles = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:StageData"))
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid));
                if (stage == null || stage.particles == null) continue;
                var source = stage.particles.GetComponentInChildren<CameraParticles>(true);
                if (source == null) continue;
                var spawned = stage.SpawnParticles();
                try
                {
                    Require(spawned != null, stage.name + " failed to spawn particles");
                    var controller = spawned.GetComponentInChildren<CameraParticles>(true);
                    Require(controller != null, stage.name + " spawned without controller");
                    player.transform.position = new Vector3(31, 12, -17);
                    MyPlayer.Instance = player;
                    typeof(CameraParticles).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                    Require(controller.transform.position == player.transform.position, stage.name + " particles did not follow player");
                    stagesWithParticles++;
                    Debug.Log("CameraParticles stage verified: " + stage.name + " -> " + stage.particles.name);
                }
                finally { if (spawned != null) UnityEngine.Object.DestroyImmediate(spawned); }
            }
            Require(stagesWithParticles > 0, "no stage assets reference camera particles");
            Debug.Log("CameraParticles stage integration passed: " + stagesWithParticles + " stage assets");
            Debug.Log("CameraParticlesPortCheck passed: all six original stage particle prefabs follow player");
        }
        finally { MyPlayer.Instance = oldPlayer; UnityEngine.Object.DestroyImmediate(playerObject); }
    }
}
