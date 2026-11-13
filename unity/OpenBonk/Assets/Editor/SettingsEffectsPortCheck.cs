using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEngine;

public static class SettingsEffectsPortCheck
{
    static void Require(bool ok, string why) { if (!ok) throw new Exception("SettingsEffectsPortCheck: " + why); }
    public static void Run()
    {
        int oldFps = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount, oldQueued = QualitySettings.maxQueuedFrames, oldMipmap = QualitySettings.globalTextureMipmapLimit, oldAa = QualitySettings.antiAliasing;
        bool oldSoft = QualitySettings.softParticles;
        var obj = new GameObject("settings effects check"); var current = obj.AddComponent<CurrentSettings>(); var video = new CFVideoSettings(); var game = new CFGameSettings();
        var db = typeof(CurrentSettings).GetMethod("SliderToDb", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            current.BetterUpdateCfSettings("fps_limit", 75f, video); Require(video.fps_limit == 75f && Application.targetFrameRate == 75, "target frame rate applied");
            current.BetterUpdateCfSettings("vsync", 2, video); Require(QualitySettings.vSyncCount == 2, "vertical sync applied");
            current.BetterUpdateCfSettings("input_delay", 9f, game); Require(QualitySettings.maxQueuedFrames == 6, "queued frame limit clamped");
            current.BetterUpdateCfSettings("soft_particles", 0, video); Require(!QualitySettings.softParticles, "soft particles switched off");
            current.BetterUpdateCfSettings("anti_aliasing", 2, video); Require(QualitySettings.antiAliasing == 4, "antialiasing level converted to sample count");
            Require(Mathf.Abs((float)db.Invoke(current, new object[] { 1f })) < .001f, "full volume is zero decibels");
            Require(Mathf.Abs((float)db.Invoke(current, new object[] { .5f }) + 6.0206f) < .001f, "half volume logarithmic conversion");
            Require((float)db.Invoke(current, new object[] { 0f }) == -80f, "zero volume floor");
            Debug.Log("SettingsEffectsPortCheck passed: frame rate, v-sync, latency, graphics and audio conversion");
        }
        finally { Application.targetFrameRate = oldFps; QualitySettings.vSyncCount = oldVsync; QualitySettings.maxQueuedFrames = oldQueued; QualitySettings.globalTextureMipmapLimit = oldMipmap; QualitySettings.antiAliasing = oldAa; QualitySettings.softParticles = oldSoft; UnityEngine.Object.DestroyImmediate(obj); }
    }
}
