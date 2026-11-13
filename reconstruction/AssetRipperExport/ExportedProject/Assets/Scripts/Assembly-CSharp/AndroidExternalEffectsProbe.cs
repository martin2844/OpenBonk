#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Intent-gated validation of actual external model materials and status effects.
internal sealed class AndroidExternalEffectsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_external_effects_probe", false)) return;
        var go = new GameObject("External effects probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalEffectsProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || MyPlayer.Instance.playerRenderer == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || player.playerRenderer == null || player.character != (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu"))
        { Debug.LogError("External effects probe INCONCLUSIVE: Pikachu not active"); Destroy(gameObject); yield break; }
        var actor = player.playerRenderer;
        var parts = new List<Renderer>();
        foreach (var part in actor.animator.GetComponentsInChildren<Renderer>(true)) if (part is SkinnedMeshRenderer || part is MeshRenderer) parts.Add(part);
        if (parts.Count < 2 || actor.damageFlash == null) { Debug.LogError("External effects probe INCONCLUSIVE: renderer or flash missing"); Destroy(gameObject); yield break; }
        bool waves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var originals = new List<Material[]>(); var baseline = new Dictionary<Material, Color>(); var colorProperty = new Dictionary<Material, string>();
        foreach (var part in parts)
        {
            var materials = part.sharedMaterials; originals.Add(materials);
            foreach (var material in materials)
            {
                if (material == null || material.HasProperty("_IridescenceColor")) continue;
                string name = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                if (name != null && !baseline.ContainsKey(material)) { baseline.Add(material, material.GetColor(name)); colorProperty.Add(material, name); }
            }
        }
        bool eventSeen = false; Action<PlayerHealth, DamageContainer, bool> observed = (health, dc, shield) => { if (health == player.inventory.playerHealth) eventSeen = true; };
        PlayerHealth.A_TakeDamage += observed;
        deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline && !player.inventory.playerHealth.CanTakeDamage()) yield return null;
        player.inventory.playerHealth.DamagePlayerExternal(1f, 0f, Vector3.zero, true, "external-effects-probe", DcFlags.BypassAll | DcFlags.IgnoreArmor);
        bool flashed = eventSeen;
        foreach (var part in parts) foreach (var material in part.sharedMaterials) if (material != actor.damageFlash) flashed = false;
        PlayerHealth.A_TakeDamage -= observed;
        yield return new WaitForSecondsRealtime(.6f);
        bool restored = true;
        for (int i = 0; i < parts.Count; i++)
        {
            var materials = parts[i].sharedMaterials;
            if (materials.Length != originals[i].Length) { restored = false; continue; }
            for (int j = 0; j < materials.Length; j++) if (materials[j] != originals[i][j]) restored = false;
        }
        bool inventoryWasPaused = player.inventory.pause;
        player.inventory.pause = true;
        MyTime.Pause();
        Debug.Log("External effects probe NORMAL CAPTURE READY");
        yield return new WaitForSecondsRealtime(3f);
        MyTime.Unpause();
        player.inventory.statusEffects.PoisonPlayer(10f);
        bool tinted = baseline.Count > 0;
        foreach (var pair in baseline)
        {
            Color expected = Color.Lerp(pair.Value, actor.poisonColor, .45f); expected.a = pair.Value.a;
            if ((pair.Key.GetColor(colorProperty[pair.Key]) - expected).maxColorComponent > .001f || (expected - pair.Key.GetColor(colorProperty[pair.Key])).maxColorComponent > .001f) tinted = false;
        }
        MyTime.Pause();
        Debug.Log("External effects probe POISON CAPTURE READY");
        yield return new WaitForSecondsRealtime(3f);
        MyTime.Unpause();
        player.inventory.statusEffects.RemoveAllStatusEffects();
        player.inventory.pause = inventoryWasPaused;
        bool tintRestored = true;
        foreach (var pair in baseline)
            if ((pair.Key.GetColor(colorProperty[pair.Key]) - pair.Value).maxColorComponent > .001f || (pair.Value - pair.Key.GetColor(colorProperty[pair.Key])).maxColorComponent > .001f) tintRestored = false;
        PlayerHealth.A_TakeDamage?.Invoke(player.inventory.playerHealth, new DamageContainer(1f, "external-switch-probe"), false);
        actor.SetCharacter(DataManager.Instance.GetCharacterData(ECharacter.Fox), player.inventory, Vector3.zero);
        yield return null;
        bool released = true;
        foreach (var materials in originals) foreach (var material in materials) if (material != null) released = false;
        bool switched = actor.renderer != null && actor.animator != null && actor.animator.runtimeAnimatorController != null;
        bool pass = flashed && restored && tinted && tintRestored && released && switched;
        Debug.Log($"External effects probe {(pass ? "PASS" : "FAIL")}: renderers={parts.Count} fallbackMaterials={baseline.Count} damageEvent={eventSeen} flashAll={flashed} restoredAll={restored} poisonTint={tinted} tintRestored={tintRestored} released={released} switchedToFox={switched}");
        foreach (var weapon in disabled) weapon.Enable(); EnemyManager.Instance.enabledWaves = waves;
        Destroy(gameObject);
    }
}
#endif
