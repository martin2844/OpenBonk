using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class PickupGroundPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("PickupGroundPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("pickup ground fixture"); root.SetActive(false);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsGround = 1 << 12;
        var manager = root.AddComponent<PickupManager>(); PickupManager.Instance = manager; manager.whatIsPickups = 0;
        typeof(PickupManager).GetField("xpList", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, new PickupStackableList(300, EPickup.Xp));
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab");
        Require(prefab != null, "original XP prefab");
        var created = new System.Collections.Generic.List<GameObject>();
        pool.xpPool = new ObjectPool<GameObject>(() => { var go = UnityEngine.Object.Instantiate(prefab); created.Add(go); return go; });
        var world = new GameObject("WorldCollider"); world.layer = 12; world.transform.position = new Vector3(0f, -.5f, 0f); world.AddComponent<BoxCollider>().size = new Vector3(4f, 1f, 4f);
        var ground = new GameObject("ordinary ground"); ground.layer = 12; ground.transform.position = new Vector3(20f, -.5f, 0f); ground.AddComponent<BoxCollider>().size = new Vector3(4f, 1f, 4f);
        try
        {
            Physics.SyncTransforms();
            var ignored = manager.SpawnPickup(EPickup.Xp, new Vector3(0f, 10f, 0f), 5, false);
            var normal = manager.SpawnPickup(EPickup.Xp, new Vector3(20f, 10f, 0f), 5, false);
            var noGround = manager.SpawnPickup(EPickup.Xp, new Vector3(40f, 10f, 0f), 5, false);
            Require(ignored != null && Mathf.Abs(ignored.transform.position.y - 12f) < .001f, "WorldCollider keeps original height plus hover");
            Require(normal != null && Mathf.Abs(normal.transform.position.y - 2f) < .001f, "ordinary ground hit plus hover");
            Require(noGround != null && Mathf.Abs(noGround.transform.position.y - 12f) < .001f, "missing ground keeps original height plus hover");
            Debug.Log("PickupGroundPortCheck passed: original XP prefab, WorldCollider, ordinary ground and missing ground");
        }
        finally { foreach (var go in created) UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(world); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(root); GameManager.Instance = null; PickupManager.Instance = null; PoolManager.Instance = null; }
    }
}
