using System;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MenuAlertsPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
            var alerts = UnityEngine.Object.FindAnyObjectByType<MenuAlerts>(FindObjectsInactive.Include);
            Check(alerts != null && alerts.alertUnlocks != null && alerts.alertQuests != null && alerts.alertShop != null, "original menu alert bindings");
            var obj = new GameObject("test save manager"); obj.SetActive(false);
            var save = obj.AddComponent<SaveManager>();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            save.progression = new ProgressionSaveFile();
            var p = save.progression;
            var refresh = typeof(MenuAlerts).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
            var awake = typeof(MenuAlerts).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            var destroy = typeof(MenuAlerts).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(alerts, null);
            SaveManager.A_SavesLoaded?.Invoke();
            Check(alerts.alertUnlocks.activeSelf && alerts.alertQuests.activeSelf && alerts.alertShop.activeSelf, "unvisited alerts");
            p.menuMeta.hasVisitedUnlocks = p.menuMeta.hasVisitedQuests = p.menuMeta.hasVisitedShop = true;
            refresh.Invoke(alerts, null);
            Check(!alerts.alertUnlocks.activeSelf && !alerts.alertQuests.activeSelf && !alerts.alertShop.activeSelf, "visited and no new entries");
            p.newUnlockables.Add("test"); p.newShopItems.Add("test"); p.achievements.Add("test");
            SaveManager.A_SavesLoaded?.Invoke();
            Check(alerts.alertUnlocks.activeSelf && alerts.alertQuests.activeSelf && alerts.alertShop.activeSelf, "new content and unclaimed quest");
            p.newUnlockables.Clear(); p.newShopItems.Clear(); p.claimedAchievements.Add("test");
            SaveManager.A_SavesLoaded?.Invoke();
            Check(!alerts.alertUnlocks.activeSelf && !alerts.alertQuests.activeSelf && !alerts.alertShop.activeSelf, "clearing and claiming");
            destroy.Invoke(alerts, null);
            p.newUnlockables.Add("test"); SaveManager.A_SavesLoaded?.Invoke();
            Check(!alerts.alertUnlocks.activeSelf, "unsubscription");
            Debug.Log("MenuAlertsPortCheck passed: original scene bindings, visits, new content, quest claims, save-load and unsubscription");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
