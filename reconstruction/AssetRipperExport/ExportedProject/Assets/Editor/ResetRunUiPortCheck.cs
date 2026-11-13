using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ResetRunUiPortCheck
{
    public static void Run()
    {
        var root = new GameObject("reset-check");
        var save = root.AddComponent<SaveManager>();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        save.config = new ConfigSaveFile();
        save.config.cfGameSettings.quick_reset_time = 2f;
        var ui = root.AddComponent<ResetRunUi>();
        ui.content = new GameObject("content").transform;
        ui.progressBar = new GameObject("progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        ui.progressBar.type = Image.Type.Filled;
        ui.text = new GameObject("text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(ResetRunUi);
        type.GetMethod("StartProgress", flags).Invoke(ui, null);
        if (!ui.content.gameObject.activeSelf || !ui.text.gameObject.activeSelf) throw new Exception("reset indicator did not open");
        type.GetField("startedHoldingTime", flags).SetValue(ui, Time.time - 1f);
        type.GetMethod("UpdateBar", flags).Invoke(ui, null);
        if (Mathf.Abs(ui.progressBar.fillAmount - .5f) > .001f) throw new Exception("reset progress did not use configured hold time");
        type.GetMethod("StopProgress", flags).Invoke(ui, null);
        if (ui.content.gameObject.activeSelf || ui.text.gameObject.activeSelf || (bool)type.GetField("holding", flags).GetValue(ui)) throw new Exception("reset indicator did not close");
        type.GetMethod("StartProgress", flags).Invoke(ui, null);
        type.GetMethod("Update", flags).Invoke(ui, null);
        if ((bool)type.GetField("holding", flags).GetValue(ui)) throw new Exception("missing player did not cancel hold");
        Debug.Log("RESET_RUN_UI_PORT_CHECK_OK");
        UnityEngine.Object.DestroyImmediate(ui.content.gameObject);
        UnityEngine.Object.DestroyImmediate(ui.progressBar.gameObject);
        UnityEngine.Object.DestroyImmediate(ui.text.gameObject);
        UnityEngine.Object.DestroyImmediate(root);
    }
}
