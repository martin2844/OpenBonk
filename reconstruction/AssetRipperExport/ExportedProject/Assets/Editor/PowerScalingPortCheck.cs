using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;

public static class PowerScalingPortCheck
{
    public static void Run()
    {
        var method = typeof(StatScaling).GetMethod("PowerScaling", BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null) throw new Exception("PowerScaling method missing");
        float Call(float input, float inputMax, float outputMax, float exponent) => (float)method.Invoke(null, new object[] { input, inputMax, outputMax, exponent });
        void Near(float actual, float expected, string label) { if (Mathf.Abs(actual - expected) > .00001f) throw new Exception($"{label}: {actual} expected {expected}"); }
        Near(Call(-10, 100, 8, 2), 0, "negative input");
        Near(Call(0, 100, 8, 2), 0, "zero input");
        Near(Call(50, 100, 8, 2), 6, "midpoint with squared exponent");
        Near(Call(100, 100, 8, 2), 8, "maximum input");
        Near(Call(200, 100, 8, 2), 8, "input above maximum");
        Near(Call(25, 100, 8, 1), 2, "linear exponent");
        if (!float.IsNaN(Call(0, 0, 8, 2))) throw new Exception("zero input maximum should preserve original NaN");
        Debug.Log("PowerScalingPortCheck passed: input clamping, scaling, exponents and zero maximum");
    }
}
