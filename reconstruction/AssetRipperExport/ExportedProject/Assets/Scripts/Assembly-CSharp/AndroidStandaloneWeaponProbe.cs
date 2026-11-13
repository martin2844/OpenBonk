#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

internal sealed class AndroidStandaloneWeaponProbe : MonoBehaviour
{
    private string source; private int hits; private float minDamage = float.MaxValue, maxDamage; private bool fireball; private bool overlap, vanillaSpawn; private int vanillaHits;
    private EWeapon vanillaWeapon = EWeapon.FireStaff;
    private readonly List<ProjectileBase> activeShots = new List<ProjectileBase>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_standalone_weapon_probe", false)) return;
        var go = new GameObject("Standalone weapon probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidStandaloneWeaponProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EWeapon)OpenBonkContentIds.Weapon("openbonk-standalone-test", "spark");
        WeaponData data;
        try { data = DataManager.Instance.GetWeapon(id); } catch { Debug.LogError("Standalone weapon INCONCLUSIVE: weapon missing"); yield break; }
        if (player == null || player.inventory == null || manager == null || !(data is OpenBonkLuaWeaponData)) { Debug.LogError("Standalone weapon INCONCLUSIVE: player or weapon unavailable"); yield break; }
        string effect = ((OpenBonkLuaWeaponData)data).effect;
        fireball = effect == "fireball" || effect == "arrow";
        vanillaWeapon = effect == "arrow" ? EWeapon.Bow : EWeapon.FireStaff;
        bool pool = UnlockUtility.GetAvailableWeapons().Contains(data), icon = data.icon != null && data.icon.width > 0;
        if (fireball) { data.projectileSpeed = .15f; data.attackDuration = 5f; data.maxDuration = 5f; data.burstTime = .1f; data.endCooldown = .05f; data.Init(); }
        manager.enabledWaves = false; foreach (var w in player.inventory.weaponInventory.weapons.Values) w.Disable();
        var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * (fireball ? 12f : 5f), -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && (target == null || !target.CanTakeDamage())) yield return null;
        if (target == null || !target.CanTakeDamage()) { Debug.LogError("Standalone weapon INCONCLUSIVE: target unavailable"); yield break; }
        target.SetDiagnosticHealth(10000f); target.enemyMovement.StopMovement(); var rb = target.GetComponent<Rigidbody>(); if (rb != null) rb.constraints = RigidbodyConstraints.FreezeAll;
        source = data.damageSourceName; Enemy.A_Damage += OnDamage; WeaponAttack.A_SpawnedProjectileSuccessfully += OnProjectile;
        var inventory = player.inventory.weaponInventory; inventory.AddWeapon(data, null);
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && (hits < 2 || (fireball && (!overlap || minDamage == float.MaxValue || maxDamage < minDamage * 1.9f)))) yield return null;
        var damageSource = new DamageSource(source, 0f);
        bool display = damageSource.GetIcon() == data.icon && damageSource.GetLevel() == 1 && Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedDamageSource(source) == data.GetName();
        bool damage = !fireball || (minDamage > 0 && maxDamage > minDamage * 1.9f && maxDamage < minDamage * 2.1f);
        if (fireball)
        {
            inventory.weapons[id].Disable();
            if (!inventory.weapons.ContainsKey(vanillaWeapon)) inventory.AddWeapon(DataManager.Instance.GetWeapon(vanillaWeapon), null);
            if (inventory.weapons.TryGetValue(vanillaWeapon, out var original)) original.Enable();
            deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline && (vanillaHits == 0 || !vanillaSpawn)) yield return null;
        }
        bool vanilla = !fireball || (vanillaSpawn && vanillaHits > 0);
        bool pass = pool && icon && hits >= 2 && display && damage && (!fireball || overlap) && vanilla;
        Debug.Log($"Standalone weapon {(pass ? "PASS" : "FAIL")}: pool={pool} icon={icon} hits={hits} display={display} fireball={fireball} damage={damage} min={minDamage:F2} max={maxDamage:F2} overlap={overlap} vanillaSpawn={vanillaSpawn} vanillaHits={vanillaHits}");
        Enemy.A_Damage -= OnDamage; WeaponAttack.A_SpawnedProjectileSuccessfully -= OnProjectile;
    }
    void OnDamage(Enemy enemy, DamageContainer dc) { if (enemy != null && dc != null && dc.damageSource == vanillaWeapon.ToString()) vanillaHits++; if (enemy != null && dc != null && dc.damageSource == source) { hits++; if (!dc.crit) { minDamage = Mathf.Min(minDamage, dc.damage); maxDamage = Mathf.Max(maxDamage, dc.damage); } } }
    void OnProjectile(ProjectileBase shot)
    {
        if (shot == null || shot.weaponBase == null) return;
        if (shot.weaponBase.weaponData.eWeapon == vanillaWeapon) { vanillaSpawn |= Mathf.Approximately(shot.damageMultiplier, 1f); return; }
        if (shot.weaponBase.weaponData.damageSourceName != source) return;
        activeShots.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);
        foreach (var other in activeShots) if (!Mathf.Approximately(other.damageMultiplier, shot.damageMultiplier)) overlap = true;
        activeShots.Add(shot);
    }
    void OnDestroy() { Enemy.A_Damage -= OnDamage; WeaponAttack.A_SpawnedProjectileSuccessfully -= OnProjectile; }
}
#endif
