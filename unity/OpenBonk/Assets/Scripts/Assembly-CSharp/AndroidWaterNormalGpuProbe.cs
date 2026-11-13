#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.IO;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Isolated, opt-in sample of the actual water texture through Android's UnpackNormal.
internal sealed class AndroidWaterNormalGpuProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "openbonk_water_normal_gpu_probe", false)) return;
        var root = new GameObject("water normal GPU probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidWaterNormalGpuProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        Material water = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
                if (material != null && material.name == "WaterForest" && material.HasProperty("_Normal")) { water = material; break; }
            if (water != null && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted) break;
            yield return null;
        }
        var normal = water != null ? water.GetTexture("_Normal") : null;
        var shader = Resources.Load<Shader>("OpenBonkWaterNormalProbe");
        if (normal == null || shader == null || !shader.isSupported) { Debug.LogError($"Water normal GPU probe INCONCLUSIVE: water={water != null} texture={normal != null} shader={shader != null}"); yield break; }
        var oldFilter = normal.filterMode;
        var oldActive = RenderTexture.active;
        var materialProbe = new Material(shader);
        var rt = RenderTexture.GetTemporary(normal.width, normal.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var image = new Texture2D(normal.width, normal.height, TextureFormat.RGBA32, false, true);
        try
        {
            normal.filterMode = FilterMode.Point;
            Graphics.Blit(normal, rt, materialProbe);
            RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
            string path = Path.Combine(Application.persistentDataPath, "water-normal-gpu-probe.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log($"Water normal GPU probe CAPTURE: size={normal.width}x{normal.height} texture={normal.name} path={path} graphics={SystemInfo.graphicsDeviceType}");
            CheckSmoothness(water);
        }
        finally
        {
            normal.filterMode = oldFilter; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(rt);
            Destroy(image); Destroy(materialProbe);
        }
        // Map generation can still be running when the player starts; wait
        // until the scene has actually enabled its water surface.
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline)
        {
            var surface = FindFirstObjectByType<Water>();
            if (surface != null && surface.waterUpper != null && surface.waterUpper.enabled && surface.waterUpper.gameObject.activeInHierarchy) break;
            yield return null;
        }
        CheckSceneWater(water);
        CheckShorelineView();
        yield return CheckFogDistance();
    }

    static IEnumerator CheckFogDistance()
    {
        var water = FindFirstObjectByType<Water>(); var surface = water != null ? water.waterUpper : null;
        if (surface == null || !surface.enabled || surface.sharedMaterial == null) { Debug.LogError("Water fog distance INCONCLUSIVE: no surface"); yield break; }
        var controlShader = Resources.Load<Shader>("OpenBonkWaterFogControl");
        if (controlShader == null || !controlShader.isSupported) { Debug.LogError("Water fog distance INCONCLUSIVE: no control shader"); yield break; }
        var previousMaterial = surface.sharedMaterial; var controlMaterial = new Material(controlShader);
        int oldLayer = surface.gameObject.layer; bool oldFog = RenderSettings.fog;
        var oldMode = RenderSettings.fogMode; var oldColor = RenderSettings.fogColor; float oldDensity = RenderSettings.fogDensity;
        var oldActive = RenderTexture.active; var root = new GameObject("water fog distance probe"); var camera = root.AddComponent<Camera>();
        var target = RenderTexture.GetTemporary(64, 64, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        try
        {
            surface.gameObject.layer = 31; camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.orthographic = true; camera.orthographicSize = 4f; camera.nearClipPlane = .1f; camera.farClipPlane = 1000f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.targetTexture = target;
            camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .01f; RenderSettings.fogColor = Color.red;
            var samples = new Color32[8];
            for (int i = 0; i < samples.Length; i++)
            {
                surface.sharedMaterial = i < 4 ? previousMaterial : controlMaterial;
                camera.transform.position = surface.bounds.center + Vector3.up * ((i % 4 < 2) ? 20f : 250f);
                RenderSettings.fog = (i % 2) == 1;
                yield return null;
                Debug.Log($"Water fog distance state: sample={i} fog={RenderSettings.fog} mode={RenderSettings.fogMode} density={RenderSettings.fogDensity} color={RenderSettings.fogColor} exp2Keyword={Shader.IsKeywordEnabled("FOG_EXP2")}");
                camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); image.Apply();
                samples[i] = image.GetPixel(32, 32);
            }
            Debug.Log($"Water fog distance samples: nearClear={samples[0]} nearFog={samples[1]} farClear={samples[2]} farFog={samples[3]}");
            Debug.Log($"Water fog distance control: nearClear={samples[4]} nearFog={samples[5]} farClear={samples[6]} farFog={samples[7]}");
        }
        finally
        {
            surface.sharedMaterial = previousMaterial; surface.gameObject.layer = oldLayer; RenderSettings.fog = oldFog; RenderSettings.fogMode = oldMode;
            RenderSettings.fogColor = oldColor; RenderSettings.fogDensity = oldDensity; RenderTexture.active = oldActive;
            camera.targetTexture = null; RenderTexture.ReleaseTemporary(target); Destroy(image); Destroy(root); Destroy(controlMaterial);
        }
    }

    static void CheckShorelineView()
    {
        var water = FindFirstObjectByType<Water>(); var renderer = water != null ? water.waterUpper : null;
        var cam = Camera.main; var game = GameManager.Instance;
        var limitedShader = Resources.Load<Shader>("OpenBonkWaterClampedProbe");
        var maskShader = Resources.Load<Shader>("OpenBonkWaterMaskProbe");
        if (renderer == null || renderer.sharedMaterial == null || cam == null || game == null || limitedShader == null || !limitedShader.isSupported || maskShader == null || !maskShader.isSupported)
        { Debug.LogError("Water shoreline probe INCONCLUSIVE: required scene objects unavailable"); return; }
        float surfaceY = renderer.bounds.center.y; Vector3 shore = default, towardWater = default; bool found = false;
        int groundSamples = 0, aboveSamples = 0, belowSamples = 0; float minGround = float.PositiveInfinity, maxGround = float.NegativeInfinity;
        Vector3[] offsets = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        for (int x = -470; x <= 470 && !found; x += 10)
        for (int z = -470; z <= 470 && !found; z += 10)
        {
            if (!Physics.Raycast(new Vector3(x, 300f, z), Vector3.down, out var land, 600f, game.whatIsGround)) continue;
            groundSamples++; minGround = Mathf.Min(minGround, land.point.y); maxGround = Mathf.Max(maxGround, land.point.y);
            if (land.point.y <= surfaceY) belowSamples++; else aboveSamples++;
            if (land.point.y < surfaceY + .1f || land.point.y > surfaceY + 12f) continue;
            foreach (var direction in offsets)
            {
                Vector3 next = new Vector3(x, 300f, z) + direction * 25f;
                if (Physics.Raycast(next, Vector3.down, out var underwater, 600f, game.whatIsGround) && underwater.point.y < surfaceY)
                { shore = land.point; towardWater = direction; found = true; break; }
            }
        }
        if (!found) { Debug.LogError($"Water shoreline probe INCONCLUSIVE: no transition surface={surfaceY:F2} ground={groundSamples} below={belowSamples} above={aboveSamples} min={minGround:F2} max={maxGround:F2} mask={game.whatIsGround.value}"); return; }
        var oldMaterial = renderer.sharedMaterial; var limited = new Material(oldMaterial); limited.shader = limitedShader;
        var maskMaterial = new Material(maskShader);
        var oldPosition = cam.transform.position; var oldRotation = cam.transform.rotation; var oldTarget = cam.targetTexture;
        var oldActive = RenderTexture.active; var oldDepth = cam.depthTextureMode; bool oldFog = RenderSettings.fog;
        var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        try
        {
            cam.transform.position = shore - towardWater * 6f + Vector3.up * 8f;
            cam.transform.LookAt(new Vector3(shore.x, surfaceY, shore.z) + towardWater * 18f);
            cam.targetTexture = target; cam.depthTextureMode |= DepthTextureMode.Depth;
            Color32[] Capture(Material material, string filename)
            {
                renderer.sharedMaterial = material; cam.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Application.persistentDataPath, filename), image.EncodeToPNG()); return image.GetPixels32();
            }
            var oldPixels = Capture(limited, "water-shoreline-limited.png");
            var newPixels = Capture(oldMaterial, "water-shoreline-full.png"); int changed = 0;
            Debug.Log($"Water shoreline settings: shader={oldMaterial.shader.name} supported={oldMaterial.shader.isSupported} fog={oldFog} fogColor={RenderSettings.fogColor} fogMode={RenderSettings.fogMode} fogStart={RenderSettings.fogStartDistance} fogEnd={RenderSettings.fogEndDistance} fogDensity={RenderSettings.fogDensity} bottom={oldMaterial.GetColor("_BottomColor")} mid={oldMaterial.GetColor("_MidColor")} top={oldMaterial.GetColor("_TopColor")} ambient={RenderSettings.ambientLight}");
            RenderSettings.fog = false;
            var noFogPixels = Capture(oldMaterial, "water-shoreline-nofog.png");
            RenderSettings.fog = oldFog;
            var maskPixels = Capture(maskMaterial, "water-shoreline-objectmask.png");
            int maskCount = 0, maskChanged = 0, fogChanged = 0; long red = 0, green = 0, blue = 0, noFogRed = 0, noFogGreen = 0, noFogBlue = 0;
            for (int i = 0; i < maskPixels.Length; i++)
            {
                var p = maskPixels[i];
                if (p.r < 245 || p.g > 10 || p.b < 245) continue;
                maskCount++; red += newPixels[i].r; green += newPixels[i].g; blue += newPixels[i].b;
                noFogRed += noFogPixels[i].r; noFogGreen += noFogPixels[i].g; noFogBlue += noFogPixels[i].b;
                if (System.Math.Abs(noFogPixels[i].r-newPixels[i].r) + System.Math.Abs(noFogPixels[i].g-newPixels[i].g) + System.Math.Abs(noFogPixels[i].b-newPixels[i].b) > 3) fogChanged++;
                if (System.Math.Abs(oldPixels[i].r-newPixels[i].r) + System.Math.Abs(oldPixels[i].g-newPixels[i].g) + System.Math.Abs(oldPixels[i].b-newPixels[i].b) > 3) maskChanged++;
            }
            Debug.Log($"Water shoreline object mask: count={maskCount} changed={maskChanged} rgbSum={red},{green},{blue} fogChanged={fogChanged} noFogRgbSum={noFogRed},{noFogGreen},{noFogBlue}");
            for (int i = 0; i < oldPixels.Length; i++)
                if (System.Math.Abs(oldPixels[i].r-newPixels[i].r) + System.Math.Abs(oldPixels[i].g-newPixels[i].g) + System.Math.Abs(oldPixels[i].b-newPixels[i].b) > 3) changed++;
            bool oldEnabled = renderer.enabled; Color32[] noWater;
            try { renderer.enabled = false; noWater = Capture(oldMaterial, "water-shoreline-hidden.png"); }
            finally { renderer.enabled = oldEnabled; }
            int waterPixels = 0;
            for (int i = 0; i < newPixels.Length; i++)
                if (System.Math.Abs(newPixels[i].r-noWater[i].r) + System.Math.Abs(newPixels[i].g-noWater[i].g) + System.Math.Abs(newPixels[i].b-noWater[i].b) > 3) waterPixels++;
            Debug.Log($"Water shoreline probe {(waterPixels >= 100 ? "CAPTURE" : "INCONCLUSIVE")}: material={oldMaterial.name} smoothness={oldMaterial.GetFloat("_Smoothness")} shore={shore} direction={towardWater} changed={changed} waterPixels={waterPixels} pixels={oldPixels.Length}");
        }
        finally
        {
            renderer.sharedMaterial = oldMaterial; cam.transform.SetPositionAndRotation(oldPosition, oldRotation);
            RenderSettings.fog = oldFog;
            cam.targetTexture = oldTarget; cam.depthTextureMode = oldDepth; RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target); Destroy(image); Destroy(limited); Destroy(maskMaterial);
        }
    }

    static void CheckSceneWater(Material forest)
    {
        var water = FindFirstObjectByType<Water>(); var renderer = water != null ? water.waterUpper : null;
        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sharedMaterial == null)
        { Debug.LogError("Water scene probe INCONCLUSIVE: active upper water renderer unavailable"); return; }
        var original = renderer.sharedMaterial; var clone = new Material(original); var forestClone = new Material(forest); int oldLayer = renderer.gameObject.layer;
        Material rawClone = null, brdfClone = null;
        var root = new GameObject("water scene camera"); var camera = root.AddComponent<Camera>(); camera.enabled = false;
        camera.cullingMask = 1 << 31; camera.orthographic = true; camera.orthographicSize = 8f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 200f; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta; camera.depthTextureMode = DepthTextureMode.Depth;
        var center = renderer.bounds.center; camera.transform.position = center + new Vector3(0f, 12f, -12f);
        camera.transform.LookAt(center);
        var target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(256, 256, TextureFormat.RGBA32, false); var previous = RenderTexture.active;
        try
        {
            renderer.gameObject.layer = 31; renderer.sharedMaterial = clone;
            Color32[] Capture(float value, string name)
            {
                clone.SetFloat("_Smoothness", value); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name), image.EncodeToPNG());
                return image.GetPixels32();
            }
            var limited = Capture(1f, "water-scene-smoothness-1.png");
            var originalValue = original.GetFloat("_Smoothness");
            var recovered = Capture(originalValue, "water-scene-smoothness-original.png");
            int changed = 0, visible = 0;
            for (int i = 0; i < limited.Length; i++)
            {
                int d = System.Math.Abs(limited[i].r - recovered[i].r) + System.Math.Abs(limited[i].g - recovered[i].g) + System.Math.Abs(limited[i].b - recovered[i].b);
                if (d > 3) changed++;
                if (recovered[i].g > 8 || recovered[i].r < 245 || recovered[i].b < 245) visible++;
            }
            Debug.Log($"Water scene probe {(visible > 100 ? "CAPTURE" : "INCONCLUSIVE")}: renderer={renderer.name} material={original.name} smoothness={originalValue} visible={visible} changed={changed} center={center} bounds={renderer.bounds.size}");
            renderer.sharedMaterial = forestClone;
            Color32[] CaptureForest(float value, string name)
            {
                forestClone.SetFloat("_Smoothness", value); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name), image.EncodeToPNG()); return image.GetPixels32();
            }
            var forestLimited = CaptureForest(1f, "water-scene-forest-smoothness-1.png");
            var forestOriginal = CaptureForest(forest.GetFloat("_Smoothness"), "water-scene-forest-smoothness-original.png");
            int forestChanged = 0;
            for (int i = 0; i < forestLimited.Length; i++)
                if (System.Math.Abs(forestLimited[i].r - forestOriginal[i].r) + System.Math.Abs(forestLimited[i].g - forestOriginal[i].g) + System.Math.Abs(forestLimited[i].b - forestOriginal[i].b) > 3) forestChanged++;
            Debug.Log($"Water scene Forest material probe: smoothness={forest.GetFloat("_Smoothness")} changed={forestChanged} desertSurface=true");
            var rawShader = Resources.Load<Shader>("OpenBonkWaterRawProbe");
            var brdfShader = Resources.Load<Shader>("OpenBonkWaterBrdf1Probe");
            if (rawShader != null && brdfShader != null && rawShader.isSupported && brdfShader.isSupported)
            {
                rawClone = new Material(forest); rawClone.shader = rawShader;
                brdfClone = new Material(forest); brdfClone.shader = brdfShader;
                Color32[] CaptureVariant(Material material, string name)
                {
                    renderer.sharedMaterial = material; camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name), image.EncodeToPNG()); return image.GetPixels32();
                }
                var rawPixels = CaptureVariant(rawClone, "water-scene-raw.png");
                var brdfPixels = CaptureVariant(brdfClone, "water-scene-brdf1.png");
                int rawWhite = 0, brdfWhite = 0, variantChanged = 0;
                for (int i = 0; i < rawPixels.Length; i++)
                {
                    if (rawPixels[i].r >= 250 && rawPixels[i].g >= 250 && rawPixels[i].b >= 250) rawWhite++;
                    if (brdfPixels[i].r >= 250 && brdfPixels[i].g >= 250 && brdfPixels[i].b >= 250) brdfWhite++;
                    if (System.Math.Abs(rawPixels[i].r-brdfPixels[i].r) + System.Math.Abs(rawPixels[i].g-brdfPixels[i].g) + System.Math.Abs(rawPixels[i].b-brdfPixels[i].b) > 3) variantChanged++;
                }
                Debug.Log($"Water BRDF variant probe: rawWhite={rawWhite} brdf1White={brdfWhite} changed={variantChanged} pixels={rawPixels.Length}");
            }
            else Debug.LogError($"Water BRDF variant probe INCONCLUSIVE: raw={rawShader != null} brdf1={brdfShader != null}");
            foreach (string lavaName in new[] { "Lava", "LavaCrypt" })
            {
                Material lava = null;
                foreach (var candidate in Resources.FindObjectsOfTypeAll<Material>()) if (candidate != null && candidate.name == lavaName) { lava = candidate; break; }
                if (lava == null) { Debug.Log($"Water lava material probe INCONCLUSIVE: material={lavaName} unavailable"); continue; }
                var lavaClone = new Material(lava);
                try
                {
                    renderer.sharedMaterial = lavaClone; camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "water-scene-" + lavaName + ".png"), image.EncodeToPNG());
                    var pixels = image.GetPixels32(); int white = 0, magenta = 0;
                    foreach (var p in pixels) { if (p.r >= 250 && p.g >= 250 && p.b >= 250) white++; if (p.r >= 245 && p.g <= 8 && p.b >= 245) magenta++; }
                    Debug.Log($"Water lava material probe: material={lavaName} smoothness={lava.GetFloat("_Smoothness")} white={white} magenta={magenta} pixels={pixels.Length}");
                }
                finally { Destroy(lavaClone); }
            }
            var limitedShader = Resources.Load<Shader>("OpenBonkWaterClampedProbe");
            if (limitedShader == null || !limitedShader.isSupported)
                Debug.LogError("Water wide comparison INCONCLUSIVE: old shader unavailable");
            else
            {
                var limitedClone = new Material(forest); limitedClone.shader = limitedShader;
                var wideTarget = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
                var wideImage = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
                try
                {
                    camera.targetTexture = wideTarget; camera.aspect = 1280f / 720f;
                    Color32[] CaptureWide(Material material, string name)
                    {
                        renderer.sharedMaterial = material; camera.Render(); RenderTexture.active = wideTarget;
                        wideImage.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); wideImage.Apply();
                        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, name), wideImage.EncodeToPNG()); return wideImage.GetPixels32();
                    }
                    var before = CaptureWide(limitedClone, "water-wide-limited.png");
                    var after = CaptureWide(forestClone, "water-wide-full.png"); int wideChanged = 0, white = 0;
                    for (int i = 0; i < before.Length; i++)
                    {
                        if (System.Math.Abs(before[i].r-after[i].r) + System.Math.Abs(before[i].g-after[i].g) + System.Math.Abs(before[i].b-after[i].b) > 3) wideChanged++;
                        if (after[i].r >= 250 && after[i].g >= 250 && after[i].b >= 250) white++;
                    }
                    Debug.Log($"Water wide comparison: size=1280x720 changed={wideChanged} fullWhite={white} pixels={before.Length}");
                }
                finally
                {
                    camera.targetTexture = target; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(wideTarget);
                    Destroy(wideImage); Destroy(limitedClone);
                }
            }
        }
        finally
        {
            renderer.sharedMaterial = original; renderer.gameObject.layer = oldLayer; camera.targetTexture = null;
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(image); Destroy(clone); Destroy(forestClone);
            if (rawClone != null) Destroy(rawClone); if (brdfClone != null) Destroy(brdfClone); Destroy(root);
        }
    }

    static void CheckSmoothness(Material original)
    {
        var sceneLights = FindObjectsOfType<Light>(); var wasEnabled = new bool[sceneLights.Length];
        for (int i = 0; i < sceneLights.Length; i++) { wasEnabled[i] = sceneLights[i].enabled; sceneLights[i].enabled = false; }
        var oldAmbientMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.black;
        var clone = new Material(original); clone.SetFloat("_NormalStrenght", 0f);
        clone.SetColor("_BottomColor", Color.white); clone.SetColor("_MidColor", Color.white);
        clone.SetColor("_TopColor", Color.black); clone.SetColor("_FoamColor", Color.black);
        var plane = GameObject.CreatePrimitive(PrimitiveType.Quad); plane.layer = 31;
        plane.transform.position = new Vector3(0f, 10000f, 0f); plane.GetComponent<Renderer>().sharedMaterial = clone;
        var cameraObject = new GameObject("water smoothness camera"); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
        cameraObject.transform.position = plane.transform.position + new Vector3(0f, 0f, -4f); camera.cullingMask = 1 << 31;
        camera.orthographic = true; camera.orthographicSize = .7f; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black; camera.depthTextureMode = DepthTextureMode.Depth;
        var lightObject = new GameObject("water smoothness light"); var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional; light.cullingMask = 1 << 31; light.intensity = .35f; lightObject.transform.rotation = Quaternion.Euler(35f, 25f, 0f);
        var target = RenderTexture.GetTemporary(128, 128, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(128, 128, TextureFormat.RGBA32, false); var previous = RenderTexture.active;
        try
        {
            Color32[] Capture(float value) { clone.SetFloat("_Smoothness", value); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply(); return image.GetPixels32(); }
            var one = Capture(1f); var ten = Capture(10f); int changed = 0, visible = 0; long delta = 0;
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "water-smoothness-gpu-ten.png"), image.EncodeToPNG());
            for (int i = 0; i < one.Length; i++)
            {
                int d = System.Math.Abs(one[i].r - ten[i].r) + System.Math.Abs(one[i].g - ten[i].g) + System.Math.Abs(one[i].b - ten[i].b);
                if (d > 3) changed++; if (one[i].r + one[i].g + one[i].b > 9) visible++; delta += d;
            }
            Debug.Log($"Water smoothness GPU probe {(visible >= 100 && changed >= 100 ? "PASS" : "INCONCLUSIVE")}: material={original.name} original={original.GetFloat("_Smoothness")} changed={changed} visible={visible} center={one[64*128+64]} rgbDelta={delta}");
        }
        finally
        {
            RenderSettings.ambientMode = oldAmbientMode; RenderSettings.ambientLight = oldAmbient;
            for (int i = 0; i < sceneLights.Length; i++) if (sceneLights[i] != null) sceneLights[i].enabled = wasEnabled[i];
            camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            Destroy(image); Destroy(clone); Destroy(plane); Destroy(cameraObject); Destroy(lightObject);
        }
    }
}
#endif
