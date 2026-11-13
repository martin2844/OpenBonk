using System;
using System.Reflection;
using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using Assets.Scripts.Game.Spawning;
using UnityEditor;
using UnityEngine;

public static class FrogEggPortCheck
{
    public static void Run()
    {
        var root = new GameObject("frog egg test"); root.SetActive(false);
        try
        {
            foreach (string color in new[] { "Blue", "Red", "Green" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuestEgg" + color + ".prefab");
                var original = prefab == null ? null : prefab.GetComponentInChildren<InteractableEgg>(true);
                Require(original != null && original.frogEnemyData != null && original.unlockAchievement != null && original.renderer != null && original.collider != null && original.frogMinimapMaterial != null && original.breakFx != null, color + " original egg references");
            }
            var egg = root.AddComponent<InteractableEgg>();
            Require(egg.CanInteract() && egg.showOutline, "unused egg can be interacted with and outlined");
            var done = typeof(InteractableEgg).GetField("done", BindingFlags.Instance | BindingFlags.NonPublic); done.SetValue(egg, true);
            Require(!egg.CanInteract() && !egg.Interact(), "used egg cannot be interacted with again");
            var target = new GameObject("target"); target.transform.SetParent(root.transform);
            Require(new InteractableEgg.FrogContainer(target.transform).target == target.transform, "frog target assigned");
            var one = new GameObject("frog one"); one.transform.SetParent(root.transform); var frog = one.AddComponent<Enemy>();
            var two = new GameObject("frog two"); two.transform.SetParent(root.transform); var other = two.AddComponent<Enemy>();
            var list = new List<Enemy> { frog, other };
            typeof(InteractableEgg).GetField("frogEnemies", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(egg, list);
            var playerObject = new GameObject("player"); playerObject.transform.SetParent(root.transform); MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
            MapInfo.mapBoundsLower = new Vector3(-100, 0, -100); MapInfo.mapBoundsUpper = new Vector3(100, 0, 100); MapInfo.mapCenter = Vector3.zero; MyTime.time = 1f;
            var otherTarget = new GameObject("other target"); otherTarget.transform.SetParent(root.transform);
            var targets = new Dictionary<Enemy, InteractableEgg.FrogContainer> { [frog] = new InteractableEgg.FrogContainer(target.transform), [other] = new InteractableEgg.FrogContainer(otherTarget.transform) };
            typeof(InteractableEgg).GetField("frogTargets", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(egg, targets);
            frog.transform.position = Vector3.right * 5; other.transform.position = Vector3.forward * 5;
            typeof(InteractableEgg).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(egg, null);
            Require(frog.state == EEnemyState.FollowTarget && Vector3.Distance(target.transform.position, Vector3.right * 25) < .01f, "nearby frog flees player");
            frog.transform.position = Vector3.right * 95; playerObject.transform.position = Vector3.right * 90; other.transform.position = Vector3.right * 85;
            typeof(InteractableEgg).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(egg, null);
            Require(targets[frog].lockDirectionUntilTime == 11f, "edge locks direction");
            typeof(InteractableEgg).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(egg, null);
            Require(Vector3.Distance(target.transform.position, Vector3.right * 75) < .01f, "locked frog heads toward map center");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "test ground"; ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = new Vector3(0, -1, 0); ground.transform.localScale = new Vector3(200, 2, 200);
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsGroundAndObjects = 1 << ground.layer; manager.whatIsGround = 1 << ground.layer;
            MapInfo.mapSize = new Vector3(100, 10, 100); Physics.SyncTransforms();
            Vector3 sampled = SpawnPositions.GetRandomSpawnPositionOnMap();
            Require(Mathf.Abs(sampled.y) < .01f && Mathf.Abs(sampled.x) <= 50 && Mathf.Abs(sampled.z) <= 50, "random map position lands on ground within map");
            Require(Utility.RaycastUtility.RayToGround(new Vector3(10, 10, 0)) == new Vector3(10, 0, 0), "reward ray reaches ground");
            ground.name = "WorldCollider";
            Require(Utility.RaycastUtility.RayToGround(new Vector3(10, 10, 0)) == new Vector3(10, 10, 0), "world collider leaves reward position unchanged");
            ground.name = "test ground";
            var rewardPrefab = new GameObject("reward prefab"); rewardPrefab.SetActive(false); rewardPrefab.transform.SetParent(root.transform);
            var effectManager = root.AddComponent<EffectManager>();
            effectManager.SpawnChest(rewardPrefab, new Vector3(10, 10, 0));
            GameObject reward = null; foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>()) if (obj.name == "reward prefab(Clone)") { reward = obj; break; }
            Require(reward != null && reward.transform.position == new Vector3(10, 0, 0), "reward chest spawned at ground");
            UnityEngine.Object.DestroyImmediate(reward);
            frog.hp = 50f; frog.maxHp = 100f;
            var hpRoot = new GameObject("health bar"); hpRoot.transform.SetParent(root.transform); var hpBar = hpRoot.AddComponent<EnemyHpBar>(); hpBar.hpBar = new GameObject("bar").transform; hpBar.hpBar.SetParent(hpRoot.transform); hpBar.Set(frog);
            typeof(EnemyHpBar).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hpBar, null);
            Require(Mathf.Abs(hpBar.hpBar.localScale.x - .5f) < .001f && hpRoot.transform.position == frog.GetHeadPosition() + Vector3.up * 1.2f, "enemy health bar follows head and health ratio");
            frog.transform.position = new Vector3(70, 0, 0); playerObject.transform.position = Vector3.zero; target.transform.position = frog.transform.position;
            typeof(InteractableEgg).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(egg, null);
            Require(Mathf.Abs(target.transform.position.y) < .01f && target.transform.position != frog.transform.position && targets[frog].lastFoundRandomPositionTime == MyTime.time, "distant frog selects map position");
            GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(ground);
            MyPlayer.Instance = null;
            typeof(InteractableEgg).GetMethod("OnEnemyReleasedFromPool", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(egg, new object[] { frog });
            Require(list.Count == 1 && list[0] == other, "released frog removed, reward waits for last frog");
            Debug.Log("FrogEggPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("FrogEggPortCheck failed: " + message); }
}
