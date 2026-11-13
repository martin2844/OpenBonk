using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class FinalFightDefeatPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string msg) { if (!value) throw new Exception("FinalFightDefeatPortCheck: " + msg); }
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, Private).Invoke(obj, null);
    public static void Run()
    {
        var root = new GameObject("final fight fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.weaponInventory = new WeaponInventory();
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSword.asset"); Check(data != null, "original sword asset");
        var weapon = (WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); weapon.weaponData = data; weapon.Disable(); player.inventory.weaponInventory.weapons.Add(data.eWeapon, weapon);
        var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageForest3.asset"); Check(stage != null && stage.stageTimeline != null, "original stage timeline");
        typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { stage });
        typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        var fight = root.AddComponent<FinalFightController>(); var boss = new GameObject("boss").AddComponent<Enemy>(); var other = new GameObject("other").AddComponent<Enemy>(); fight.boss = boss;
        int defeated = 0, times = 0, toggles = 0; bool parameter = true; float observed = -1;
        Action<bool> onDefeat = value => { defeated++; parameter = value; }; Action<float> onTime = value => { times++; observed = value; }; Action<WeaponBase> onToggle = _ => toggles++;
        FinalFightController.A_BossDefeated += onDefeat; FinalFightController.A_BossDefeatedTime += onTime; WeaponInventory.A_WeaponToggled += onToggle;
        try {
            Call(fight, "Awake"); Check(FinalFightController.isFightingFinalBoss, "fight flag on awake");
            var goons = (List<Enemy>)typeof(FinalFightController).GetField("goons", Private).GetValue(fight); goons.Add(other); MyTime.time = 123;
            Enemy.A_EnemyReleasedFromPool(other); Check(goons.Count == 0 && (float)typeof(FinalFightController).GetField("goonsDeadAtTime", Private).GetValue(fight) == 123f, "last minion release time");
            Check(defeated == 0 && fight.boss == boss, "other enemy does not end fight");
            MyTime.stageTimer = 100f; Enemy.A_EnemyReleasedFromPool(boss);
            Check(fight.isBossDefeated && !FinalFightController.isFightingFinalBoss, "boss and fight cleared");
            Check(weapon.enabled && toggles == 1, "weapon restored through inventory event");
            Check(defeated == 1 && !parameter && times == 1 && observed == 100f, "original defeat events and time");
            Check(MyTime.stageTimer == 590f, "stage timer advanced to ten-second grace boundary");
            weapon.Disable(); PlayerHealth.A_Died(); Check(weapon.enabled && toggles == 2, "player death restores weapons");
            fight.boss = boss; FinalFightController.isFightingFinalBoss = true; MyTime.stageTimer = 599f;
            Enemy.A_EnemyReleasedFromPool(boss);
            Check(defeated == 2 && times == 2 && observed == 599f && MyTime.stageTimer == 599f, "late defeat retains stage time");
            Call(fight, "OnDestroy"); weapon.Disable(); PlayerHealth.A_Died?.Invoke(); Check(!weapon.enabled && toggles == 3, "death listener removed");
            Enemy.A_EnemyReleasedFromPool?.Invoke(boss); Check(defeated == 2 && times == 2, "release listener removed");
            Debug.Log("FinalFightDefeatPortCheck passed: enemy release, weapon restore, stage timer, events and unsubscription");
        } finally {
            FinalFightController.A_BossDefeated -= onDefeat; FinalFightController.A_BossDefeatedTime -= onTime; WeaponInventory.A_WeaponToggled -= onToggle;
            UnityEngine.Object.DestroyImmediate(other.gameObject); UnityEngine.Object.DestroyImmediate(boss.gameObject); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null;
        }
    }
}
