using System;
using System.Reflection;
using UnityEngine;

public static class AndroidTriggerPortCheck
{
    public static void Run()
    {
        var type = typeof(AndroidNativeInput).GetNestedType("TriggerState", BindingFlags.NonPublic);
        var state = Activator.CreateInstance(type, true);
        var sample = type.GetMethod("Sample");
        bool Get(string name) => (bool)type.GetField(name).GetValue(state);
        void Step(bool digital, float analog, bool held, bool down, bool up)
        {
            sample.Invoke(state, new object[] { digital, analog });
            if (Get("held") != held || Get("down") != down || Get("up") != up) throw new Exception($"Trigger state failed at digital={digital} analog={analog}");
        }
        Step(false, 0, false, false, false);
        Step(false, .49f, false, false, false);
        Step(false, .5f, true, true, false);
        Step(true, .8f, true, false, false);
        Step(false, .45f, true, false, false);
        Step(false, .4f, false, false, true);
        Step(true, 0, true, true, false);
        Step(false, 0, false, false, true);
        Debug.Log("AndroidTriggerPortCheck passed: threshold, hysteresis, press, hold, release and overlapping digital/analog inputs");
    }
}
