using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SettingButtonProbe : BetterSetting
{
    public int calls;
    public int direction;
    public float lastMultiplier;
    public override void ControllerInputDir(int dir, float multiplier)
    {
        calls++;
        direction = dir;
        lastMultiplier = multiplier;
    }
    protected override void ShowValue() { }
}

public static class SettingButtonPortCheck
{
    public static void Run()
    {
        var go = new GameObject("setting control check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        try
        {
            var probe = go.AddComponent<SettingButtonProbe>();
            var row = go.AddComponent<MyButtonSetting>();
            row.betterSetting = probe;
            row.background = go.GetComponent<Image>();
            row.defaultColor = Color.white;
            row.hoverColor = Color.red;
            int entered = 0, exited = 0;
            row.A_StartHover += () => entered++;
            row.A_StopHover += () => exited++;
            row.StartHover();
            if (row.background.color != Color.red || entered != 1) throw new Exception("Selection did not update the row");
            var step = typeof(MyButtonSetting).GetMethod("ProcessHorizontal", BindingFlags.Instance | BindingFlags.NonPublic);
            Action<float, float> send = (axis, time) => step.Invoke(row, new object[] { axis, time });
            send(1f, 10f);
            if (probe.calls != 1 || probe.direction != 1 || Math.Abs(probe.lastMultiplier - 1.015f) > .0001f) throw new Exception("First right input failed");
            send(1f, 10.2f);
            if (probe.calls != 1) throw new Exception("Repeated too early");
            send(1f, 10.31f);
            if (probe.calls != 2 || probe.lastMultiplier <= 1.015f) throw new Exception("Hold repeat failed");
            send(-1f, 10.32f);
            if (probe.calls != 3 || probe.direction != -1 || Math.Abs(probe.lastMultiplier - 1.015f) > .0001f) throw new Exception("Direction reversal failed");
            send(0f, 10.33f);
            send(-.5f, 10.34f);
            if (probe.calls != 3) throw new Exception("Dead zone failed");
            row.StopHover();
            if (row.background.color != Color.white || exited != 1) throw new Exception("Deselection did not restore the row");
            Debug.Log("SettingButtonPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
