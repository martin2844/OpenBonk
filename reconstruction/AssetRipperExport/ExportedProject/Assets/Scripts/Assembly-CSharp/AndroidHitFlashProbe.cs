#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in material check; the separate live flag applies a small, controlled enemy hit.
internal sealed class AndroidHitFlashProbe : MonoBehaviour
{
    private static bool liveProbe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_hit_flash_probe", false)) return;
        liveProbe = intent.Call<bool>("getBooleanExtra", "megabonk_enemy_flash_live_probe", false);
        var go = new GameObject("Hit flash pixel probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidHitFlashProbe>();
    }
    private IEnumerator Start()
    {
        Enemy enemy = null; float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline)
        {
            foreach (var candidate in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.whiteMaterial != null && (!liveProbe || (candidate.CanTakeDamage() && candidate.hp > 1 && candidate.renderer != null))) { enemy = candidate; break; }
            if (enemy != null) break;
            yield return null;
        }
        if (enemy == null) { Debug.LogError("Hit flash probe: enemy timeout"); yield break; }
        var original = enemy.whiteMaterial;
        var camGo = new GameObject("Hit flash test camera"); camGo.transform.position = new Vector3(0, 1000, 0);
        var camera = camGo.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 31; camera.nearClipPlane = .1f; camera.farClipPlane = 30; camera.depth = 100;
        var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.layer = 31; obj.transform.position = new Vector3(0, 1000, 5); obj.transform.localScale = Vector3.one * 2;
        var renderer = obj.GetComponent<Renderer>(); var material = new Material(original); renderer.sharedMaterial = material;
        for (int pass = 0; pass < 2; pass++)
        {
            material.enableInstancing = pass == 1;
            for (int frame = 0; frame < 3; frame++) yield return new WaitForEndOfFrame();
            var old = RenderTexture.active; RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply(); RenderTexture.active = old;
            int white = 0; foreach (Color32 p in image.GetPixels32()) if (p.r > 240 && p.g > 240 && p.b > 240) white++;
            Debug.Log($"Hit flash probe: asset={original.name}, assetInstancing={original.enableInstancing}, testInstancing={material.enableInstancing}, whitePixels={white}, shaderSupported={material.shader.isSupported}");
        }
        camera.targetTexture = null; Destroy(camGo); Destroy(obj); target.Release(); Destroy(target); Destroy(image); Destroy(material);
        if (!liveProbe) yield break;
        var visualSettings = SaveManager.Instance != null && SaveManager.Instance.config != null ? SaveManager.Instance.config.cfVisualsSettings : null;
        var player = MyPlayer.Instance;
        if (visualSettings == null || player == null || player.inventory == null || player.inventory.weaponInventory == null)
        { Debug.LogError("Enemy flash live probe: player or settings unavailable"); yield break; }
        int previousSetting = visualSettings.damage_flash; visualSettings.damage_flash = 1;
        var weapons = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon != null && weapon.enabled) { weapon.Disable(); weapons.Add(weapon); }
        var readyField = typeof(Enemy).GetField("readyToFlashTime", BindingFlags.NonPublic | BindingFlags.Instance);
        float until = Time.realtimeSinceStartup + 5f;
        while (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CanTakeDamage() && MyTime.time <= (float)readyField.GetValue(enemy) && Time.realtimeSinceStartup < until) yield return null;
        if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.CanTakeDamage() || enemy.hp <= 1)
            Debug.LogError("Enemy flash live probe: target unavailable");
        else
        {
            float before = enemy.hp; var expectedMaterial = enemy.enemyData.material;
            enemy.DamageExternal(new DamageContainer(0, "HitFlashProbe") { enemy = enemy, damage = .1f });
            bool damaged = enemy.hp < before, flashed = enemy.renderer.sharedMaterial == original;
            until = Time.realtimeSinceStartup + 3f;
            while (enemy != null && enemy.gameObject.activeInHierarchy && enemy.renderer.sharedMaterial == original && Time.realtimeSinceStartup < until) yield return null;
            bool restored = enemy != null && enemy.gameObject.activeInHierarchy && enemy.renderer.sharedMaterial == expectedMaterial;
            bool pass = damaged && flashed && restored;
            Debug.Log($"Enemy flash live probe {(pass ? "PASS" : "FAIL")}: enemy={(enemy != null ? enemy.name : "missing")}, before={before:F2}, after={(enemy != null ? enemy.hp : 0):F2}, damaged={damaged}, flashed={flashed}, restored={restored}");
        }
        visualSettings.damage_flash = previousSetting;
        foreach (var weapon in weapons) if (weapon != null) weapon.Enable();
    }
}
#endif
