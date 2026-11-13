using System;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class WeaponSharedEffectsPortCheck
{
    static void Require(bool v, string message) { if (!v) throw new Exception("WeaponSharedEffectsPortCheck: " + message); }
    static void Invoke(object instance, string method, params object[] args) => instance.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, args);
    public static void Run()
    {
        var root = new GameObject("shared weapon effects test"); root.SetActive(false);
        var timerObject = new GameObject("timer"); timerObject.SetActive(false);
        float previousTime = MyTime.time;
        var previousPause = MyTime.A_Pause;
        AudioClip clip = null;
        try
        {
            var source = root.AddComponent<AudioSource>();
            var random = root.AddComponent<RandomSfx>(); random.s = source;
            random.minPitch = random.maxPitch = 1; random.randomVolume = 0;
            clip = AudioClip.Create("test", 1024, 1, 44100, false); random.sounds = new[] { clip };
            var player = root.AddComponent<PlaySfxOnEnable>(); Invoke(player, "OnValidate");
            Require(player.randomSfx == random && player.audioSource == source, "component lookup");
            Invoke(player, "OnEnable"); Require(source.clip == clip, "random sound route selects clip");
            var pauser = root.AddComponent<AudioPauser>(); Invoke(pauser, "Awake");
            Require(pauser.audioSource == source && MyTime.A_Pause != previousPause, "audio lookup and subscription");
            MyTime.A_Pause?.Invoke(true); MyTime.A_Pause?.Invoke(false);
            Invoke(pauser, "OnDestroy"); Require(MyTime.A_Pause == previousPause, "pause event cleanup");
            var timer = timerObject.AddComponent<DisableAfterTime>(); timer.disableDelay = 2f;
            MyTime.time = 10f; timerObject.SetActive(true); Invoke(timer, "OnEnable");
            MyTime.time = 12f; Invoke(timer, "Update"); Require(timerObject.activeSelf, "strict time boundary");
            MyTime.time = 12.01f; Invoke(timer, "Update"); Require(!timerObject.activeSelf, "object disabled after delay");
            MyTime.time = 30f; timerObject.SetActive(true); Invoke(timer, "OnEnable");
            MyTime.time = 31.99f; Invoke(timer, "Update"); Require(timerObject.activeSelf, "reactivation resets timer");
            MyTime.time = 32.01f; Invoke(timer, "Update"); Require(!timerObject.activeSelf, "reactivated object disabled");
            Debug.Log("WeaponSharedEffectsPortCheck passed: sound route, pause subscription and reuse timeout");
        }
        finally
        {
            MyTime.A_Pause = previousPause; MyTime.time = previousTime;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(timerObject);
            if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
