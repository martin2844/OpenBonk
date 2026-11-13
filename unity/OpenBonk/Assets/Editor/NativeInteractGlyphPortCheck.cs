using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NativeInteractGlyphPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("NativeInteractGlyphPortCheck: " + message); }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GeneratedMap.unity");
        var detector = Resources.FindObjectsOfTypeAll<DetectInteractables>().FirstOrDefault(d => d.gameObject.scene.IsValid() && d.glyphContainer != null);
        Require(detector != null, "scene has wired interaction glyph");
        var glyph = detector.glyphContainer;
        Require(glyph.i_glyph != null && glyph.t_text != null, "glyph image and text are wired");
        var billboard = detector.uiParent.GetComponent<LookAtCamera>(); Require(billboard != null && billboard.invert, "original prompt has inverted camera-facing component");
        var cameraObject = new GameObject("test camera"); cameraObject.SetActive(false); PlayerCamera.Instance = cameraObject.AddComponent<PlayerCamera>();
        cameraObject.transform.position = new Vector3(10f, 8f, -4f); detector.uiParent.position = Vector3.zero;
        var updateFacing = typeof(LookAtCamera).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        updateFacing.Invoke(billboard, null); updateFacing.Invoke(billboard, null);
        Require(Vector3.Dot(detector.uiParent.forward, -cameraObject.transform.position.normalized) > .999f, "inverted prompt faces camera");
        billboard.invert = false; updateFacing.Invoke(billboard, null);
        Require(Vector3.Dot(detector.uiParent.forward, cameraObject.transform.position.normalized) > .999f, "non-inverted orientation");
        billboard.xzOnly = true; billboard.invert = true; updateFacing.Invoke(billboard, null);
        Require(Vector3.Dot(detector.uiParent.forward, new Vector3(10f, 0f, -4f).normalized) > .999f, "horizontal-only orientation follows original branch");
        PlayerCamera.Instance = null; UnityEngine.Object.DestroyImmediate(cameraObject);
        var show = typeof(MyGlyphDisplay).GetMethod("ShowNativeAction", BindingFlags.NonPublic | BindingFlags.Instance);
        show.Invoke(glyph, new object[] { "Interact" });
        Require(!glyph.i_glyph.enabled && !glyph.i_glyph.gameObject.activeSelf && glyph.t_text.gameObject.activeSelf && glyph.t_text.enabled && glyph.t_text.text == "X / E", "interaction label uses mapped gamepad and keyboard bindings");
        Require(((RectTransform)glyph.transform).rect.width >= glyph.t_text.GetPreferredValues("X / E").x, "glyph container reserves label width");
        show.Invoke(glyph, new object[] { "unknown" });
        Require(!glyph.i_glyph.enabled && !glyph.t_text.gameObject.activeSelf && !glyph.t_text.enabled && glyph.t_text.text == "", "unknown action does not show wrong binding");
        if (glyph.hoverOverlay != null) { glyph.Hover(true); Require(glyph.hoverOverlay.activeSelf, "hover enabled"); glyph.Hover(false); Require(!glyph.hoverOverlay.activeSelf, "hover disabled"); }
        Debug.Log("NativeInteractGlyphPortCheck passed: original scene interaction glyph wiring, label, unknown action and hover");
    }
}
