using System;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEngine;

public static class GravestonePortCheck
{
    public static void Run()
    {
        var previousPlayer = MyPlayer.Instance;
        var previousUi = UiManager.Instance;
        var previousEnemies = EnemyManager.Instance;
        var previousData = DataManager.Instance;
        var counter = typeof(InteractableGravestone).GetField("numInteractions", BindingFlags.Static | BindingFlags.NonPublic);
        int oldCounter = (int)counter.GetValue(null);
        var playerObject = new GameObject("gravestone test player");
        GameObject gravestoneObject = null, detachedFx = null;
        try
        {
            MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
            MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            MyPlayer.Instance.inventory.statInventory = new StatInventory();
            UiManager.Instance = null; EnemyManager.Instance = null; DataManager.Instance = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Gravestone.prefab");
            if (prefab == null) throw new Exception("Gravestone prefab missing");
            gravestoneObject = UnityEngine.Object.Instantiate(prefab);
            var grave = gravestoneObject.GetComponent<InteractableGravestone>();
            if (grave == null || grave.fx == null || grave.statEffects == null || grave.statEffects.Length != 2)
                throw new Exception("Original gravestone references missing");
            typeof(InteractableGravestone).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grave, null);
            var select = typeof(InteractableGravestone).GetMethod("GetGhostForInteraction", BindingFlags.Static | BindingFlags.NonPublic);
            var expected = new[] { EEnemy.Ghost, EEnemy.GreaterGhost, EEnemy.GhostPurple, EEnemy.GhostRed, EEnemy.GhostRed };
            for (int i = 0; i < expected.Length; i++)
                if ((EEnemy)select.Invoke(null, new object[] { i }) != expected[i]) throw new Exception("Ghost sequence differs at " + i);
            if (!grave.CanInteract() || !grave.ShowInDebug() || grave.GetDebugName() != "Gravestones") throw new Exception("Initial interaction state differs");
            detachedFx = grave.fx;
            if (!grave.Interact() || grave.Interact() || grave.CanInteract()) throw new Exception("Gravestone single-use state differs");
            if (gravestoneObject.activeSelf || !detachedFx.activeSelf || detachedFx.transform.parent != null) throw new Exception("Gravestone visual lifecycle differs");
            foreach (var effect in grave.statEffects)
                if (!MyPlayer.Instance.inventory.statInventory.permanentChanges.TryGetValue(effect.statModifier.stat, out var list) || list.Count != 1)
                    throw new Exception("Permanent gravestone stat not applied once");
            if ((int)counter.GetValue(null) != 1) throw new Exception("Gravestone interaction count differs");
            Debug.Log("GravestonePortCheck passed: original prefab, two stats, single use, effect lifecycle and ghost sequence");
        }
        finally
        {
            MyPlayer.Instance = previousPlayer; UiManager.Instance = previousUi; EnemyManager.Instance = previousEnemies; DataManager.Instance = previousData;
            counter.SetValue(null, oldCounter);
            if (detachedFx != null) UnityEngine.Object.DestroyImmediate(detachedFx);
            if (gravestoneObject != null) UnityEngine.Object.DestroyImmediate(gravestoneObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
