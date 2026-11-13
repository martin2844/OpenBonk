#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

// Isolated setup; the real weapon must find and kill the spawned Wisp.
internal sealed class AndroidFoxWispAchievementProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_fox_wisp_achievement_probe", false)) return;
        var go = new GameObject("Fox Wisp achievement probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidFoxWispAchievementProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Fox Wisp probe: scene unavailable"); yield break; }
        var data = DataManager.Instance.GetEnemyData(EEnemy.Wisp);
        if (data == null) { Debug.LogError("Fox Wisp probe: Wisp data unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        MyStats.SetValueForce("foxWispsKills", 999f);
        var enemy = manager.SpawnEnemy(data, player.transform.position + player.transform.forward * 4f, -1, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Fox Wisp probe: spawn failed"); Restore(); yield break; }
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Fox Wisp probe: enemy not damageable"); Restore(); yield break; }
        RenderWispNoise(enemy);
        enemy.SetDiagnosticHealth(1f);
        bool died = false; Enemy.A_EnemyDied += OnDied;
        Debug.Log($"Fox Wisp probe: before character={player.character}, enemy={data.enemyName}, count={MyStats.GetStat("foxWispsKills")}, unlocked={SaveManager.Instance.progression.achievements.Contains("a_dragonsBreath")}, weapons={disabled.Count}");
        foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && !died) yield return new WaitForSecondsRealtime(.25f);
        yield return new WaitForSecondsRealtime(3f);
        Debug.Log($"Fox Wisp probe: after died={died}, count={MyStats.GetStat("foxWispsKills")}, unlocked={SaveManager.Instance.progression.achievements.Contains("a_dragonsBreath")}");
        Enemy.A_EnemyDied -= OnDied; Restore();
        void OnDied(Enemy dead, DamageContainer dc) { if (dead != enemy) return; died = true; Debug.Log($"Fox Wisp probe: death source={(dc != null ? dc.damageSource : "<null>")}"); }
        void Restore() { if (player != null) invincible.SetValue(player, oldInvincible); if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null && !weapon.enabled) weapon.Enable(); }
    }

    static void RenderWispNoise(Enemy enemy)
    {
        var material = enemy.renderer != null ? enemy.renderer.sharedMaterial : null;
        var filter = enemy.animatedMesh != null ? enemy.animatedMesh.GetComponent<MeshFilter>() : null;
        var mesh = filter != null ? filter.sharedMesh : null;
        if (material == null || mesh == null || !material.IsKeywordEnabled("_MK_VERTEX_ANIMATION_NOISE")) {
            Debug.LogError("Wisp noise render probe INCONCLUSIVE: animated material or mesh unavailable"); return;
        }
        var copy = new Material(material);
        var model = new GameObject("Wisp noise render mesh", typeof(MeshFilter), typeof(MeshRenderer));
        model.layer = 31; model.GetComponent<MeshFilter>().sharedMesh = mesh; model.GetComponent<MeshRenderer>().sharedMaterial = copy;
        var center = new Vector3(0, 10000, 0); model.transform.position = center - mesh.bounds.center;
        var cameraObject = new GameObject("Wisp noise render camera");
        var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(mesh.bounds.extents.magnitude * 1.4f, 0.1f);
        cameraObject.transform.position = center + Vector3.back * (camera.orthographicSize * 5f);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.cullingMask = 1 << 31;
        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        try {
            Color32[] Render(float value) {
                copy.SetFloat("_VertexAnimationIntensity", value); camera.Render();
                var old = RenderTexture.active; RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply(); RenderTexture.active = old;
                return image.GetPixels32();
            }
            var still = Render(0f); var animated = Render(material.GetFloat("_VertexAnimationIntensity")); var reset = Render(0f);
            int changed = 0, residual = 0;
            for (int i = 0; i < still.Length; i++) {
                int d = System.Math.Abs(still[i].r - animated[i].r) + System.Math.Abs(still[i].g - animated[i].g) + System.Math.Abs(still[i].b - animated[i].b);
                int r = System.Math.Abs(still[i].r - reset[i].r) + System.Math.Abs(still[i].g - reset[i].g) + System.Math.Abs(still[i].b - reset[i].b);
                if (d > 9) changed++; if (r > 9) residual++;
            }
            Debug.Log($"Wisp noise render probe {(changed > 100 && residual == 0 ? "PASS" : "FAIL")}: mesh={mesh.name}, vertices={mesh.vertexCount}, material={material.name}, intensity={material.GetFloat("_VertexAnimationIntensity"):F2}, changed={changed}, resetDifference={residual}");
        } finally {
            camera.targetTexture = null; Destroy(model); Destroy(cameraObject); Destroy(copy); Destroy(image); Destroy(target);
        }
    }
}
#endif
