using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BossHealthBarPlayCheck
{
    const string Pending = "BossHealthBarPlayCheck.pending";
    static BossHealthBarPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("boss bar test runner").AddComponent<BossHealthBarPlayRunner>().Begin(); } }
}
public class BossHealthBarPlayRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("BossHealthBarPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try
        {
            var root = new GameObject("boss UI"); root.SetActive(false);
            var manager = root.AddComponent<TargetOfInterestUi>();
            var rowObject = new GameObject("boss row", typeof(RectTransform)); rowObject.transform.SetParent(root.transform); rowObject.SetActive(false);
            var row = rowObject.AddComponent<TargetOfInterestPrefab>(); row.canvasGroup = rowObject.AddComponent<CanvasGroup>();
            row.hpBar = new GameObject("health", typeof(RectTransform)).AddComponent<RawImage>(); row.hpBar.transform.SetParent(rowObject.transform);
            row.t_hp = new GameObject("hp text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); row.t_hp.transform.SetParent(rowObject.transform);
            row.t_armor = new GameObject("armor text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); row.t_armor.transform.SetParent(rowObject.transform);
            row.defaultColor = Color.red; row.invulnerableColor = Color.blue;
            manager.prefabs = new List<TargetOfInterestPrefab> { row }; root.SetActive(true);
            var enemies = new GameObject("enemies"); enemies.SetActive(false);
            var first = new GameObject("first boss").AddComponent<Enemy>(); first.transform.SetParent(enemies.transform); first.maxHp = 200; first.hp = 150;
            var second = new GameObject("second boss").AddComponent<Enemy>(); second.transform.SetParent(enemies.transform); second.maxHp = 400; second.hp = 300;
            Enemy.A_TargetOfInterestSpawn?.Invoke(first);
            typeof(TargetOfInterestUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Require(rowObject.activeSelf && Mathf.Approximately(row.hpBar.transform.localScale.x, .75f) && row.t_hp.text == "150 / 200", "spawn displays boss health");
            Enemy.A_TargetOfInterestSpawn?.Invoke(second);
            typeof(TargetOfInterestUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Require(manager.prefabs.Count == 2 && manager.prefabs[1].gameObject.activeSelf && manager.prefabs[1].t_hp.text == "300 / 400", "additional boss gets its own row");
            first.hp = 100; Enemy.A_HealthChange?.Invoke(first);
            Require(Mathf.Approximately(row.hpBar.transform.localScale.x, .5f) && row.t_hp.text == "100 / 200", "health event updates displayed boss");
            Enemy.A_ArmorChanged?.Invoke(first, 1, 2); Require(row.t_armor.text.Contains("ShieldBroken"), "armor changes are displayed");
            Enemy.A_InvulnerableChanged?.Invoke(first, true); Require(row.hpBar.color == Color.blue, "invulnerability color");
            Enemy.A_EnemyReleasedFromPool?.Invoke(first);
            Require(manager.prefabs.Exists(r => r.gameObject.activeSelf && r.t_hp.text == "300 / 400" && r.hpBar.color == Color.red), "remaining boss stays visible after release");
            Enemy.A_EnemyReleasedFromPool?.Invoke(second); Require(!rowObject.activeSelf, "bar hides after final boss");
            var bosses = new List<Enemy>();
            for (int i = 0; i < 12; i++)
            {
                var boss = new GameObject("boss " + i).AddComponent<Enemy>(); boss.transform.SetParent(enemies.transform);
                boss.maxHp = 100; boss.hp = 40 + i; bosses.Add(boss); Enemy.A_TargetOfInterestSpawn?.Invoke(boss);
            }
            typeof(TargetOfInterestUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Require(manager.prefabs.Count >= 12, "twelve boss rows allocated");
            foreach (var boss in bosses) Require(manager.prefabs.Exists(r => r.gameObject.activeSelf && r.t_hp.text == (40 + bosses.IndexOf(boss)) + " / 100"), "every boss shown");
            Enemy.A_EnemyReleasedFromPool?.Invoke(bosses[3]);
            Require(manager.prefabs.FindAll(r => r.gameObject.activeSelf).Count == 11, "release hides only one of twelve rows");
            for (int i = 0; i < bosses.Count; i++) if (i != 3) Enemy.A_EnemyReleasedFromPool?.Invoke(bosses[i]);
            Require(manager.prefabs.FindAll(r => r.gameObject.activeSelf).Count == 0, "all rows hide after release");
            int allocatedRows = manager.prefabs.Count;
            for (int i = 0; i < 7; i++) Enemy.A_TargetOfInterestSpawn?.Invoke(bosses[i]);
            Enemy.A_TargetOfInterestSpawn?.Invoke(bosses[0]);
            typeof(TargetOfInterestUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Require(manager.prefabs.Count == allocatedRows && manager.prefabs.FindAll(r => r.gameObject.activeSelf).Count == 7, "later bosses reuse rows and duplicate events do not create duplicates");
            var groupedTemplate = Instantiate(row);
            groupedTemplate.gameObject.SetActive(false);
            DestroyImmediate(root);
            var groupedRoot = new GameObject("grouped boss UI", typeof(RectTransform)); groupedRoot.SetActive(false);
            var groupedRect = (RectTransform)groupedRoot.transform; groupedRect.sizeDelta = new Vector2(1200, 1080);
            var groupedManager = groupedRoot.AddComponent<TargetOfInterestUi>();
            groupedManager.prefabs = new List<TargetOfInterestPrefab>();
            for (int g = 0; g < 2; g++)
            {
                var group = new GameObject("group " + g, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                group.transform.SetParent(groupedRoot.transform);
                for (int i = 0; i < 3; i++)
                {
                    var groupedRow = Instantiate(groupedTemplate, group.transform);
                    groupedRow.gameObject.SetActive(false);
                    groupedManager.prefabs.Add(groupedRow);
                }
            }
            groupedRoot.SetActive(true);
            foreach (var boss in bosses) Enemy.A_TargetOfInterestSpawn?.Invoke(boss);
            typeof(TargetOfInterestUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(groupedManager, null);
            Require(groupedRoot.transform.childCount == 4 && groupedManager.prefabs.Count == 12, "twelve targets create four horizontal groups");
            Require(Mathf.Approximately(groupedRect.sizeDelta.x, 936f), "multiple rows reserve space beside objectives");
            foreach (var boss in bosses) Require(groupedManager.prefabs.Exists(r => r.gameObject.activeSelf && r.t_hp.text == (40 + bosses.IndexOf(boss)) + " / 100"), "grouped rows associate each boss");
            foreach (var boss in bosses) Enemy.A_EnemyReleasedFromPool?.Invoke(boss);
            Require(groupedManager.prefabs.FindAll(r => r.gameObject.activeSelf).Count == 0, "grouped rows hide after release");
            Require(Mathf.Approximately(groupedRect.sizeDelta.x, 1200f), "single-row width restored after targets leave");
            foreach (var boss in bosses) Enemy.A_TargetOfInterestSpawn?.Invoke(boss);
            Enemy.A_EnemyReleasedFromPool?.Invoke(bosses[0]);
            typeof(TargetOfInterestUi).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(groupedManager, null);
            Require(groupedRoot.transform.childCount == 4 && groupedManager.prefabs.FindAll(r => r.gameObject.activeSelf).Count == 11, "grouped rows are reused and released pending targets stay hidden");
            Destroy(groupedTemplate.gameObject); Destroy(groupedRoot); Destroy(enemies);
            Debug.Log("BossHealthBarPlayCheck PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield break;
    }
}
