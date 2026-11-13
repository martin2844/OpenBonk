using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

public static class PlayerSfxPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("PlayerSfxPortCheck: " + message); }
    static void Invoke(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    public static void Run()
    {
        var root = new GameObject("player sound check"); root.SetActive(false);
        AudioClip normal = null, phantom = null;
        try
        {
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var movement = root.AddComponent<PlayerMovement>(); var body = root.AddComponent<Rigidbody>();
            typeof(PlayerMovement).GetField("rb", Private).SetValue(movement, body); player.playerMovement = movement;
            var sounds = root.AddComponent<PlayerSfxs>(); sounds.windAudio = root.AddComponent<AudioSource>();
            sounds.sourceEvade = root.AddComponent<RandomSfx>(); sounds.sourceEvade.s = root.AddComponent<AudioSource>();
            normal = AudioClip.Create("normal evade", 100, 1, 44100, false); phantom = AudioClip.Create("phantom evade", 100, 1, 44100, false);
            sounds.evade = normal; sounds.evadePhantom = phantom;
            sounds.Evade(false); Check(sounds.sourceEvade.sounds[0] == normal, "normal evade clip");
            sounds.Evade(true); Check(sounds.sourceEvade.sounds[0] == phantom, "phantom evade clip");
            typeof(PlayerSfxs).GetField("avgSpeed", Private).SetValue(sounds, 27.5f);
            Invoke(sounds, "UpdateWind");
            Check(Mathf.Abs(sounds.windAudio.volume - 0.16f) < 0.001f, "wind volume at half speed");
            Check(Mathf.Abs(sounds.windAudio.pitch - 1.9f) < 0.001f, "wind pitch at half speed");
            var oldPause = MyTime.A_Pause; Invoke(sounds, "Start"); Check(MyTime.A_Pause != oldPause, "pause subscription");
            Invoke(sounds, "OnDestroy"); Check(MyTime.A_Pause == oldPause, "pause cleanup");
            Debug.Log("PlayerSfxPortCheck passed: evade clip choice, wind scaling, pause subscription cleanup");
        }
        finally { MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(root); if (normal != null) UnityEngine.Object.DestroyImmediate(normal); if (phantom != null) UnityEngine.Object.DestroyImmediate(phantom); }
    }
}
