#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

// Controlled schedule; real Desert map event, storm prefab and update loop.
internal sealed class AndroidDesertStormProbe : MonoBehaviour
{
    private static bool externalMap;
    private static bool shortStage;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        externalMap = intent.Call<bool>("getBooleanExtra", "openbonk_external_desert_storm_probe", false);
        shortStage = intent.Call<bool>("getBooleanExtra", "openbonk_external_short_desert_probe", false);
        externalMap |= shortStage;
        if (!externalMap && !intent.Call<bool>("getBooleanExtra", "megabonk_desert_storm_probe", false)) return;
        var go = new GameObject("Desert storm probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidDesertStormProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        MapEventsManager manager = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            manager = FindObjectOfType<MapEventsManager>();
            if (MatchesMap() && GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && manager != null && EffectManager.Instance != null && EnemyManager.Instance != null) break;
            yield return null;
        }
        if (manager == null || !MatchesMap()) { Debug.LogError("Desert storm probe: Desert scene unavailable"); yield break; }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var stormEvent = typeof(MapEventsManager).GetField("mapEvents", flags).GetValue(manager) as MapEventsDesert;
        if (stormEvent == null) { Debug.LogError("Desert storm probe: map event unavailable"); yield break; }
        var player = MyPlayer.Instance; var enemies = EnemyManager.Instance;
        bool waves = enemies.enabledWaves; enemies.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", flags); bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
        var times = (List<float>)typeof(MapEventsDesert).GetField("stormTimes", flags).GetValue(stormEvent);
        int scheduled = times.Count; float initialFog = RenderSettings.fogDensity;
        if (shortStage)
        {
            float next = (float)typeof(MapEventsDesert).GetField("nextStormTime", flags).GetValue(stormEvent);
            yield return new WaitForSecondsRealtime(3f);
            bool ok = scheduled == 0 && next == float.MaxValue && !MapEventsDesert.IsActiveStorm();
            Debug.Log($"Short desert storm probe {(ok ? "PASS" : "FAIL")}: scheduled={scheduled}, noStormScheduled={next == float.MaxValue}, active={MapEventsDesert.IsActiveStorm()}");
            Restore(); yield break;
        }
        typeof(MapEventsDesert).GetField("nextStormTime", flags).SetValue(stormEvent, MyTime.stageTimer + .1f);
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline && !MapEventsDesert.IsActiveStorm()) yield return null;
        var effect = EffectManager.Instance.GetDesertStorm();
        if (!MapEventsDesert.IsActiveStorm() || effect == null) { Debug.LogError("Desert storm probe: did not start"); Restore(); yield break; }
        yield return new WaitForSecondsRealtime(4f);
        int tornadoes = FindObjectsByType<Tornado>(FindObjectsSortMode.None).Length;
        int tumbleweeds = FindObjectsByType<InteractableTumbleWeed>(FindObjectsSortMode.None).Length;
        bool active = effect.gameObject.activeInHierarchy && effect.fogOfWarRenderer.enabled;
        int emitting = 0; foreach (var particle in effect.stormParticles) if (particle.emission.enabled) emitting++;
        float duringFog = RenderSettings.fogDensity;
        typeof(MapEventsDesert).GetField("stormOverAtTime", flags).SetValue(stormEvent, MyTime.stageTimer - 1f);
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline && (MapEventsDesert.IsActiveStorm() || effect.gameObject.activeInHierarchy)) yield return null;
        bool stopped = !MapEventsDesert.IsActiveStorm() && !effect.gameObject.activeInHierarchy;
        int stillEmitting = 0; foreach (var particle in effect.stormParticles) if (particle.emission.enabled) stillEmitting++;
        float finalFog = RenderSettings.fogDensity;
        bool pass = scheduled > 0 && active && emitting > 0 && stopped && stillEmitting == 0 && Mathf.Abs(finalFog - initialFog) < .001f && (!externalMap || (tornadoes > 0 && tumbleweeds > 0));
        Debug.Log($"Desert storm probe {(pass ? "PASS" : "FAIL")}: external={externalMap}, scheduled={scheduled}, active={active}, emitting={emitting}, stopped={stopped}, emittingAfter={stillEmitting}, tornadoes={tornadoes}, tumbleweeds={tumbleweeds}, fog={initialFog:F4}->{duringFog:F4}->{finalFog:F4}");
        Restore();
        void Restore() { body.constraints = constraints; invincible.SetValue(player, oldInvincible); enemies.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
    private static bool MatchesMap() => externalMap
        ? MapController.currentMap is OpenBonkExternalMapData external && external.enemyPoolMap == EMap.Desert
        : MapController.currentMap != null && MapController.currentMap.eMap == EMap.Desert;
}
#endif
