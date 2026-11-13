using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public static class ToolTipPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    public static void Run()
    {
        var root = new GameObject("ToolTipCheck", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        var tip = root.AddComponent<ToolTip>();
        tip.parentCanvas = canvas;
        tip.group = new GameObject("Group", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        tip.group.transform.SetParent(root.transform);
        tip.backDrop = tip.group.GetComponent<RectTransform>();
        tip.t_tip = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        tip.t_tip.transform.SetParent(tip.group.transform);
        var trigger = new GameObject("Trigger").AddComponent<ToolTipObject>();
        trigger.transform.SetParent(root.transform);
        try
        {
            Invoke(tip, "Awake");
            trigger.text = " ";
            trigger.OnPointerEnter(null);
            Require(!Field<bool>(trigger, "hovering"), "blank tooltip started hovering");
            trigger.text = "Inventory item";
            trigger.OnPointerEnter(null);
            Require(Field<bool>(trigger, "hovering"), "pointer did not start hover");
            Set(trigger, "startedHoveringTime", Time.time - 1f);
            Invoke(trigger, "Update");
            Require(Field<bool>(trigger, "showed") && Field<bool>(tip, "visible") && tip.t_tip.text == trigger.text, "hover did not show tooltip");
            trigger.OnPointerExit(null);
            Require(!Field<bool>(trigger, "hovering") && !Field<bool>(tip, "visible"), "pointer exit did not hide tooltip");
            tip.SetTip("Transition test");
            TransitionUI.A_transitionStart?.Invoke();
            Require(!Field<bool>(tip, "visible"), "transition did not hide tooltip");
            tip.SetTip("Anchored test", tip.backDrop);
            Require(Field<bool>(tip, "visible") && !Field<bool>(tip, "useMouse"), "anchored tooltip state");
            Debug.Log("ToolTipPortCheck passed: hover, exit, transition, and anchored display states");
        }
        finally
        {
            Invoke(tip, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
