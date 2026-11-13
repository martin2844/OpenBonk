using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class MapNavigationInspect
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var map = Object.FindAnyObjectByType<MapSelectionUi>(FindObjectsInactive.Include);
        Debug.Log($"MAP NAV start={map.startBtn?.name} confirm={map.btnConfirm?.name} mapRoot={map.gameObject.name}");
        foreach (var button in map.GetComponentsInChildren<Button>(true))
        {
            var nav = button.navigation;
            Debug.Log($"MAP NAV {button.name} active={button.gameObject.activeSelf} interactable={button.interactable} mode={nav.mode} up={nav.selectOnUp?.name} down={nav.selectOnDown?.name} left={nav.selectOnLeft?.name} right={nav.selectOnRight?.name}");
        }
    }
}
