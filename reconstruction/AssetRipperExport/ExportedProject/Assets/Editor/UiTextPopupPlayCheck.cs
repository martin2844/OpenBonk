using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class UiTextPopupPlayCheck
{
    const string Pending = "UiTextPopupPlayCheck.pending";
    static UiTextPopupPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("popup check").AddComponent<UiTextPopupPlayRunner>().StartCheck(); } }
}
public class UiTextPopupPlayRunner : MonoBehaviour
{
    public void StartCheck() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        var root = new GameObject("popup root");
        var popup = root.AddComponent<UiTextPopup>();
        popup.parent = new GameObject("content", typeof(RectTransform)); popup.parent.transform.SetParent(root.transform); popup.parent.SetActive(false);
        popup.canvasGroup = popup.parent.AddComponent<CanvasGroup>();
        popup.t_text = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); popup.t_text.transform.SetParent(popup.parent.transform);
        popup.sfx = root.AddComponent<RandomSfx>();
        popup.SetText("Need keys", new Vector3(100, 150, 0), Color.red, 0.75f);
        try { Require(popup.parent.activeSelf && popup.t_text.text == "Need keys" && popup.t_text.color == Color.red, "text appears"); Require(popup.parent.transform.position == new Vector3(100,150,0) && popup.parent.transform.localScale == Vector3.zero, "position and initial scale"); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(0.5f);
        try { Require(popup.parent.activeSelf && popup.canvasGroup.alpha > .9f && popup.parent.transform.localScale.x > .7f && popup.parent.transform.position.y > 150, "fade in, growth and rise"); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(1.2f);
        try { Require(!popup.parent.activeSelf, "automatic hide"); Debug.Log("UiTextPopupPlayCheck passed"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("UiTextPopupPlayCheck failed: " + message); }
}
