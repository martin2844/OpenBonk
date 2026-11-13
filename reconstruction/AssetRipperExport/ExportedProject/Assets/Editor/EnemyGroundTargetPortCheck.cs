using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class EnemyGroundTargetPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("enemy target fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(0, 5, 20);
        GameManager.Instance = root.AddComponent<GameManager>(); GameManager.Instance.whatIsGround = 1 << 8; GameManager.Instance.whatIsObjects = 1 << 9;
        PoolManager.Instance = root.AddComponent<PoolManager>(); EffectManager.Instance = root.AddComponent<EffectManager>();
        var warnings = new List<GameObject>();
        PoolManager.Instance.warningSpherePool = new ObjectPool<GameObject>(() => { var go = new GameObject("warning"); var w = go.AddComponent<CircleWarning>(); w.filler = new GameObject("fill").transform; w.filler.SetParent(go.transform); warnings.Add(go); return go; });
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = 8; ground.transform.position = new Vector3(0, 0, 20); ground.transform.localScale = new Vector3(100, 1, 100);
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.layer = 9; obstacle.transform.position = new Vector3(0, 3, 20); obstacle.transform.localScale = new Vector3(100, 1, 100);
        var map = ScriptableObject.CreateInstance<MapData>(); map.mapType = EMapType.Tiles;
        var oldMap = MapController.currentMap; var mapSetter = typeof(MapController).GetProperty("currentMap").GetSetMethod(true); mapSetter.Invoke(null, new object[] { map });
        var single = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RootAttack.prefab")); single.SetActive(false);
        var pig = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Miniboss_Pig.asset");
        var mudConfig = pig.specialAttacks.First(a => a.attackPrefab != null && a.attackPrefab.GetComponent<EnemyProjectileMud>() != null);
        var mud = UnityEngine.Object.Instantiate(mudConfig.attackPrefab); mud.SetActive(false);
        var ent = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage1.asset");
        var multiConfig = ent.specialAttacks.First(a => a.attackName == "RootAttackLines3");
        var multiObject = UnityEngine.Object.Instantiate(multiConfig.attackPrefab); multiObject.SetActive(false);
        var pharaoh = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Pharaoh1.asset");
        var ringConfig = pharaoh.specialAttacks.First(a => a.attackName == "DesertPillarsCircles");
        var ringObject = UnityEngine.Object.Instantiate(ringConfig.attackPrefab); ringObject.SetActive(false);
        var cactusObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemyCactusProjectile.prefab")); cactusObject.SetActive(false);
        var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false); enemyObject.transform.position = new Vector3(0, 5, 0);
        var enemy = enemyObject.AddComponent<Enemy>(); enemy.renderer = enemyObject.AddComponent<MeshRenderer>();
        var attacks = new EnemySpecialAttackPrefab[] { single.GetComponent<EnemySpecialAttackPrefabSingle>(), mud.GetComponent<EnemyProjectileMud>() };
        try
        {
            Check(attacks.All(a => a != null), "original Single and Pig Mud prefabs");
            Physics.SyncTransforms();
            foreach (var attack in attacks)
            {
                var type = attack.GetType(); type.GetField("grounded").SetValue(attack, true); type.GetField("predictive").SetValue(attack, false);
                typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(attack, new object[] { new EnemySpecialAttack { attackRadius = 2f, attackChargeTime = 1f } });
                var init = type.GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
                init.Invoke(attack, null);
                Check(Vector3.Distance(attack.transform.position, new Vector3(0, .5f, 20)) < .001f, type.Name + " must select ground instead of upper object");
                ground.tag = "Ignore"; init.Invoke(attack, null);
                Check(Vector3.Distance(attack.transform.position, new Vector3(0, .5f, 20)) < .001f, type.Name + " uses filtered first hit on tiled map");
                ground.tag = "Untagged"; GameManager.Instance.whatIsGround = 0; init.Invoke(attack, null);
                Check(Vector3.Distance(attack.transform.position, player.transform.position) < .001f, type.Name + " keeps target if raycast misses");
                GameManager.Instance.whatIsGround = 1 << 8;
            }
            var multi = multiObject.GetComponent<EnemySpecialAttackPrefabMulti>(); Check(multi != null, "original multi prefab");
            typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(multi, new object[] { multiConfig });
            typeof(EnemySpecialAttackPrefab).GetField("enemy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(multi, enemy);
            multi.grounded = true;
            var doAttack = typeof(EnemySpecialAttackPrefabMulti).GetMethod("DoAttack", BindingFlags.Instance | BindingFlags.NonPublic);
            int previousWarnings = warnings.Count;
            var routine = (IEnumerator)doAttack.Invoke(multi, null); Check(routine.MoveNext() && warnings.Count == previousWarnings + multi.numLines, "multi warnings");
            Check(warnings.Skip(previousWarnings).All(w => Mathf.Abs(w.transform.position.y - .5f) < .001f), "multi uses ground layer rather than object");
            ground.tag = "Ignore"; previousWarnings = warnings.Count;
            routine = (IEnumerator)doAttack.Invoke(multi, null); Check(routine.MoveNext() && warnings.Skip(previousWarnings).All(w => Mathf.Abs(w.transform.position.y - .5f) < .001f), "multi filtered tiled hit fallback");
            ground.tag = "Untagged";
            var ring = ringObject.GetComponent<EnemySpecialAttackManyRandom>(); Check(ring != null, "original circle prefab"); ring.grounded = true;
            var groundPosition = typeof(EnemySpecialAttackManyRandom).GetMethod("GroundPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            var point = new Vector3(0, 5, 20);
            Check(Mathf.Abs(((Vector3)groundPosition.Invoke(ring, new object[] { point })).y - .5f) < .001f, "circle uses ground rather than object");
            ground.tag = "Ignore";
            Check(Mathf.Abs(((Vector3)groundPosition.Invoke(ring, new object[] { point })).y - .5f) < .001f, "circle filtered tiled hit fallback");
            GameManager.Instance.whatIsGround = 0;
            Check((Vector3)groundPosition.Invoke(ring, new object[] { point }) == point, "circle miss keeps original point");
            var cactus = cactusObject.GetComponent<EnemySpecialAttackCactusProjectile>(); Check(cactus != null && cactus.grounded, "original grounded cactus prefab");
            cactus.predictive = false;
            typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(cactus, new object[] { new EnemySpecialAttack { attackRadius = 2f, attackChargeTime = 1f } });
            typeof(EnemySpecialAttackPrefab).GetField("enemy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cactus, enemy);
            var cactusInit = typeof(EnemySpecialAttackCactusProjectile).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
            cactusInit.Invoke(cactus, null); Check(cactus.transform.position == Vector3.zero, "cactus missing ground uses default hit point");
            GameManager.Instance.whatIsGround = 1 << 8; cactusInit.Invoke(cactus, null);
            Check(Mathf.Abs(cactus.transform.position.y - .5f) < .001f, "cactus filtered tiled hit uses first hit point");
            ground.tag = "Untagged"; cactusInit.Invoke(cactus, null);
            Check(Mathf.Abs(cactus.transform.position.y - .5f) < .001f, "cactus valid ground hit");
            var cactusUpdate = typeof(EnemySpecialAttackCactusProjectile).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var start = (Vector3)typeof(EnemySpecialAttackCactusProjectile).GetField("startPos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cactus);
            var impact = (Vector3)typeof(EnemySpecialAttackCactusProjectile).GetField("impactPos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cactus);
            float oldDelta = MyTime.deltaTime;
            try
            {
                MyTime.deltaTime = .25f; cactusUpdate.Invoke(cactus, null);
                Check(Vector3.Distance(cactus.transform.position, Vector3.Lerp(start, impact, .25f) + Vector3.up * 15f) < .001f, "cactus quarter-flight arc");
                MyTime.deltaTime = 0f; cactusUpdate.Invoke(cactus, null);
                Check(Vector3.Distance(cactus.transform.position, Vector3.Lerp(start, impact, .25f) + Vector3.up * 15f) < .001f, "cactus game-time freeze");
                MyTime.deltaTime = .25f; cactusUpdate.Invoke(cactus, null);
                Check(Vector3.Distance(cactus.transform.position, Vector3.Lerp(start, impact, .5f) + Vector3.up * 20f) < .001f, "cactus midpoint arc");
                MyTime.deltaTime = .5f; cactusUpdate.Invoke(cactus, null);
                Check(Vector3.Distance(cactus.transform.position, impact) < .001f, "cactus reaches impact after charge");
                MyTime.deltaTime = .25f; cactusUpdate.Invoke(cactus, null);
                Check(Vector3.Distance(cactus.transform.position, impact) < .001f, "completed cactus flight remains at impact");
            }
            finally { MyTime.deltaTime = oldDelta; }
            Debug.Log("EnemyGroundTargetPortCheck passed: single, mud, multi, circle and cactus ground targeting; cactus flight and game-time freeze");
        }
        finally
        {
            mapSetter.Invoke(null, new object[] { oldMap }); foreach (var w in warnings) UnityEngine.Object.DestroyImmediate(w);
            UnityEngine.Object.DestroyImmediate(single); UnityEngine.Object.DestroyImmediate(mud); UnityEngine.Object.DestroyImmediate(multiObject); UnityEngine.Object.DestroyImmediate(ringObject); UnityEngine.Object.DestroyImmediate(cactusObject); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(obstacle); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(root);
            MyPlayer.Instance = null; GameManager.Instance = null; PoolManager.Instance = null; EffectManager.Instance = null;
        }
    }
}
