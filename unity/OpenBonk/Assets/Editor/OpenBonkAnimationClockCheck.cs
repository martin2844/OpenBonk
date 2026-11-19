#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

// Run -executeMethod OpenBonkAnimationClockCheck.Run in the runtime project.
// Drives the actual component/game clock; no rendering or game session required.
public static class OpenBonkAnimationClockCheck
{
    private static readonly MethodInfo UpdateMethod = typeof(AnimatedMesh).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
    private static void Require(bool value, string description) { if (!value) throw new Exception(description); }

    public static void Run()
    {
        float savedTime = MyTime.time; bool savedPause = MyTime.paused;
        var savedRandom = UnityEngine.Random.state;
        var meshes = new List<Mesh>();
        var animation = ScriptableObject.CreateInstance<AnimatedMeshScriptableObject>();
        var root = new GameObject("Animation clock check");
        try
        {
            animation.AnimationFPS = 12;
            animation.Animations = new List<AnimatedMeshScriptableObject.Animation>();
            foreach (string clip in new[] { "walk", "idle", "charge" })
            {
                var frames = new List<Mesh>();
                for (int frame = 0; frame < 12; frame++)
                {
                    var mesh = new Mesh { name = clip + frame };
                    mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                    mesh.triangles = new[] { 0, 1, 2 };
                    frames.Add(mesh); meshes.Add(mesh);
                }
                animation.Animations.Add(new AnimatedMeshScriptableObject.Animation { Name = clip, Meshes = frames });
            }
            var component = root.AddComponent<AnimatedMesh>();
            var filter = root.GetComponent<MeshFilter>();
            MyTime.paused = false;
            void Reset(float time = 0) { MyTime.time = time; component.SetAnimation(animation); }
            void At(float time) { MyTime.time = time; UpdateMethod.Invoke(component, null); }
            void Frame(string clip, int index, string label) => Require(filter.sharedMesh.name == clip + index,
                label + ": expected " + clip + index + ", got " + filter.sharedMesh.name);

            foreach (int fps in new[] { 15, 30, 60 })
            {
                Reset();
                for (int frame = 0; frame <= fps * 5; frame++)
                {
                    At(frame / (float)fps);
                    Frame("walk", (frame * 12 / fps) % 12, "12 animation fps at " + fps + " rendered fps, sample " + frame);
                }
            }
            // A hitch spanning many cycles jumps to the correct frame directly.
            Reset(); At(103.7f); Frame("walk", 8, "large delta");
            // A backwards game-clock reset must not generate negative frames.
            At(2f); Frame("walk", 8, "clock rollback"); At(2.1f); Frame("walk", 9, "clock resumes after rollback");

            Reset(); At(.04f); component.Pause();
            At(9f); Frame("walk", 0, "local freeze");
            MyTime.time = 10f; component.UnPause(); At(10.05f);
            Frame("walk", 1, "resume retains fractional frame without frozen-time catch-up");
            MyTime.paused = true; At(20f); Frame("walk", 1, "global pause");
            MyTime.paused = false; At(20.05f); Frame("walk", 1, "global pause has no accumulated catch-up");
            At(20.10f); Frame("walk", 2, "global resume continues normally");

            Reset(); MyTime.time = .04f; component.SetPlaybackSpeed(2f); At(.08f);
            Frame("walk", 1, "rate change preserves old-rate elapsed time");
            At(.12f); Frame("walk", 2, "double playback rate");
            component.SelectClip("charge"); Frame("charge", 0, "attack begins at first pose");
            At(.22f); Frame("charge", 1, "attack does not inherit locomotion speed");

            Reset(); At(.52f); component.SelectClip("idle"); Frame("idle", 0, "native clips retain restart semantics");
            Reset(); component.ConfigureExternalPlayback(71);
            int phase = animation.Animations[0].Meshes.IndexOf(filter.sharedMesh);
            Require(phase >= 0, "shared frame mesh");
            component.SelectClip("idle"); Frame("idle", phase, "external walk/idle phase continuity");
            component.SelectClip("charge"); Frame("charge", 0, "external attack has authored start");
            component.SelectClip("walk"); Frame("walk", 0, "attack exit restarts locomotion");

            var phases = new HashSet<int>();
            for (uint id = 1; id <= 32; id++)
            {
                Reset(); component.ConfigureExternalPlayback(id);
                int selected = animation.Animations[0].Meshes.IndexOf(filter.sharedMesh);
                phases.Add(selected);
                Reset(); component.ConfigureExternalPlayback(id);
                Frame("walk", selected, "reproducible phase for id " + id);
            }
            Require(phases.Count >= 8, "spawned enemies must use diverse phases");
            UnityEngine.Random.InitState(5419);
            var before = UnityEngine.Random.state;
            component.ConfigureExternalPlayback(700);
            float next = UnityEngine.Random.value;
            UnityEngine.Random.state = before;
            Require(next == UnityEngine.Random.value, "animation must not consume gameplay RNG");

            component.SetPlaybackSpeed(1.8f); component.Pause();
            Reset(100);
            Require(!component.paused, "pool reset clears pause"); Frame("walk", 0, "pool reset clears external phase");
            At(100.1f); Frame("walk", 1, "pool reset clears speed and old time");
            component.SelectClip("idle"); Frame("idle", 0, "pool reset clears external clip policy");
            Debug.Log("OpenBonk animation clock PASS: 12fps at 15/30/60Hz, hitches, rollback, freezes, speed changes, clip transitions, deterministic phases, RNG independence and pool reset");
        }
        finally
        {
            MyTime.time = savedTime; MyTime.paused = savedPause; UnityEngine.Random.state = savedRandom;
            Object.DestroyImmediate(root); Object.DestroyImmediate(animation);
            foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
        }
    }
}
#endif
