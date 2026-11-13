using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyWarningReleaseFixture : EnemySpecialAttackPrefab
{
    public int completions;
    protected override void Init() { CreateWarningSphere(Vector3.zero, () => completions++); }
    public void ReleaseEnemy(Enemy enemy) { OnEnemyDied(enemy); }
}
public static class EnemyWarningReleasePortCheck
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var managers = new GameObject("fixture managers"); managers.SetActive(false);
        PoolManager.Instance = managers.AddComponent<PoolManager>(); EffectManager.Instance = managers.AddComponent<EffectManager>();
        var playerObj = new GameObject("fixture player"); playerObj.SetActive(false);
        var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.statusEffects = new PlayerStatusEffects();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/WarningSphere.prefab");
        if (prefab == null) throw new Exception("WarningSphere prefab missing");
        var warnings = new ObjectPool<GameObject>(() => UnityEngine.Object.Instantiate(prefab), x => x.SetActive(true), x => x.SetActive(false));
        PoolManager.Instance.warningSpherePool = warnings;
        PoolManager.Instance.enemyAttacksPools = new Dictionary<string, ObjectPool<GameObject>>();
        var attackPool = new ObjectPool<GameObject>(() => { var go = new GameObject("fixture attack"); go.AddComponent<EnemyWarningReleaseFixture>(); return go; }, x => x.SetActive(true), x => x.SetActive(false));
        PoolManager.Instance.enemyAttacksPools.Add("fixture", attackPool);
        var attackerObj = new GameObject("attacker"); attackerObj.SetActive(false); var attacker = attackerObj.AddComponent<Enemy>();
        var otherObj = new GameObject("other"); otherObj.SetActive(false); var other = otherObj.AddComponent<Enemy>();
        var attack = attackPool.Get().GetComponent<EnemyWarningReleaseFixture>();
        attack.Set(new EnemySpecialAttack { attackName = "fixture", attackRadius = 1, attackChargeTime = 1 }, attacker);
        var warning = UnityEngine.Object.FindObjectOfType<CircleWarning>();
        if (warning == null || warnings.CountActive != 1) throw new Exception("warning not created");
        attack.ReleaseEnemy(other);
        if (attackPool.CountActive != 1 || warnings.CountActive != 1) throw new Exception("other enemy cancelled attack");
        attack.ReleaseEnemy(attacker);
        if (attackPool.CountActive != 0 || warnings.CountActive != 1 || !warning.gameObject.activeSelf) throw new Exception("warning incorrectly cancelled when attack returned");
        MyTime.deltaTime = 1f;
        typeof(CircleWarning).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(warning, null);
        if (attack.completions != 1 || warnings.CountActive != 0) throw new Exception("warning did not complete and return");
        Debug.Log("EnemyWarningReleasePortCheck passed: unrelated enemy ignored, attacker release returns attack, existing warning completes");
    }
}
