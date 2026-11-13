#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts._Data.Hats;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using System.IO;
using UnityEngine;

// Event-driven visual check; mutates only the disposable test run.
internal sealed class AndroidExternalShieldProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_external_shield_probe", false)) return;
        var go = new GameObject("External shield probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalShieldProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || MyPlayer.Instance.playerRenderer == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null || player.character != (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu")) { Debug.LogError("External shield probe INCONCLUSIVE: Pikachu not active"); yield break; }
        var actor = player.playerRenderer; var health = player.inventory.playerHealth;
        var parts = new List<Renderer>();
        foreach (var part in actor.animator.GetComponentsInChildren<Renderer>(true)) if (part is SkinnedMeshRenderer || part is MeshRenderer) parts.Add(part);
        bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
        float oldShield = health.shield; health.shield = 10f; PlayerHealth.A_Heal?.Invoke(health, 10f, true);
        actor.SetHat(DataManager.Instance.GetHat(EHat.Wizard));
        bool all = parts.Count > 1;
        foreach (var part in parts) { var outline = part.GetComponent<Outline>(); all &= outline != null && outline.enabled; }
        var hat = actor.transform.Find("PlayerHat");
        if (hat == null) foreach (var part in actor.GetComponentsInChildren<Transform>(true)) if (part.name == "PlayerHat") { hat = part; break; }
        var hatOutline = hat != null ? hat.GetComponent<Outline>() : null;
        bool hatActive = hatOutline != null && hatOutline.enabled && hat.gameObject.activeInHierarchy;
        Debug.Log("External shield probe CAPTURE READY");
        yield return new WaitForSecondsRealtime(4f);
        // Isolate each part and compare rendered frames against the same paused
        // scene without outlines. This reveals which mesh actually draws cyan.
        bool wasPaused = MyTime.paused; bool inventoryPaused = player.inventory.pause;
        player.inventory.pause = true; if (!wasPaused) MyTime.Pause();
        foreach (var part in parts) part.GetComponent<Outline>().enabled = false;
        if (hatOutline != null) hatOutline.enabled = false;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        var baselineImage = ScreenCapture.CaptureScreenshotAsTexture();
        var baseline = baselineImage.GetPixels32(); int width = baselineImage.width, height = baselineImage.height;
        string probeDir = Path.Combine(Application.persistentDataPath, "Probe"); Directory.CreateDirectory(probeDir);
        File.WriteAllBytes(Path.Combine(probeDir, "external-shield-baseline.png"), baselineImage.EncodeToPNG());
        Destroy(baselineImage);
        Vector3 center = Camera.main != null ? Camera.main.WorldToScreenPoint(actor.renderer.bounds.center) : new Vector3(width / 2f, height / 2f, 0f);
        int left = Mathf.Max(0, (int)center.x - 150), right = Mathf.Min(width, (int)center.x + 150);
        int bottom = Mathf.Max(0, (int)center.y - 150), top = Mathf.Min(height, (int)center.y + 150);
        foreach (var part in parts)
        {
            var partOutline = part.GetComponent<Outline>(); partOutline.enabled = true;
            yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); var pixels = image.GetPixels32();
            int cyan = 0;
            for (int y = bottom; y < top; y++) for (int x = left; x < right; x++)
            { int i = y * width + x; if (pixels[i].b - baseline[i].b > 10 && pixels[i].g - baseline[i].g > 3) cyan++; }
            var skin = part as SkinnedMeshRenderer; var mesh = skin != null ? skin.sharedMesh : null;
            Debug.Log($"External shield part: name={part.name} cyanPixels={cyan} readable={(mesh != null && mesh.isReadable)} submeshes={(mesh != null ? mesh.subMeshCount : 0)} materials={part.sharedMaterials.Length}");
            if (part == actor.renderer) File.WriteAllBytes(Path.Combine(probeDir, "external-shield-body.png"), image.EncodeToPNG());
            Destroy(image); partOutline.enabled = false;
        }
        foreach (var part in parts) part.GetComponent<Outline>().enabled = true;
        if (hatOutline != null) hatOutline.enabled = true;
        if (!wasPaused) MyTime.Unpause(); player.inventory.pause = inventoryPaused;
        PlayerHealth.A_TakeDamage?.Invoke(health, new DamageContainer(1f, "external-shield-probe"), true);
        health.shield = 0f; PlayerHealth.A_Heal?.Invoke(health, 0f, true);
        actor.ResetMaterial();
        bool removed = true;
        foreach (var part in parts)
        {
            var outline = part.GetComponent<Outline>(); removed &= outline != null && !outline.enabled;
            foreach (var material in part.sharedMaterials) if (material != null && material.name.StartsWith("Outline")) removed = false;
        }
        health.shield = 10f; PlayerHealth.A_Heal?.Invoke(health, 10f, true);
        actor.SetCharacter(DataManager.Instance.GetCharacterData(ECharacter.Fox), player.inventory, Vector3.zero);
        var foxOutline = actor.renderer != null ? actor.renderer.GetComponent<Outline>() : null;
        bool fox = foxOutline != null && foxOutline.enabled;
        health.shield = oldShield; PlayerHealth.A_Heal?.Invoke(health, 0f, true); EnemyManager.Instance.enabledWaves = waves;
        Debug.Log($"External shield probe {(all && hatActive && removed && fox ? "PASS" : "FAIL")}: parts={parts.Count} allOutlined={all} hat={hatActive} removedAfterFlash={removed} foxAfterSwitch={fox}");
        Destroy(gameObject);
    }
}
#endif
