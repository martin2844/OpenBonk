using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public static class LogsUiPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("LogsUiPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("log test", typeof(RectTransform));
        try
        {
            var ui = root.AddComponent<LogsUi>();
            var content = new GameObject("content", typeof(RectTransform)); content.transform.SetParent(root.transform);
            ui.content = (RectTransform)content.transform; ui.content.sizeDelta = new Vector2(400, 150);
            var template = new GameObject("entry", typeof(RectTransform), typeof(MyButtonLog)); template.transform.SetParent(content.transform);
            ui.entryPrefab = (RectTransform)template.transform; ui.entryPrefab.sizeDelta = new Vector2(300, 50);
            ui.t_pages = new GameObject("pages", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            ui.t_pages.transform.SetParent(root.transform);
            ui.t_title = new GameObject("title", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            ui.t_title.transform.SetParent(root.transform);
            var refresh = typeof(LogsUi).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
            refresh.Invoke(ui, null);
            int count = Enum.GetValues(typeof(Actors.Enemies.EEnemy)).Length;
            int pages = Mathf.CeilToInt(count / 3f);
            Require(content.transform.childCount == 3, "entry count");
            Require(ui.t_pages.text == "1/" + pages, "first page");
            ui.FlipPage(1);
            Require(ui.t_pages.text == "2/" + pages, "second page");
            ui.FlipPage(-2);
            Require(ui.t_pages.text == pages + "/" + pages, "wrap backwards");
            int remainder = count - (pages - 1) * 3;
            for (int i = 0; i < 3; i++) Require(content.transform.GetChild(i).gameObject.activeSelf == (i < remainder), "last page visibility");
            ui.FlipPage(1);
            Require(ui.t_pages.text == "1/" + pages, "wrap forwards");
            refresh.Invoke(ui, null);
            Require(content.transform.childCount == 3, "refresh duplicated entries");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Additive);
        try
        {
            LogsUi ui = null;
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                var candidate = rootObject.GetComponentInChildren<LogsUi>(true);
                if (candidate != null) { ui = candidate; break; }
            }
            Require(ui != null && ui.content != null && ui.entryPrefab != null && ui.t_pages != null && ui.t_title != null && ui.entryNavigation != null && ui.logDisplay != null, "Android scene bindings");
            for (var parent = ui.transform; parent != null; parent = parent.parent) Debug.Log("LogsUi hierarchy: " + parent.name + " active=" + parent.gameObject.activeSelf);
            Require(ui.entryPrefab.GetComponent<MyButtonLog>() != null, "Android entry component");
            ui.gameObject.SetActive(true);
            typeof(LogsUi).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            var entries = (System.Collections.Generic.List<MyButtonLog>)typeof(LogsUi).GetField("logEntries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            int pageCount = (int)typeof(LogsUi).GetField("maxPages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            Require(entries.Count > 0 && pageCount > 1, "Android scene page initialization");
            Require(ui.t_pages.text == "1/" + pageCount, "Android scene first page");
            ui.FlipPage(1);
            Require(ui.t_pages.text == "2/" + pageCount, "Android scene next page");
            Require(entries[0].gameObject.activeSelf, "Android scene first entry inactive");
            Debug.Log("LogsUiPortCheck passed: pagination, wrapping, refresh and actual Android scene with " + entries.Count + " entries and " + pageCount + " pages");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
