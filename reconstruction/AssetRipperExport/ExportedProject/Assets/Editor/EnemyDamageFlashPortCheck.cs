using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class EnemyDamageFlashPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception("EnemyDamageFlashPortCheck: " + message); }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, P).SetValue(target, value);
    static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, P).Invoke(target, args);
    public static void Run()
    {
        var root = new GameObject("controlled enemy damage flash"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldTime = MyTime.time;
        var save = root.AddComponent<SaveManager>();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        save.config = new ConfigSaveFile { cfVisualsSettings = new CFVisualsSettings { damage_flash = 1 } };
        var enemyObject = new GameObject("target"); var enemy = enemyObject.AddComponent<Enemy>();
        var data = ScriptableObject.CreateInstance<EnemyData>();
        var material = new Material(Shader.Find("Standard")); var white = new Material(Shader.Find("Standard"));
        enemy.renderer = enemyObject.AddComponent<MeshRenderer>(); enemy.renderer.sharedMaterial = material; enemy.whiteMaterial = white;
        data.material = material; typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        var rb = enemyObject.AddComponent<Rigidbody>(); enemy.enemyMovement = enemyObject.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = rb; enemy.enemyMovement.enemy = enemy;
        enemy.hp = 100; Set(enemy, "controlHp", 100f);
        try
        {
            MyTime.time = 10f;
            Call(enemy, "Damage", new DamageContainer(0f, "test") { damage = 1f });
            Require(enemy.renderer.sharedMaterial == white, "surviving hit did not use white shared material");
            float stop = (float)typeof(Enemy).GetField("stopFlashTime", P).GetValue(enemy);
            float ready = (float)typeof(Enemy).GetField("readyToFlashTime", P).GetValue(enemy);
            Require(Math.Abs(stop - 10.08f) < .001f && Math.Abs(ready - 10.22f) < .001f, "timers");
            MyTime.time = stop; Call(enemy, "UpdateDamageFlash"); Require(enemy.renderer.sharedMaterial == white, "strict end boundary");
            MyTime.time = stop + .001f; Call(enemy, "UpdateDamageFlash"); Require(enemy.renderer.sharedMaterial == material, "restore original shared material");
            Call(enemy, "Damage", new DamageContainer(0f, "test") { damage = 1f }); Require(enemy.renderer.sharedMaterial == material, "cooldown guard");
            MyTime.time = ready; Call(enemy, "Damage", new DamageContainer(0f, "test") { damage = 1f }); Require(enemy.renderer.sharedMaterial == material, "strict cooldown boundary");
            MyTime.time = ready + .001f; Call(enemy, "Damage", new DamageContainer(0f, "test") { damage = 1f }); Require(enemy.renderer.sharedMaterial == white, "cooldown expiry");
            Call(enemy, "UpdateDamageFlash"); Require(enemy.renderer.sharedMaterial == white, "stationary game time keeps flash");
            enemy.hp = 0; MyTime.time += 1f; Call(enemy, "UpdateDamageFlash"); Require(enemy.renderer.sharedMaterial == white, "dead enemy material not restored");
            enemy.renderer.sharedMaterial = material; enemy.hp = 100; Set(enemy, "controlHp", 100f); save.config.cfVisualsSettings.damage_flash = 0;
            Call(enemy, "Damage", new DamageContainer(0f, "test") { damage = 1f }); Require(enemy.renderer.sharedMaterial == material, "disabled setting");
            save.config.cfVisualsSettings.damage_flash = 1; enemy.hp = 1; Set(enemy, "controlHp", 1f); Set(enemy, "deathFunctionCalled", true);
            Call(enemy, "Damage", new DamageContainer(0f, "test") { damage = 2f }); Require(enemy.renderer.sharedMaterial == material, "fatal hit does not begin flash");
            Debug.Log("EnemyDamageFlashPortCheck passed: surviving/fatal hits, setting, game clock, strict boundaries, cooldown and shared materials");
        }
        finally
        {
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); MyTime.time = oldTime;
            UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(white);
        }
    }
}
