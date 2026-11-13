#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class OpenBonkPikachuMovementCheck
{
    public static void Run()
    {
        var path = "Assets/OpenBonkModAuthoring/Pikachu/pikachu-study.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
        AnimationClip Clip(string name) => clips.Single(c => c.name == "Armature|OpenBonk " + name);
        var data = ScriptableObject.CreateInstance<OpenBonkExternalCharacterData>();
        data.idleAnimation = Clip("idle"); data.walkAnimation = Clip("walk study"); data.runAnimation = Clip("run"); data.jumpAnimation = Clip("jump"); data.surfAnimation = Clip("surf"); data.surfboard = "OpenBonkSurfboard";
        var go = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var animator = go.GetComponent<Animator>(); if (animator == null) animator = go.AddComponent<Animator>();
            animator.speed = 1f; Assets.Scripts.Utility.MyTime.paused = false;
            var driver = go.AddComponent<OpenBonkExternalAnimator>(); driver.Initialize(animator, data);
            var board = go.GetComponentsInChildren<Transform>(true).Single(t => t.name == data.surfboard).gameObject;
            var update = typeof(OpenBonkExternalAnimator).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var current = typeof(OpenBonkExternalAnimator).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic);
            void Check(bool ground, bool jumping, bool surfing, float speed, bool moving, AnimationClip expected, bool boardVisible, float input = 1f)
            {
                driver.SetMoving(moving); driver.SetMovementState(ground, jumping, surfing, speed, input); update.Invoke(driver, null);
                if ((AnimationClip)current.GetValue(driver) != expected || board.activeSelf != boardVisible) throw new Exception("movement state mismatch for " + expected.name + " current=" + ((AnimationClip)current.GetValue(driver)) + " board=" + board.activeSelf + " speed=" + animator.speed + " paused=" + Assets.Scripts.Utility.MyTime.paused);
            }
            // Bake the imported skin at distinct points on each action. This checks
            // the imported clip actually deforms geometry, not just its name/state.
            foreach (var clip in new[] { data.idleAnimation, data.walkAnimation, data.runAnimation, data.jumpAnimation, data.surfAnimation })
            {
                var skins = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                clip.SampleAnimation(go, 0f);
                var first = skins.Select(s => { var mesh = new Mesh(); s.BakeMesh(mesh); var v = mesh.vertices; UnityEngine.Object.DestroyImmediate(mesh); return v; }).ToArray();
                clip.SampleAnimation(go, clip.length * .5f);
                float max = 0f;
                for (int i = 0; i < skins.Length; i++)
                {
                    var mesh = new Mesh(); skins[i].BakeMesh(mesh); var v = mesh.vertices;
                    if (v.Length != first[i].Length) throw new Exception("vertex count changed");
                    for (int j = 0; j < v.Length; j++) max = Mathf.Max(max, Vector3.Distance(v[j], first[i][j]));
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                if (max < .01f) throw new Exception("clip has no meaningful deformation: " + clip.name + " max=" + max);
                Debug.Log("Pikachu deform PASS " + clip.name + " max=" + max);
            }
            // A sampled clip must give the same pose regardless of which action
            // preceded it; otherwise unkeyed bones can retain an old pose.
            var transforms = go.GetComponentsInChildren<Transform>(true).Where(t => t != board.transform).ToArray();
            var actions = new[] { data.idleAnimation, data.walkAnimation, data.runAnimation, data.jumpAnimation, data.surfAnimation };
            foreach (var target in actions)
            {
                actions[0].SampleAnimation(go, actions[0].length * .37f);
                target.SampleAnimation(go, target.length * .31f);
                var positions = transforms.Select(t => t.localPosition).ToArray();
                var rotations = transforms.Select(t => t.localRotation).ToArray();
                var scales = transforms.Select(t => t.localScale).ToArray();
                foreach (var previous in actions)
                {
                    previous.SampleAnimation(go, previous.length * .63f);
                    target.SampleAnimation(go, target.length * .31f);
                    for (int i = 0; i < transforms.Length; i++)
                        if (Vector3.Distance(positions[i], transforms[i].localPosition) > .0001f || Quaternion.Angle(rotations[i], transforms[i].localRotation) > .05f || Vector3.Distance(scales[i], transforms[i].localScale) > .0001f)
                            throw new Exception("pose depends on previous clip: " + previous.name + " -> " + target.name + " bone=" + transforms[i].name);
                }
                Debug.Log("Pikachu transition pose PASS " + target.name);
            }
            Check(true, false, false, 2, true, data.walkAnimation, false);
            Check(true, false, false, 10, true, data.runAnimation, false);
            Check(true, false, false, 10, true, data.walkAnimation, false, .16f);
            Check(true, false, false, 10, true, data.runAnimation, false, .8f);
            Check(false, true, false, 10, true, data.jumpAnimation, false);
            Check(false, false, true, 10, true, data.surfAnimation, true);
            Check(true, false, false, 0, false, data.idleAnimation, false);
            Assets.Scripts.Utility.MyTime.paused = true;
            driver.SetMoving(true); driver.SetMovementState(false, true, true, 10); update.Invoke(driver, null);
            if ((AnimationClip)current.GetValue(driver) != data.idleAnimation || board.activeSelf) throw new Exception("pause did not freeze visual state");
            Assets.Scripts.Utility.MyTime.paused = false;
            // API 3 characters made before optional movement clips still use walk.
            data.idleAnimation = data.runAnimation = data.jumpAnimation = data.surfAnimation = null;
            driver.Initialize(animator, data);
            Check(false, true, true, 10, true, data.walkAnimation, false);
            Debug.Log("Pikachu movement pause and legacy fallback PASS");
            Debug.Log("Pikachu movement states PASS: walk, run, jump, surf board and return to idle");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(data); }
    }
}
#endif
