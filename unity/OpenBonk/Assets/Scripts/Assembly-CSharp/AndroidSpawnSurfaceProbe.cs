#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in, disposable run: exercise the actual multi-hit spawn selector on Desert.
internal sealed class AndroidSpawnSurfaceProbe : MonoBehaviour
{
    static bool enabledForRun;
    static bool stageThreeForRun;
    static int terrainSeed = int.MinValue;
    bool observeOnly;
    bool configurationOnly;
    bool originalNaturalPose;
    Vector3 replayPosition;
    Quaternion replayRotation;
    float replayFov;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool observeOnly = intent.Call<bool>("getBooleanExtra", "megabonk_desert_start_probe", false);
        bool configurationOnly = intent.Call<bool>("getBooleanExtra", "megabonk_desert_progress_probe", false)
            || intent.Call<bool>("getBooleanExtra", "megabonk_desert_config_only", false);
        if (!observeOnly && !configurationOnly && !intent.Call<bool>("getBooleanExtra", "megabonk_spawn_surface_probe", false)) return;
        stageThreeForRun = observeOnly && intent.Call<bool>("getBooleanExtra", "megabonk_desert_stage3_probe", false);
        enabledForRun = true;
        terrainSeed = intent.Call<int>("getIntExtra", "megabonk_desert_terrain_seed", int.MinValue);
        var obj = new GameObject("spawn surface probe"); DontDestroyOnLoad(obj); var probe = obj.AddComponent<AndroidSpawnSurfaceProbe>(); probe.observeOnly = observeOnly; probe.configurationOnly = configurationOnly;
        probe.originalNaturalPose = intent.Call<bool>("getBooleanExtra", "megabonk_desert_original_natural_pose", false);
        if (probe.originalNaturalPose)
        {
            probe.replayPosition = new Vector3(intent.Call<float>("getFloatExtra", "megabonk_camera_x", -381.769226f), intent.Call<float>("getFloatExtra", "megabonk_camera_y", 15.826862f), intent.Call<float>("getFloatExtra", "megabonk_camera_z", 222.407349f));
            probe.replayRotation = new Quaternion(intent.Call<float>("getFloatExtra", "megabonk_camera_qx", 0f), intent.Call<float>("getFloatExtra", "megabonk_camera_qy", 0.579818f), intent.Call<float>("getFloatExtra", "megabonk_camera_qz", 0f), intent.Call<float>("getFloatExtra", "megabonk_camera_qw", -0.814746f));
            probe.replayFov = intent.Call<float>("getFloatExtra", "megabonk_camera_fov", 85f);
        }
    }
    public static void Configure(RunConfig config)
    {
        if (!enabledForRun) return;
        MapData map = DataManager.Instance != null ? DataManager.Instance.GetMap(EMap.Desert) : null;
        if (map == null || map.stages == null || map.stages.Length == 0) { Debug.LogError("Spawn surface probe FAIL: Desert unavailable"); return; }
        if (stageThreeForRun)
        {
            if (map.stages.Length < 3 || map.stages[2] == null) { Debug.LogError("Desert stage-three probe INCONCLUSIVE: stage unavailable"); return; }
            Debug.Log("Desert stage-three probe: selected=" + map.stages[2].name + ", stage index forced to 2");
        }
        config.mapData = map; config.stageData = map.stages[stageThreeForRun ? 2 : 0]; config.mapTierIndex = 0;
    }
    public static int InitialStageIndex(int original) => stageThreeForRun ? 1 : original;
    public static int TerrainSeed(int original) => enabledForRun && terrainSeed != int.MinValue ? terrainSeed : original;
    public static void LogTerrainHeights(MapGenerator generator, int original)
    {
        if (!enabledForRun || terrainSeed == int.MinValue) return;
        var heights = generator.heightMap;
        if (heights == null || heights.GetLength(0) != 241 || heights.GetLength(1) != 241) { Debug.LogError("Desert seed probe: invalid height map"); return; }
        Debug.Log($"Desert seed probe: original={original} used={terrainSeed} height length={heights.Length}");
        var bytes = new byte[heights.Length * sizeof(float)]; System.Buffer.BlockCopy(heights, 0, bytes, 0, bytes.Length);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, "desert-height-map-f32.bin"), bytes);
        Debug.Log($"Desert seed probe: dumped {heights.Length} floats, littleEndian={System.BitConverter.IsLittleEndian}");
        int[,] coordinates = { {0,0}, {10,10}, {40,180}, {80,120}, {120,120}, {160,70}, {200,200}, {240,240} };
        for (int i = 0; i < coordinates.GetLength(0); i++) { int x = coordinates[i,0], y = coordinates[i,1]; Debug.Log($"Desert seed probe: height[{x},{y}]={heights[x,y].ToString("R", System.Globalization.CultureInfo.InvariantCulture)}"); }
    }
    IEnumerator Start()
    {
        if (configurationOnly) yield break;
        float deadline = Time.realtimeSinceStartup + 240f;
        while (Time.realtimeSinceStartup < deadline && (MapController.currentMap == null || MapController.currentMap.eMap != EMap.Desert || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        if (MapController.currentMap == null || MapController.currentMap.eMap != EMap.Desert || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted) { Debug.LogError("Spawn surface probe FAIL: Desert startup unavailable"); yield break; }
        if (observeOnly)
        {
            for (int i = 0; i < 6; i++)
            {
                var p = MyPlayer.Instance;
                if (p == null) { Debug.Log("Desert start observation ended: player unavailable"); yield break; }
                bool groundFound = Physics.Raycast(p.transform.position + Vector3.up, Vector3.down, out var hit, 100f, GameManager.Instance.whatIsGroundAndObjects);
                Debug.Log($"Desert start observation: sample={i} player={p.transform.position} ground={groundFound} groundY={(groundFound ? hit.point.y : float.NaN):F2} collider={(groundFound ? hit.collider.name : "none")} camera={(Camera.main != null ? Camera.main.transform.position.ToString() : "none")}");
                yield return new WaitForSecondsRealtime(3f);
            }
            float oldTimeScale = Time.timeScale; Time.timeScale = 0f;
            yield return new WaitForEndOfFrame();
            var edgeRenderers = new System.Collections.Generic.List<MeshRenderer>();
            var edgeMaterials = new System.Collections.Generic.List<Material>();
            var diagnosticMaterials = new System.Collections.Generic.List<Material>();
            foreach (var renderer in FindObjectsOfType<MeshRenderer>())
            {
                var mat = renderer.sharedMaterial;
                if (!renderer.enabled || mat == null || !mat.HasProperty("_SuppressOutline") || (mat.name != "m_1_TerrainDesertEdge" && mat.name != "m_3_TerrainDesertEdge")) continue;
                Debug.Log($"Desert edge observed: material={mat.name}, suppressed={mat.GetFloat("_SuppressOutline")}, object={renderer.name}");
                edgeRenderers.Add(renderer); edgeMaterials.Add(mat);
                var clone = new Material(mat); clone.SetFloat("_SuppressOutline", 0f); if (originalNaturalPose) clone.SetColor("_OutlineColor", Color.magenta); diagnosticMaterials.Add(clone);
            }
            // Revisit a camera location from an affected run, with identical scene
            // state between the actual material and its old-outline control.
            var sceneCamera = Camera.main;
            if (sceneCamera != null && edgeRenderers.Count > 0)
            {
                var oldPosition = sceneCamera.transform.position; var oldRotation = sceneCamera.transform.rotation; var oldFov = sceneCamera.fieldOfView; var oldTarget = sceneCamera.targetTexture; var oldActive = RenderTexture.active;
                var target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
                float edgeScale = stageThreeForRun ? 1.2f : 1f;
                if (originalNaturalPose)
                {
                    sceneCamera.transform.position = replayPosition;
                    sceneCamera.transform.rotation = replayRotation;
                    sceneCamera.fieldOfView = replayFov;
                    Debug.Log($"Desert replay camera: position={replayPosition.ToString("F6")}, rotation={replayRotation.ToString("F6")}, fov={replayFov}");
                }
                else { sceneCamera.transform.position = new Vector3(465.72f, 54.61f, -333.38f) * edgeScale; sceneCamera.transform.LookAt(new Vector3(460.54f, 52.18f, -322.56f) * edgeScale); }
                sceneCamera.targetTexture = target;
                sceneCamera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply(); var corrected = image.GetPixels32();
                System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-fixed-camera.png", image.EncodeToPNG());
                for (int i = 0; i < edgeRenderers.Count; i++) edgeRenderers[i].sharedMaterial = diagnosticMaterials[i];
                sceneCamera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply(); var prior = image.GetPixels32(); int recoveredAtCamera = 0;
                for (int i = 0; i < corrected.Length; i++) if (prior[i].r < 12 && prior[i].g < 12 && prior[i].b < 12 && corrected[i].r > 70 && corrected[i].g > 60) recoveredAtCamera++;
                if (originalNaturalPose)
                {
                    int magenta = 0, changed = 0;
                    for (int i = 0; i < prior.Length; i++)
                    {
                        if (prior[i].r > 100 && prior[i].b > 100 && prior[i].g * 2 < prior[i].r && prior[i].g * 2 < prior[i].b) magenta++;
                        if (Mathf.Abs(prior[i].r - corrected[i].r) > 10 || Mathf.Abs(prior[i].g - corrected[i].g) > 10 || Mathf.Abs(prior[i].b - corrected[i].b) > 10) changed++;
                    }
                    Debug.Log($"Desert original natural pose comparison: renderers={edgeRenderers.Count}, magentaPixels={magenta}, changedPixels={changed}");
                }
                System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-unfixed-camera.png", image.EncodeToPNG());
                Debug.Log($"Desert edge fixed camera comparison: renderers={edgeRenderers.Count}, recoveredPixels={recoveredAtCamera}");
                for (int i = 0; i < edgeRenderers.Count; i++) edgeRenderers[i].sharedMaterial = edgeMaterials[i];
                sceneCamera.targetTexture = oldTarget; sceneCamera.transform.position = oldPosition; sceneCamera.transform.rotation = oldRotation; sceneCamera.fieldOfView = oldFov; RenderTexture.active = oldActive; target.Release(); Destroy(target); Destroy(image);
            }
            if (originalNaturalPose) { foreach (var material in diagnosticMaterials) Destroy(material); Time.timeScale = oldTimeScale; yield break; }
            var fixedImage = ScreenCapture.CaptureScreenshotAsTexture(); var fixedPixels = fixedImage.GetPixels32();
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-fixed.png", fixedImage.EncodeToPNG()); Destroy(fixedImage);
            for (int i = 0; i < edgeRenderers.Count; i++) edgeRenderers[i].sharedMaterial = diagnosticMaterials[i];
            yield return new WaitForEndOfFrame();
            var unfixedImage = ScreenCapture.CaptureScreenshotAsTexture(); var unfixedPixels = unfixedImage.GetPixels32(); int recovered = 0;
            for (int i = 0; i < fixedPixels.Length; i++)
                if (unfixedPixels[i].r < 12 && unfixedPixels[i].g < 12 && unfixedPixels[i].b < 12 && fixedPixels[i].r > 70 && fixedPixels[i].g > 60) recovered++;
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-unfixed.png", unfixedImage.EncodeToPNG()); Destroy(unfixedImage);
            Debug.Log($"Desert edge fix comparison: edgeRenderers={edgeRenderers.Count}, recoveredPixels={recovered}");
            for (int i = 0; i < edgeRenderers.Count; i++) { if (edgeRenderers[i] != null) edgeRenderers[i].sharedMaterial = edgeMaterials[i]; Destroy(diagnosticMaterials[i]); }
            yield return new WaitForEndOfFrame();
            SaveScreen("desert-sky-baseline");
            foreach (var kind in new[] { typeof(MeshRenderer), typeof(SkinnedMeshRenderer), typeof(ParticleSystemRenderer) })
            {
                var changed = new System.Collections.Generic.List<Renderer>();
                foreach (var renderer in FindObjectsOfType<Renderer>()) if (renderer.enabled && kind.IsInstanceOfType(renderer)) { changed.Add(renderer); renderer.enabled = false; }
                yield return new WaitForEndOfFrame();
                SaveScreen("desert-sky-without-" + kind.Name);
                Debug.Log($"Desert sky isolation: disabledType={kind.Name}, count={changed.Count}");
                foreach (var renderer in changed) if (renderer != null) renderer.enabled = true;
                yield return new WaitForEndOfFrame();
            }
            // Bisect the active mesh renderers against pixels that are black in
            // the baseline but bright with all meshes disabled. A zero score is
            // inconclusive (for example, two meshes may overlap the same pixels).
            var meshes = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in FindObjectsOfType<MeshRenderer>()) if (renderer.enabled) meshes.Add(renderer);
            yield return new WaitForEndOfFrame(); var baseline = ScreenCapture.CaptureScreenshotAsTexture();
            foreach (var r in meshes) r.enabled = false;
            yield return new WaitForEndOfFrame(); var clear = ScreenCapture.CaptureScreenshotAsTexture();
            foreach (var r in meshes) if (r != null) r.enabled = true;
            var darkPixels = new System.Collections.Generic.List<int>();
            Color32[] basePixels = baseline.GetPixels32(), clearPixels = clear.GetPixels32();
            for (int y = baseline.height * 3 / 5; y < baseline.height - 35; y += 4)
                for (int x = 300; x < baseline.width - 300; x += 4)
                {
                    int index = y * baseline.width + x; var a = basePixels[index]; var b = clearPixels[index];
                    if (a.r < 12 && a.g < 12 && a.b < 12 && b.r > 70 && b.g > 60) darkPixels.Add(index);
                }
            Destroy(baseline); Destroy(clear);
            Debug.Log($"Desert sky mesh bisection: candidates={meshes.Count}, blackSkySamples={darkPixels.Count}");
            while (meshes.Count > 1 && darkPixels.Count > 100)
            {
                int midpoint = meshes.Count / 2, bestSide = -1, bestScore = 0;
                for (int side = 0; side < 2; side++)
                {
                    int start = side == 0 ? 0 : midpoint, end = side == 0 ? midpoint : meshes.Count;
                    for (int i = start; i < end; i++) if (meshes[i] != null) meshes[i].enabled = false;
                    yield return new WaitForEndOfFrame(); var image = ScreenCapture.CaptureScreenshotAsTexture(); var pixels = image.GetPixels32(); int score = 0;
                    foreach (int index in darkPixels) if (pixels[index].r > 70 && pixels[index].g > 60) score++;
                    Destroy(image);
                    for (int i = start; i < end; i++) if (meshes[i] != null) meshes[i].enabled = true;
                    Debug.Log($"Desert sky mesh bisection: count={meshes.Count}, side={side}, exposedSamples={score}");
                    if (score > bestScore) { bestScore = score; bestSide = side; }
                    yield return new WaitForEndOfFrame();
                }
                if (bestSide < 0 || bestScore < 20) break;
                meshes = bestSide == 0 ? meshes.GetRange(0, midpoint) : meshes.GetRange(midpoint, meshes.Count - midpoint);
            }
            if (meshes.Count <= 10) { foreach (var r in meshes) if (r != null) Debug.Log($"Desert sky mesh candidate: object={r.name}, material={(r.sharedMaterial != null ? r.sharedMaterial.name : "none")}, shader={(r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "none")}, bounds={r.bounds}"); }
            else Debug.Log($"Desert sky mesh bisection INCONCLUSIVE: remaining={meshes.Count}");
            if (meshes.Count == 1 && meshes[0] != null && meshes[0].sharedMaterial != null)
            {
                var selected = meshes[0]; var original = selected.sharedMaterial; var temporary = new Material(original);
                selected.sharedMaterial = temporary;
                foreach (string passName in new[] { "OUTLINE", "FORWARD" })
                {
                    temporary.SetShaderPassEnabled(passName, false);
                    yield return new WaitForEndOfFrame();
                    var capture = ScreenCapture.CaptureScreenshotAsTexture(); var pixels = capture.GetPixels32(); int exposed = 0;
                    foreach (int index in darkPixels) if (pixels[index].r > 70 && pixels[index].g > 60) exposed++;
                    System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-no-" + passName + ".png", capture.EncodeToPNG());
                    Destroy(capture);
                    Debug.Log($"Desert sky material pass: disabled={passName}, flag={temporary.GetShaderPassEnabled(passName)}, exposedSamples={exposed}");
                    temporary.SetShaderPassEnabled(passName, true);
                    yield return new WaitForEndOfFrame();
                }
                if (temporary.HasProperty("_OutlineWidth"))
                {
                    float width = temporary.GetFloat("_OutlineWidth"); temporary.SetFloat("_OutlineWidth", 0f);
                    yield return new WaitForEndOfFrame();
                    var capture = ScreenCapture.CaptureScreenshotAsTexture(); var pixels = capture.GetPixels32(); int exposed = 0;
                    foreach (int index in darkPixels) if (pixels[index].r > 70 && pixels[index].g > 60) exposed++;
                    System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-zero-outline.png", capture.EncodeToPNG()); Destroy(capture);
                    Debug.Log($"Desert sky material width: original={width}, exposedSamples={exposed}");
                    temporary.SetFloat("_OutlineWidth", width);
                }
                foreach (string variant in new[] { "outline", "forward" })
                {
                    var shader = Resources.Load<Shader>("DesertDiag/" + variant);
                    if (shader == null || !shader.isSupported) { Debug.LogError("Desert sky shader variant unavailable: " + variant); continue; }
                    temporary.shader = shader;
                    yield return new WaitForEndOfFrame();
                    var capture = ScreenCapture.CaptureScreenshotAsTexture(); var pixels = capture.GetPixels32(); int black = 0, exposed = 0;
                    foreach (int index in darkPixels) { var p = pixels[index]; if (p.r < 12 && p.g < 12 && p.b < 12) black++; if (p.r > 70 && p.g > 60) exposed++; }
                    System.IO.File.WriteAllBytes(Application.persistentDataPath + "/desert-edge-" + variant + "-only.png", capture.EncodeToPNG()); Destroy(capture);
                    Debug.Log($"Desert sky shader variant: {variant}, blackSamples={black}, exposedSamples={exposed}, total={darkPixels.Count}");
                }
                selected.sharedMaterial = original; Destroy(temporary);
            }
            Time.timeScale = oldTimeScale;
            yield break;
        }
        var high = GameObject.CreatePrimitive(PrimitiveType.Cube); var low = GameObject.CreatePrimitive(PrimitiveType.Cube);
        int ground = LayerMask.NameToLayer("Ground");
        high.layer = low.layer = ground;
        high.transform.position = new Vector3(10000, 10, 10000); low.transform.position = new Vector3(10000, 0, 10000);
        Physics.SyncTransforms();
        var sampler = typeof(SpawnPositions).GetMethod("MyRaycastAll", BindingFlags.Static | BindingFlags.NonPublic);
        var hits = (RaycastHit[])sampler.Invoke(null, new object[] { new Vector3(10000, 40, 10000), Vector3.down, 1 << ground, 100f, 10 });
        RaycastHit chosen = SpawnPositions.FindHitClosestToPlayerY(hits, out bool found, false);
        bool pass = hits.Length >= 2 && found && chosen.collider == high.GetComponent<Collider>();
        Debug.Log($"Spawn surface probe {(pass ? "PASS" : "FAIL")}: map={MapController.currentMap.name}, hits={hits.Length}, selectedY={chosen.point.y:F2}, expectedY=10.50");
        Destroy(high); Destroy(low);

        // Cover the real candidate area, then invoke the same position sampler
        // used by enemy waves, including bounds, prediction and multi-ray search.
        high = GameObject.CreatePrimitive(PrimitiveType.Cube); low = GameObject.CreatePrimitive(PrimitiveType.Cube);
        high.layer = low.layer = ground;
        Vector3 player = MyPlayer.Instance.transform.position;
        high.transform.position = player + Vector3.up * 100f; low.transform.position = player + Vector3.up * 50f;
        high.transform.localScale = low.transform.localScale = new Vector3(4000f, 1f, 4000f);
        high.GetComponent<Renderer>().enabled = false; low.GetComponent<Renderer>().enabled = false;
        Physics.SyncTransforms();
        int passed = 0, abovePlatform = 0;
        for (int i = 0; i < 25; i++)
        {
            Vector3 position = SpawnPositions.GetEnemySpawnPositionBiased(null, 0f, 50, 35f);
            var observed = (RaycastHit[])sampler.Invoke(null, new object[] { position + Vector3.up * 800f, Vector3.down, GameManager.Instance.whatIsGroundAndObjects.value, 1500f, 10 });
            float highest = float.NegativeInfinity;
            foreach (var hit in observed)
                if (!hit.collider.CompareTag("Ignore") && !hit.collider.CompareTag("CameraIgnore")) highest = Mathf.Max(highest, hit.point.y);
            if (highest > player.y + 100.55f) abovePlatform++;
            bool samplePassed = position != SpawnPositions.INVALID_POS && observed.Length >= 2 && Mathf.Abs(position.y - highest) < .05f;
            if (samplePassed) passed++;
            else Debug.LogError($"Spawn surface diagnostic mismatch: returned={position}, highest={highest:F2}, hits={observed.Length}");
        }
        Debug.Log($"Spawn surface integrated {(passed == 25 ? "PASS" : "FAIL")}: passed={passed}/25, naturalSurfaceAbovePlatform={abovePlatform}");
        Destroy(high); Destroy(low);
    }
    static void SaveScreen(string name)
    {
        var capture = ScreenCapture.CaptureScreenshotAsTexture();
        if (capture == null) { Debug.LogError("Desert sky isolation: screenshot unavailable for " + name); return; }
        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" + name + ".png", capture.EncodeToPNG());
        Destroy(capture);
    }
}
#endif
