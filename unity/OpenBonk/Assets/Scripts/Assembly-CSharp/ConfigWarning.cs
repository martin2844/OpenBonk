using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.UI;
using Assets.Scripts.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigWarning : MonoBehaviour
{
    public GameObject overlay;
    public TextMeshProUGUI t_warning;
    public TextSizer textSizer;
    private string configFilePath;
    public static ConfigWarning Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
#if UNITY_ANDROID
        // The exported close button points at a generic Window.Close. Loading
        // failed, so closing it must first offer the explicit no-save route.
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (button.name == "B_Back")
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(IgnoreWarning);
            }
#endif
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void OpenFile() { } // The shipped method is empty.

    public void RefreshFile() { HideWarning(); SaveManager.Instance?.Load(false); }
    public void LoadBackup() { HideWarning(); SaveManager.Instance?.Load(true); }

    public void ResetFile()
    {
        var windows = AlwaysUi.Instance != null ? AlwaysUi.Instance.dynamicWindows : null;
        if (windows == null) return;
        string content = Localized("CONFIG_ERROR_RESET_WARNING", new Dictionary<string, string> { ["file"] = configFilePath ?? "" }, "Reset this file? This removes the current file.");
        windows.NewWindowPrompt(Localized("WARNING", null, "Warning"), content, () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(configFilePath) && File.Exists(configFilePath)) File.Delete(configFilePath);
                HideWarning(); SaveManager.Instance?.Load(false);
            }
            catch (Exception e) { ShowWarning(e.Message, configFilePath); }
        });
    }

    public void IgnoreWarning()
    {
        var windows = AlwaysUi.Instance != null ? AlwaysUi.Instance.dynamicWindows : null;
        if (windows == null) return;
        windows.NewWindowPrompt(Localized("WARNING", null, "Warning"), Localized("CONFIG_ERROR_IGNORE_WARNING", null, "Continue without saving?"), () =>
        {
            HideWarning(); SaveManager.Instance?.LoadNoSave();
        });
    }

    public void ShowWarning(string e, string filePath) { StartCoroutine(ShowWarningCoroutine(e, filePath)); }
    private IEnumerator ShowWarningCoroutine(string e, string filepath)
    {
        yield return null;
        configFilePath = filepath;
        if (overlay != null) overlay.SetActive(true);
        string details = "\n\n<color=red><size=85%>" + (e ?? "") + "\n" + (filepath ?? "") + "</size></color>";
        if (t_warning != null) t_warning.text = Localized("CONFIG_ERROR_MSG", new Dictionary<string, string> { ["error"] = details }, "Could not load saved data." + details);
        if (textSizer != null) { textSizer.Refresh(); textSizer.Recalculate(); }
        UiUtility.RebuildUi(transform);
    }

    private static string Localized(string key, Dictionary<string, string> args, string fallback)
    {
        try
        {
            string result = args == null ? LocalizationUtility.GetLocalizedString("MainMenuOther", key) : LocalizationUtility.GetLocalizedString("MainMenuOther", key, args);
            return string.IsNullOrEmpty(result) || result.StartsWith("Missing localization") ? fallback : result;
        }
        catch (Exception) { return fallback; }
    }

    public void HideWarning() { if (overlay != null) overlay.SetActive(false); }
}
