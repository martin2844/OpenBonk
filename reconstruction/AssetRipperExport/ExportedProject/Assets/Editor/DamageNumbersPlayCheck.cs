using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DamageNumbersPlayCheck
{
    const string Pending = "DamageNumbersPlayCheck.pending";
    static DamageNumbersPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("damage popup runner").AddComponent<DamageNumbersPlayRunner>().Begin(); } }
}
public class DamageNumbersPlayRunner : MonoBehaviour
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("DamageNumbersPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        PoolManager pool = null; EffectManager effects = null; DamageNumbers popup = null;
        try
        {
            var state = new GameObject("pool state"); state.SetActive(false);
            pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.damageNumbersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DamageNumbersNew.prefab");
            var canvas = new GameObject("numbers canvas", typeof(RectTransform), typeof(Canvas)); canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace; pool.damageNumbersParent = canvas.transform;
            typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null);
            effects = state.AddComponent<EffectManager>(); EffectManager.Instance = effects;
            var camera = new GameObject("player camera"); camera.SetActive(false); PlayerCamera.Instance = camera.AddComponent<PlayerCamera>(); camera.transform.position = new Vector3(0, 2, -10);
            Require(DamageNumbers.FormatDamageNumber(9999f) == "9,999" && DamageNumbers.FormatDamageNumber(10000f) == "10K" && DamageNumbers.FormatDamageNumber(12500f) == "12.5K", "original formatting thresholds");
            effects.PopupText("Block", Color.cyan, new Vector3(1, 2, 3), 22);
            popup = pool.damageNumbersParent.GetComponentInChildren<DamageNumbers>(true);
            Require(popup != null && popup.gameObject.activeSelf && popup.damageText.text == "Block" && popup.damageText.fontSize == 22 && popup.damageText.color == Color.cyan, "original prefab shows block text");
            Require(pool.damageNumbersPool.CountAll == 1 && popup.transform.position == new Vector3(1, 2, 3), "spawn and parent placement");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        float timeout = Time.realtimeSinceStartup + 4f;
        while (popup.gameObject.activeSelf && Time.realtimeSinceStartup < timeout) yield return null;
        try
        {
            Require(!popup.gameObject.activeSelf && pool.damageNumbersPool.CountInactive == 1, "automatic pool return");
            effects.PopupText(12500f, Color.red, Vector3.zero, 24);
            Require(popup.gameObject.activeSelf && popup.damageText.text == "12.5K" && pool.damageNumbersPool.CountAll == 1, "number popup reuses pooled instance");
            var saveObject = new GameObject("save"); saveObject.SetActive(false); var save = saveObject.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile();
            Require(save.config.cfVisualsSettings.damage_numbers == 1 && save.config.cfVisualsSettings.particle_opacity == 1f && save.config.cfVisualsSettings.xp_gold_hud == 1, "original visual setting defaults");
            var target = new GameObject("enemy"); target.SetActive(false); var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.hp = enemy.maxHp = 1000f;
            typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f);
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); target.SetActive(true);
            int before = pool.damageNumbersPool.CountAll;
            enemy.DamageExternal(new DamageContainer(0f, "test") { damage = 25f, crit = true });
            Require(enemy.hp == 975f && pool.damageNumbersPool.CountAll == before + 2, "enemy hit emits damage and critical label");
            var numbers = pool.damageNumbersParent.GetComponentsInChildren<DamageNumbers>();
            bool sawNumber = false, sawCrit = false;
            foreach (var n in numbers) { if (n.damageText.text == "25" && n.damageText.color == new Color(1f, 235f / 255f, 4f / 255f, 1f)) sawNumber = true; if (n.damageText.text == "CRIT!") sawCrit = true; }
            Require(sawNumber && sawCrit, "critical damage uses original color and label");
            save.config.cfVisualsSettings.damage_numbers = 0; before = pool.damageNumbersPool.CountAll;
            enemy.DamageExternal(new DamageContainer(0f, "test") { damage = 1f });
            Require(enemy.hp == 974f && pool.damageNumbersPool.CountAll == before, "disabled setting suppresses enemy popups");
            Require(new DamageContainer(0f, "test") { damageEffect = EDamageEffect.Poison }.GetColor() == MyColorUtility.poisonColor && MyColorUtility.poisonColor.a == 1f, "original poison color is opaque");
            Debug.Log("DamageNumbersPlayCheck passed: original popup prefab, number formatting, timed return and reuse"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
