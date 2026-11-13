using System;
using System.Reflection;
using MilkShake;
using UnityEditor;
using UnityEngine;

public static class ShakeParametersPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ShakeParametersPortCheck: " + message); }
    public static void Run()
    {
        var preset = AssetDatabase.LoadAssetAtPath<ShakePreset>("Assets/MonoBehaviour/DamageShake.asset");
        Check(preset != null, "damage preset exists");
        Check(preset.ShakeType == ShakeType.OneShot && preset.Strength == .75f && preset.Roughness == 14f && preset.FadeIn == .05f && preset.FadeOut == .25f, "serialized damage settings");
        Check(preset.PositionInfluence == Vector3.one && preset.RotationInfluence == Vector3.zero, "serialized influence");
        var copy = new ShakeParameters(preset);
        Check(copy.Strength == .75f && copy.Roughness == 14f && copy.FadeIn == .05f && copy.FadeOut == .25f && copy.PositionInfluence == Vector3.one, "preset copy");
        copy.Strength = 3f; copy.ShakeType = ShakeType.Sustained; copy.PositionInfluence = Vector3.right;
        Check(preset.Strength == .75f && preset.ShakeType == ShakeType.OneShot && preset.PositionInfluence == Vector3.one, "copy is independent");
        var sum = new ShakeResult { PositionShake = Vector3.right, RotationShake = Vector3.up } + new ShakeResult { PositionShake = Vector3.up, RotationShake = Vector3.forward };
        Check(sum.PositionShake == Vector3.right + Vector3.up && sum.RotationShake == Vector3.up + Vector3.forward, "shake result addition");
        var instance = new ShakeInstance(preset, 42);
        var frame = typeof(ShakeInstance).GetField("lastUpdatedFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        frame.SetValue(instance, -1); instance.UpdateShake(.025f);
        Check(instance.State == ShakeState.FadingIn && Mathf.Abs(instance.CurrentStrength - .375f) < .001f, "halfway through fade-in");
        frame.SetValue(instance, -1); instance.UpdateShake(.025f);
        Check(instance.State == ShakeState.FadingOut && !instance.IsFinished, "one-shot starts fading out");
        frame.SetValue(instance, -1); instance.UpdateShake(.25f);
        Check(instance.IsFinished && instance.CurrentStrength == 0f, "one-shot stops and is removable");
        instance.Start(.1f); instance.Pause(0f); Check(instance.IsPaused, "pause"); instance.TogglePaused(0f); Check(!instance.IsPaused, "resume");
        Debug.Log("ShakeParametersPortCheck passed: shipped preset, copying, result addition and one-shot timing");
    }
}
