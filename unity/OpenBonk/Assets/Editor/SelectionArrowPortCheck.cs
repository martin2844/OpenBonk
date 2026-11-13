using System;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;

public static class SelectionArrowPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SelectionArrowPortCheck: " + message); }
    static void Invoke(SelectionArrow arrow, string name) { typeof(SelectionArrow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(arrow, null); }
    public static void Run()
    {
        var root = new GameObject("arrow test", typeof(RectTransform));
        root.SetActive(false);
        var arrowObject = new GameObject("arrow", typeof(RectTransform), typeof(Image), typeof(SelectionArrow)); arrowObject.transform.SetParent(root.transform, false);
        var maskObject = new GameObject("mask", typeof(RectTransform)); maskObject.transform.SetParent(root.transform, false);
        var scrollObject = new GameObject("scroll", typeof(RectTransform), typeof(ScrollRect)); scrollObject.transform.SetParent(root.transform, false);
        var viewport = new GameObject("viewport", typeof(RectTransform)); viewport.transform.SetParent(scrollObject.transform, false);
        var buttonObject = new GameObject("button", typeof(RectTransform), typeof(MyButtonNormal)); buttonObject.transform.SetParent(viewport.transform, false);
        var arrow = arrowObject.GetComponent<SelectionArrow>(); arrow.rectTransform = arrowObject.GetComponent<RectTransform>(); arrow.renderer = arrowObject.GetComponent<Image>(); arrow.mask = maskObject.GetComponent<RectTransform>();
        var selected = buttonObject.GetComponent<RectTransform>(); selected.sizeDelta = new Vector2(120, 40); selected.localScale = new Vector3(2, 1, 1); selected.anchoredPosition = new Vector2(35, 22);
        var viewportRect = viewport.GetComponent<RectTransform>(); viewportRect.sizeDelta = new Vector2(300, 180); scrollObject.GetComponent<ScrollRect>().viewport = viewportRect;
        try
        {
            root.SetActive(true);
            Invoke(arrow, "Awake");
            ButtonManager.A_ButtonHover?.Invoke(buttonObject.GetComponent<MyButtonNormal>());
            Invoke(arrow, "Update");
            Check(arrow.renderer.enabled, "selection becomes visible");
            Check(Vector3.Distance(arrow.rectTransform.position, selected.TransformPoint(selected.rect.center)) < .001f, "selection follows button center");
            Check(Vector2.Distance(arrow.mask.sizeDelta, viewportRect.rect.size) < .001f, "mask matches scroll viewport");
            Check(arrow.rectTransform.sizeDelta.x > 260 && arrow.rectTransform.sizeDelta.y > 60, "selection includes scale and offset");
            selected.anchoredPosition += new Vector2(30, 10); Invoke(arrow, "Update");
            Check(Vector3.Distance(arrow.rectTransform.position, selected.TransformPoint(selected.rect.center)) < .001f, "selection follows moved button");
            arrow.Hide(); Check(!arrow.renderer.enabled, "hide clears selection");
            Invoke(arrow, "OnDestroy"); ButtonManager.A_ButtonHover?.Invoke(buttonObject.GetComponent<MyButtonNormal>()); Invoke(arrow, "Update");
            Check(!arrow.renderer.enabled && SelectionArrow.Instance == null, "destroy unsubscribes");
            Debug.Log("SelectionArrowPortCheck passed: hover, geometry, viewport, movement, hide and cleanup");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
