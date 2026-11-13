#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Opt-in diagnostic; uses the scene's original settings-row prefab.
public sealed class AndroidRebindProbe : MonoBehaviour
{
    public static void Begin() { new GameObject("Android rebind probe").AddComponent<AndroidRebindProbe>(); }
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(5f);
        AndroidControllerBindings.Reset();
        var settings = FindObjectOfType<Settings>(true);
        if (settings == null || settings.controlPrefab == null || SaveManager.Instance == null) { Debug.Log("Rebind probe: missing settings prefab or save"); yield break; }
        var canvasObject = new GameObject("Rebind probe canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 32000;
        var row = Instantiate(settings.controlPrefab, canvasObject.transform);
        var setting = row.GetComponent<InputSetting>();
        var rect = row.transform as RectTransform; rect.anchoredPosition = Vector2.zero;
        var config = SaveManager.Instance.config.cfControlSettings;
        setting.SetSetting((string name, object value, CFSettings cf) => { }, "controller_jump", config.controller_jump, settings, config);
        var button = row.GetComponent<Button>();
        if (button == null) { Debug.Log("Rebind probe: row has no button"); yield break; }
        bool beforeNav = EventSystem.current != null && EventSystem.current.sendNavigationEvents;
        button.onClick.Invoke();
        Debug.Log($"Rebind probe: ready listening={AndroidControllerBindings.IsListening}, label={setting.keyDisplays[0].text.text}, navBefore={beforeNav}, navDuring={(EventSystem.current != null && EventSystem.current.sendNavigationEvents)}");
        float deadline = Time.realtimeSinceStartup + 16f;
        while (AndroidControllerBindings.IsListening && Time.realtimeSinceStartup < deadline) yield return null;
        Debug.Log($"Rebind probe: bound jump={AndroidControllerBindings.DisplayForAction("Jump")}, interact={AndroidControllerBindings.DisplayForAction("Interact")}, aim={AndroidControllerBindings.DisplayForAction("Aim")}, label={setting.keyDisplays[0].text.text}, listening={AndroidControllerBindings.IsListening}, navAfter={(EventSystem.current != null && EventSystem.current.sendNavigationEvents)}");
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.JoystickButton0))
                Debug.Log($"Rebind probe: event x={Input.GetKeyDown(KeyCode.JoystickButton2)}, y={Input.GetKeyDown(KeyCode.JoystickButton3)}, a={Input.GetKeyDown(KeyCode.JoystickButton0)}, jump={MyInputManager.GetButtonDown(MyInputManager.Jump)}, interact={MyInputManager.GetButtonDown(MyInputManager.Interact)}, aim={MyInputManager.GetButtonDown(MyInputManager.Aim)}, uiSubmit={MyInputManager.GetButtonDown(MyInputManager.UISubmit)}");
            yield return null;
        }
        Destroy(canvasObject); Destroy(gameObject);
    }
}
#endif
