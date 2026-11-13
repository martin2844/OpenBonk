#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Local diagnostic only. The test bundles contain game assets.
public static class OpenBonkClipPoseStudy
{
    // Entries are pinned by their exported asset GUID; controllers and prefabs
    // were checked before selecting these candidates for a private study.
    private static readonly (string id, string clip, string prefab)[] CandidatesBatch1 = {
        ("2a23f0ba0bef86640bd27a258461b2db", "NoelleFalling", "Noelle"),
        ("3bd4e7b04ee27cb428568b583ba3405e", "A_Cruise", "Calcium"),
        ("588f405b6967afd4cafaef06092c5f46", "Grind_6", "Monke"),
        ("5e2684d41f3d33e4292eb0216efe5991", "Falling_1", "Bandit"),
        ("5e73ce7d8f31d4c4ca0a3ebc69422d2b", "Grind", "Astronaut"),
        ("7875e1d678005a449a1f3ed2c899b60d", "A_Falling", "Calcium"),
        ("8881832692b485a4eb8f604f70a5aa10", "FoxGrind2", "FoxWizard"),
        ("88ff40bccda905d45bfe7c25d0fc1454", "BirdoGrind", "Birdo"),
        ("8b0490bab8a3b2f4fa428fb0a594658c", "Grind_4", "Amog"),
        ("8b2d0c1648043194295bca80cc4a3e2c", "Grind_3", "Dicehead"),
        ("de8d2fe18363ec646b43a292397f5dc6", "Grind_1", "Bandit"),
        ("f6a87e19eaddaf64198f909f9fac9bac", "NoelleGrind", "Noelle")
    };

    private static readonly (string id, string clip, string prefab)[] CandidatesBatch2 = {
        ("092fce90cc1a8a94587af717364ee92e", "Slide", "Roberto"),
        ("1b66b1aa400e1d14b82588aa3351f9ae", "Idle_2", "BloodMage"),
        ("3e4f673b4a424ce42a29f114029b48b6", "KnightRun", "CorruptedKnight"),
        ("421a0e06e58d95d4b942da60e7151467", "A_Jump", "Calcium"),
        ("423504acca51724429a1637c8fca9674", "CowboyFalling", "RobotCowboy"),
        ("48dde080a5bdefa448cf24b26abd3fcc", "Falling_4", "Roberto"),
        ("495ba194f92dd8649a37bf2f94f64288", "KnightGrind", "CorruptedKnight"),
        ("4a69e487ae9b55a4fbba40dae3842c4c", "Run_4", "Ninja"),
        ("509a55ef1be9494498d8c7a97c349eba", "Grind_7", "Ninja"),
        ("59a9b9ce091e1034c999e58776417148", "KnightRun_0", "Knight"),
        ("6df67103f0be2454ca68ac855fa52fe5", "A_Grinding", "Calcium"),
        ("76ea6d58bdc5d0a43945cbcb04ae5c33", "Falling", "Astronaut"),
        ("82d77ca90025324439e1590f2b05fd7a", "Falling_2", "BloodMage"),
        ("8c43189040da5e9408e0495333bb75e3", "Idle_5", "Monke"),
        ("8c58e85098a54154b9ebf994cb4e01b2", "Falling_10", "Ninja"),
        ("9d88648fb1363874f91c62f0c451f0cc", "Run_1", "BloodMage"),
        ("a7e032c1ac4f20a4a8bf0c3342dba488", "BirdoFall", "Birdo"),
        ("b15374f5333584845aad25321e9dcecb", "KnightJump", "CorruptedKnight"),
        ("b19486fb906282947a9812fdd7b0e2c6", "Grind_2", "BloodMage"),
        ("b54b8de3d8f02ef4d9cd36711bbfb1e3", "Idle_4", "B_Megachad"),
        ("b69916918bd16684fa1f61dda6426e88", "Orc_Grind", "Ogre"),
        ("bf8f1a0e6de1e6f419d0a2011814ef79", "KnightFalling", "CorruptedKnight"),
        ("cebca57dae55ae548aa713fc323194e1", "Falling_5", "Amog"),
        ("e8b271bff679f654983540ba618f2e6c", "KnightJump_0", "Knight"),
        ("e997d70d56e29924ba0cda425c7e7445", "Orc_Idle", "Ogre"),
        ("f88fdb95c79d39c4b9a44d0d3d179c96", "Jump_2", "BloodMage"),
        ("fe9fcd40b867a324c9649e9f6a6029b1", "BirdoIdle", "Birdo"),
    };

    private static readonly (string id, string clip, string prefab)[] CandidatesBatch3 = {
        ("1b3a2c01fadc66f42a2c881fadff121c", "Jump", "Astronaut"),
        ("1d3bf757c5a84564d9da68f423be1bfb", "FoxIdle", "FoxWizard"),
        ("216617a5f587c91489f849626b97dabc", "Idle_1", "Bandit"),
        ("311e03e7771abe847b856b79fe37ddb1", "FoxFalling", "FoxWizard"),
        ("3ada34c37f4784f448ebdc93a4aa3b43", "Run_3", "Roberto"),
        ("3d0854e33e20ba24cb74a7b7cf21ef4e", "HoverJump", "HoverGuy"),
        ("3d155c7f215f565499c4ce1c88126585", "Orc_Falling", "Ogre"),
        ("3e34dcb288c50d54c952bd3e6e9eadc6", "Orc_Jump", "Ogre"),
        ("3f57d5332672bee4fbd5d555cdb73fe3", "NoelleJump", "Noelle"),
        ("44d435e4acfe5a54ab1f82e9a312a45e", "Orc_Walk", "Ogre"),
        ("4a89a79b6eee7a842a33d0a8e328ec68", "Falling_6", "B_Megachad"),
        ("546a0e8add941314c97e7dcce1dc17d0", "CowboyRun", "RobotCowboy"),
        ("551d6a2f21bfa87448935a2848471d55", "IdleNew2", "Athena"),
        ("5685cc39f73712d4a8e3f6865bf97341", "NoelleRun", "Noelle"),
        ("59361321944b3f946a437d165e64cea4", "FoxRun", "FoxWizard"),
        ("5e86657e262f299468ba533a4e13a6b2", "Jumping", "George"),
        ("625b72e4d62c60e4c88db481082305bd", "RunForEnemyAnimation", "Bandit"),
        ("67aba0cbe11e8824ba5e0adb05ebe13e", "Jump_9", "Ninja"),
        ("6b80753f2616b974f826e8da76624769", "HoverIdle", "HoverGuy"),
        ("78eebffc0665e574e833d7fb3043c74b", "Moving", "George"),
        ("89272ce2712cce540a5c0b8467c9e04f", "Run_2", "Dicehead"),
        ("8a5c1a6f38b929c4bbbf7699a69e9365", "NoelleIdle", "Noelle"),
        ("8a6b5d16e42866640a13d5f2cd56d9c8", "A_Push", "Calcium"),
        ("9de844079976daa438663a89f6e5ff4e", "Jump_5", "Amog"),
        ("9e091b9700cf9d148b4401bcaca6bcc7", "Idle2_0", "Dicehead"),
        ("a1b787f1821163744adfd997807d3feb", "Run_0", "Bandit"),
        ("a5dcfe93f716b974d85895464804d516", "HoverGrind", "HoverGuy"),
        ("b5e0f998c10ffc740be75ac2874682c9", "Jump_6", "B_Megachad"),
        ("b947cd5e81ce4b64aa9f02b84e3be122", "Jump_4", "Roberto"),
        ("bee7e9aed9cbb29449fd0cab4584eb5c", "Run", "Astronaut"),
        ("c048f095a81987f479fc2316bcc08fb4", "Jump_3", "Dicehead"),
        ("c06cf2a159c96f140aa38b635da50640", "Idle2", "Astronaut"),
        ("c3d77f7843b91464aa61a5e40f338a08", "Walking", "B_Megachad"),
        ("cdd80f5d8c86e2b469aed8086fe922eb", "CowboyJump", "RobotCowboy"),
        ("cfd249cbeca8b304380800c215e52985", "FoxJump4", "FoxWizard"),
        ("d2712c34ebef02746b3bda5b287025e8", "BirdoRun", "Birdo"),
        ("d72ef8c5d014dde4194dc643ab4665fc", "HoverMove", "HoverGuy"),
        ("d8c5034f441c1b24fa8b7e578655f29d", "BirdoJump", "Birdo"),
        ("df789e14353836641a8c9cac3f19f41c", "Jump_7", "Monke"),
        ("f708298fd60c85a48a11c222bbdb7180", "HoverFalling", "HoverGuy"),
    };

    private static readonly (string id, string clip, string prefab)[] CandidatesBatch4 = {
        ("1d3bf757c5a84564d9da68f423be1bfb", "FoxIdle", "FoxWizard"),
        ("216617a5f587c91489f849626b97dabc", "Idle_1", "Bandit"),
        ("3ada34c37f4784f448ebdc93a4aa3b43", "Run_3", "Roberto"),
        ("3d0854e33e20ba24cb74a7b7cf21ef4e", "HoverJump", "HoverGuy"),
        ("44d435e4acfe5a54ab1f82e9a312a45e", "Orc_Walk", "Ogre"),
        ("546a0e8add941314c97e7dcce1dc17d0", "CowboyRun", "RobotCowboy"),
        ("551d6a2f21bfa87448935a2848471d55", "IdleNew2", "Athena"),
        ("5685cc39f73712d4a8e3f6865bf97341", "NoelleRun", "Noelle"),
        ("59361321944b3f946a437d165e64cea4", "FoxRun", "FoxWizard"),
        ("5e86657e262f299468ba533a4e13a6b2", "Jumping", "George"),
        ("625b72e4d62c60e4c88db481082305bd", "RunForEnemyAnimation", "Bandit"),
        ("67aba0cbe11e8824ba5e0adb05ebe13e", "Jump_9", "Ninja"),
        ("6b80753f2616b974f826e8da76624769", "HoverIdle", "HoverGuy"),
        ("78eebffc0665e574e833d7fb3043c74b", "Moving", "George"),
        ("89272ce2712cce540a5c0b8467c9e04f", "Run_2", "Dicehead"),
        ("8a5c1a6f38b929c4bbbf7699a69e9365", "NoelleIdle", "Noelle"),
        ("9e091b9700cf9d148b4401bcaca6bcc7", "Idle2_0", "Dicehead"),
        ("a1b787f1821163744adfd997807d3feb", "Run_0", "Bandit"),
        ("a5dcfe93f716b974d85895464804d516", "HoverGrind", "HoverGuy"),
        ("b947cd5e81ce4b64aa9f02b84e3be122", "Jump_4", "Roberto"),
        ("bee7e9aed9cbb29449fd0cab4584eb5c", "Run", "Astronaut"),
        ("c048f095a81987f479fc2316bcc08fb4", "Jump_3", "Dicehead"),
        ("c06cf2a159c96f140aa38b635da50640", "Idle2", "Astronaut"),
        ("c3d77f7843b91464aa61a5e40f338a08", "Walking", "B_Megachad"),
        ("cdd80f5d8c86e2b469aed8086fe922eb", "CowboyJump", "RobotCowboy"),
        ("d2712c34ebef02746b3bda5b287025e8", "BirdoRun", "Birdo"),
        ("d8c5034f441c1b24fa8b7e578655f29d", "BirdoJump", "Birdo"),
        ("df789e14353836641a8c9cac3f19f41c", "Jump_7", "Monke"),
        ("f708298fd60c85a48a11c222bbdb7180", "HoverFalling", "HoverGuy"),
    };

    private static readonly (string id, string clip, string prefab)[] CandidatesSharedA = {
        ("87ee74cbcd463f34b9b3eaf55283fd54", "CowboyGrind 1", "Ogre"),
        ("8c6bab715b5d92e4ebae648f39bd5e0f", "Walk", "Monke")
    };
    private static readonly (string id, string clip, string prefab)[] CandidatesSharedB = {
        ("87ee74cbcd463f34b9b3eaf55283fd54", "CowboyGrind 1", "RobotCowboy"),
        ("8c6bab715b5d92e4ebae648f39bd5e0f", "Walk", "MonkeCage")
    };

    private static readonly (string id, string clip, string prefab)[] CandidatesDivergent = {
        ("1b3a2c01fadc66f42a2c881fadff121c", "Jump", "Astronaut"),
        ("311e03e7771abe847b856b79fe37ddb1", "FoxFalling", "FoxWizard"),
        ("3d155c7f215f565499c4ce1c88126585", "Orc_Falling", "Ogre"),
        ("3e34dcb288c50d54c952bd3e6e9eadc6", "Orc_Jump", "Ogre"),
        ("3f57d5332672bee4fbd5d555cdb73fe3", "NoelleJump", "Noelle"),
        ("4a89a79b6eee7a842a33d0a8e328ec68", "Falling_6", "B_Megachad"),
        ("8a6b5d16e42866640a13d5f2cd56d9c8", "A_Push", "Calcium"),
        ("9de844079976daa438663a89f6e5ff4e", "Jump_5", "Amog"),
        ("b5e0f998c10ffc740be75ac2874682c9", "Jump_6", "B_Megachad"),
        ("cfd249cbeca8b304380800c215e52985", "FoxJump4", "FoxWizard"),
        ("d72ef8c5d014dde4194dc643ab4665fc", "HoverMove", "HoverGuy"),
    };

    private static (string id, string clip, string prefab)[] Candidates =>
        Environment.GetEnvironmentVariable("OPENBONK_STUDY_BATCH") == "7" ? CandidatesDivergent :
        Environment.GetEnvironmentVariable("OPENBONK_STUDY_BATCH") == "5" ? CandidatesSharedA :
        Environment.GetEnvironmentVariable("OPENBONK_STUDY_BATCH") == "6" ? CandidatesSharedB :
        Environment.GetEnvironmentVariable("OPENBONK_STUDY_BATCH") == "4" ? CandidatesBatch4 :
        Environment.GetEnvironmentVariable("OPENBONK_STUDY_BATCH") == "3" ? CandidatesBatch3 :
        Environment.GetEnvironmentVariable("OPENBONK_STUDY_BATCH") == "2" ? CandidatesBatch2 : CandidatesBatch1;

    public static void BatchBuild()
    {
        string directory = Setting("OPENBONK_STUDY_DIR");
        if (!Directory.Exists(directory)) throw new Exception("Study directory missing");
        bool android = Environment.GetEnvironmentVariable("OPENBONK_STUDY_ANDROID") == "1";
        var builds = Candidates.Select(c => {
            string path = "Assets/AnimationClip/" + c.clip + ".anim";
            if (AssetDatabase.AssetPathToGUID(path) != c.id) throw new Exception("Clip GUID mismatch: " + path);
            string prefab = "Assets/GameObject/" + c.prefab + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) == null) throw new Exception("Prefab missing: " + prefab);
            return new AssetBundleBuild { assetBundleName = "reference-" + c.id, assetNames = android ? new[] { path, prefab } : new[] { path } };
        }).ToArray();
        // Android study bundles each include their prefab. Some clips share a
        // prefab, so build them separately to avoid assigning one asset to
        // multiple bundles in a single BuildAssetBundles invocation.
        if (android)
        {
            foreach (var build in builds)
                if (BuildPipeline.BuildAssetBundles(directory, new[] { build }, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.Android) == null)
                    throw new Exception("Study bundle build failed: " + build.assetBundleName);
        }
        else if (BuildPipeline.BuildAssetBundles(directory, builds, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneLinux64) == null)
            throw new Exception("Study bundle build failed");
        Debug.Log("OPENBONK_CLIP_BATCH_BUILD_PASS count=" + builds.Length);
    }

    public static void BatchCompare()
    {
        string directory = Setting("OPENBONK_STUDY_DIR");
        int compared = 0, errors = 0;
        foreach (var c in Candidates)
        {
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + c.prefab + ".prefab");
                if (prefab == null) throw new Exception("Prefab missing");
                var a = Capture(Path.Combine(directory, "reference-" + c.id), prefab, out float da);
                var b = Capture(Path.Combine(directory, "steam-" + c.id), prefab, out float db);
                if (da != db) throw new Exception("Duration differs");
                float position = 0f, rotation = 0f, scale = 0f, movement = 0f, rotationMovement = 0f;
                int worstRotationBone = -1, worstRotationSample = -1;
                for (int t = 0; t < a.Length; t++)
                {
                    if (a[t].Length != b[t].Length) throw new Exception("Hierarchy differs");
                    for (int i = 0; i < a[t].Length; i++)
                    {
                        position = Math.Max(position, Vector3.Distance(a[t][i].position, b[t][i].position));
                        float rotationError = Quaternion.Angle(a[t][i].rotation, b[t][i].rotation);
                        if (rotationError > rotation) { rotation = rotationError; worstRotationBone = i; worstRotationSample = t; }
                        scale = Math.Max(scale, Vector3.Distance(a[t][i].scale, b[t][i].scale));
                        movement = Math.Max(movement, Vector3.Distance(a[t][i].position, a[0][i].position));
                        rotationMovement = Math.Max(rotationMovement, Quaternion.Angle(a[t][i].rotation, a[0][i].rotation));
                    }
                }
                if (movement < .0001f && rotationMovement < .01f) throw new Exception("No observed motion; inconclusive");
                Debug.Log($"OPENBONK_CLIP_BATCH_POSE id={c.id} clip={c.clip} prefab={c.prefab} samples={a.Length} transforms={a[0].Length} maxPosition={position:R} maxRotationDegrees={rotation:R} maxScale={scale:R} maxMovement={movement:R} maxRotationMovementDegrees={rotationMovement:R}");
                if (worstRotationBone >= 0)
                {
                    var bones = prefab.GetComponentsInChildren<Transform>(true);
                    if (bones.Length != a[0].Length) throw new Exception("Prefab hierarchy differs from capture");
                    string path = AnimationUtility.CalculateTransformPath(bones[worstRotationBone], prefab.transform);
                    Debug.Log($"OPENBONK_CLIP_BATCH_WORST id={c.id} prefab={c.prefab} bone={path} sample={worstRotationSample} seconds={da * worstRotationSample / (a.Length - 1):R} rotationDegrees={rotation:R}");
                }
                compared++;
            }
            catch (Exception e) { errors++; Debug.LogError("OPENBONK_CLIP_BATCH_ERROR id=" + c.id + " " + e.Message); }
        }
        if (errors != 0) throw new Exception("Inconclusive batch clips: " + errors);
        Debug.Log("OPENBONK_CLIP_BATCH_COMPARE_PASS count=" + compared);
    }
    private static string Setting(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value)) throw new Exception("Missing " + name);
        return value;
    }

    public static void Build()
    {
        string directory = Setting("OPENBONK_STUDY_DIR");
        if (!Directory.Exists(directory)) throw new Exception("Study directory missing");
        bool android = Environment.GetEnvironmentVariable("OPENBONK_STUDY_ANDROID") == "1";
        var build = new AssetBundleBuild { assetBundleName = "clip-reference", assetNames = android
            ? new[] { Setting("OPENBONK_STUDY_CLIP"), Setting("OPENBONK_STUDY_PREFAB") }
            : new[] { Setting("OPENBONK_STUDY_CLIP") } };
        if (BuildPipeline.BuildAssetBundles(directory, new[] { build }, BuildAssetBundleOptions.UncompressedAssetBundle, android ? BuildTarget.Android : BuildTarget.StandaloneLinux64) == null)
            throw new Exception("Study bundle build failed");
        Debug.Log("OPENBONK_CLIP_STUDY_BUILD_PASS");
    }

    private struct Pose { public Vector3 position, scale; public Quaternion rotation; }

    private static Pose[][] Capture(string path, GameObject prefab, out float duration)
    {
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new Exception("Cannot load study bundle");
        GameObject instance = null;
        try
        {
            var clip = bundle.LoadAllAssets<AnimationClip>().Single();
            instance = UnityEngine.Object.Instantiate(prefab);
            GameObject sampleRoot = instance;
            if (prefab.name == "MonkeCage")
            {
                // This prefab's quest monkey has its own Animator under an
                // inactive child; sampling the outer cage moves nothing.
                var animator = instance.GetComponentsInChildren<Animator>(true)
                    .Single(x => x.runtimeAnimatorController != null && x.runtimeAnimatorController.name == "AnimatorMonkeQuest");
                for (Transform t = animator.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
                sampleRoot = animator.gameObject;
            }
            var bones = instance.GetComponentsInChildren<Transform>(true);
            duration = clip.length;
            if (bones.Length < 2 || duration <= 0f) throw new Exception("Invalid clip or prefab");
            var result = new Pose[101][];
            for (int i = 0; i < result.Length; i++)
            {
                clip.SampleAnimation(sampleRoot, duration * i / (result.Length - 1));
                result[i] = bones.Select(x => new Pose { position = x.localPosition, rotation = x.localRotation, scale = x.localScale }).ToArray();
            }
            Debug.Log($"OPENBONK_CLIP_STUDY_CAPTURE clip={clip.name} duration={duration:R} transforms={bones.Length}");
            return result;
        }
        finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance); bundle.Unload(true); }
    }

    public static void Compare()
    {
        string directory = Setting("OPENBONK_STUDY_DIR");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Setting("OPENBONK_STUDY_PREFAB"));
        if (prefab == null) throw new Exception("Study prefab missing");
        var a = Capture(Path.Combine(directory, "clip-reference"), prefab, out float durationA);
        var b = Capture(Path.Combine(directory, "clip-steam"), prefab, out float durationB);
        if (durationA != durationB) throw new Exception("Clip duration differs");
        float position = 0f, rotation = 0f, scale = 0f, movement = 0f, rotationMovement = 0f;
        for (int t = 0; t < a.Length; t++)
        {
            if (a[t].Length != b[t].Length) throw new Exception("Hierarchy mismatch");
            for (int i = 0; i < a[t].Length; i++)
            {
                position = Math.Max(position, Vector3.Distance(a[t][i].position, b[t][i].position));
                rotation = Math.Max(rotation, Quaternion.Angle(a[t][i].rotation, b[t][i].rotation));
                scale = Math.Max(scale, Vector3.Distance(a[t][i].scale, b[t][i].scale));
                movement = Math.Max(movement, Vector3.Distance(a[t][i].position, a[0][i].position));
                rotationMovement = Math.Max(rotationMovement, Quaternion.Angle(a[t][i].rotation, a[0][i].rotation));
            }
        }
        if (movement < .0001f && rotationMovement < .01f) throw new Exception("Clip did not move prefab; comparison inconclusive");
        Debug.Log($"OPENBONK_CLIP_STUDY_POSE samples={a.Length} transforms={a[0].Length} maxPosition={position:R} maxRotationDegrees={rotation:R} maxScale={scale:R} maxMovement={movement:R} maxRotationMovementDegrees={rotationMovement:R}");
    }
}
#endif
