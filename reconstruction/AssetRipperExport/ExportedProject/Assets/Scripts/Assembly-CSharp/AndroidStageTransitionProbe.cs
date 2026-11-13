#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in on a disposable Android run. Forces a stage transition after ordinary startup.
internal sealed class AndroidStageTransitionProbe : MonoBehaviour
{
    static bool installed;
    static bool usePortal;
    static bool beaconProbe;
    static bool pumpkinProbe;
    static bool statusProbe;
    static bool loadoutProbe;
    static bool portalXpProbe;
    static bool luaRefreshProbe;
    static bool mapLuaProbe;
    int portalXpOpened, portalXpClosed, portalXpTargetLevel;
    int hasteRemoved, shieldRemoved;
    int stageFirestaffHits;
    float stageFirestaffDamage;
    bool sawScene;
    int transitionStarts, mapStarts, transitionEnds;
    PlayerInventory trackedInventory;
    int hpAtSceneLoad = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        usePortal = intent.Call<bool>("getBooleanExtra", "megabonk_stage_portal_probe", false);
        beaconProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stage_beacon_probe", false);
        pumpkinProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stage_pumpkin_probe", false);
        statusProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stage_status_probe", false);
        loadoutProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stage_loadout_probe", false);
        portalXpProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stage_portal_xp_probe", false);
        luaRefreshProbe = intent.Call<bool>("getBooleanExtra", "megabonk_stage_lua_refresh_probe", false);
        mapLuaProbe = intent.Call<bool>("getBooleanExtra", "openbonk_map_lua_transition_probe", false);
        if (portalXpProbe && !usePortal) { Debug.LogError("Stage portal XP probe INCONCLUSIVE: portal mode required"); portalXpProbe = false; }
        if (statusProbe && !usePortal)
        {
            Debug.LogError("Stage status probe INCONCLUSIVE: the direct scene transition bypasses the portal's teleport cleanup");
            statusProbe = false;
        }
        if (!usePortal && !intent.Call<bool>("getBooleanExtra", "megabonk_stage_transition_probe", false)) return;
        installed = true;
        var root = new GameObject("Stage transition probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidStageTransitionProbe>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TransitionUI.A_transitionStart += OnTransitionStart;
        TransitionUI.A_MapTransitionStart += OnMapTransitionStart;
        TransitionUI.A_transitionEnd += OnTransitionEnd;
        if (statusProbe) PlayerStatusEffects.A_StatusEffectRemoved += OnStatusRemoved;
        if (portalXpProbe) { LevelupScreen.A_LevelupEnabled += OnPortalXpOpened; LevelupScreen.A_LevelUpClose += OnPortalXpClosed; }
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TransitionUI.A_transitionStart -= OnTransitionStart;
        TransitionUI.A_MapTransitionStart -= OnMapTransitionStart;
        TransitionUI.A_transitionEnd -= OnTransitionEnd;
        PlayerStatusEffects.A_StatusEffectRemoved -= OnStatusRemoved;
        Enemy.A_Damage -= OnStageDamage;
        LevelupScreen.A_LevelupEnabled -= OnPortalXpOpened;
        LevelupScreen.A_LevelUpClose -= OnPortalXpClosed;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!requested) return;
        sawScene = true;
        hpAtSceneLoad = trackedInventory != null && trackedInventory.playerHealth != null ? trackedInventory.playerHealth.hp : -1;
        Debug.Log($"Stage transition probe: loaded scene={scene.name}, index={MapController.index}, mode={mode}, retainedHp={hpAtSceneLoad}");
    }
    void OnTransitionStart() { if (requested) transitionStarts++; }
    void OnMapTransitionStart() { if (requested) mapStarts++; }
    void OnTransitionEnd() { if (requested) transitionEnds++; }
    void OnStatusRemoved(EStatusEffect effect)
    {
        if (!requested) return;
        if (effect == EStatusEffect.Haste) hasteRemoved++;
        if (effect == EStatusEffect.Shield) shieldRemoved++;
    }
    void OnStageDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy == null || damage == null || damage.damageSource != EWeapon.FireStaff.ToString()) return;
        stageFirestaffHits++;
        stageFirestaffDamage += damage.damage;
    }
    void OnPortalXpOpened() { portalXpOpened++; }
    void OnPortalXpClosed() { portalXpClosed++; }
    bool requested;

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
            (SceneManager.GetActiveScene().name != "GeneratedMap" || GameManager.Instance == null ||
             !GameManager.Instance.IsPlaying() || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted ||
             MyPlayer.Instance.inventory == null || MapController.currentMap == null || TransitionUI.Instance == null ||
             EnemyManager.Instance == null)) yield return null;
        var oldPlayer = MyPlayer.Instance;
        if (oldPlayer == null || oldPlayer.inventory == null || MapController.currentMap == null ||
            MapController.currentMap.stages == null || MapController.currentMap.stages.Length < 2 ||
            TransitionUI.Instance == null || EnemyManager.Instance == null)
        {
            Debug.LogError("Stage transition probe: initial scene unavailable or map lacks a second stage");
            yield break;
        }
        var originalInventory = oldPlayer.inventory;
        float mapSpawnBefore = EnemyManager.Instance.summonerController?.stageSummoner?.MapSpawnMultiplier ?? -1f;
        WeaponBase trackedWeapon = null;
        EStat trackedWeaponStat = default;
        float weaponValuePrepared = 0f, tomeValuePrepared = 0f, damageMultiplierPrepared = 0f;
        int weaponLevelPrepared = 0, tomeLevelPrepared = 0;
        var oldPlacer = FindObjectOfType<RandomObjectPlacer>();
        int initialShrineAmount = oldPlacer != null && oldPlacer.chargeShrineSpawns != null ? oldPlacer.chargeShrineSpawns.amount : -1;
        int originalIndex = MapController.index;
        var originalStage = MapController.currentStage;
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
        var invincibleUntil = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic);
        if (invincible != null) invincible.SetValue(oldPlayer, true);
        if (invincibleUntil != null) invincibleUntil.SetValue(oldPlayer, MyTime.time + 10000f);
        EnemyManager.Instance.enabledWaves = false;
        // Let the initial scene fade finish so only the requested transition's events are counted.
        yield return new WaitForSecondsRealtime(2f);
        if (originalInventory.itemInventory == null || originalInventory.playerStats == null || originalInventory.playerHealth == null)
        {
            Debug.LogError("Stage transition probe: item, stat or health inventory unavailable");
            yield break;
        }
        int cloverBefore = originalInventory.itemInventory.GetAmount(EItem.Clover);
        int beaconBefore = originalInventory.itemInventory.GetAmount(EItem.Beacon);
        if (beaconProbe) originalInventory.itemInventory.AddItem(EItem.Beacon);
        int pumpkinBefore = originalInventory.itemInventory.GetAmount(EItem.Pumpkin);
        if (pumpkinProbe) originalInventory.itemInventory.AddItem(EItem.Pumpkin, 2);
        float luckBefore = originalInventory.playerStats.GetStat(EStat.Luck);
        originalInventory.itemInventory.AddItem(EItem.Clover, 2);
        float statDeadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < statDeadline &&
            !Mathf.Approximately(originalInventory.playerStats.GetStat(EStat.Luck) - luckBefore, .2f)) yield return null;
        int cloverPrepared = originalInventory.itemInventory.GetAmount(EItem.Clover);
        float luckPrepared = originalInventory.playerStats.GetStat(EStat.Luck);
        if (loadoutProbe)
        {
            if (DataManager.Instance == null || !originalInventory.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out trackedWeapon))
            { Debug.LogError("Stage loadout probe INCONCLUSIVE: Firestaff unavailable"); yield break; }
            var tome = DataManager.Instance.GetTome(ETome.Damage);
            var weaponOffer = trackedWeapon.weaponData.GetUpgradeOffer(ERarity.Common);
            if (tome == null || weaponOffer == null || weaponOffer.Count == 0 || originalInventory.tomeInventory.GetTomeLevel(ETome.Damage) != 0)
            { Debug.LogError("Stage loadout probe INCONCLUSIVE: upgrade setup unavailable"); yield break; }
            trackedWeaponStat = weaponOffer[0].stat;
            originalInventory.weaponInventory.AddWeapon(trackedWeapon.weaponData, weaponOffer);
            originalInventory.tomeInventory.AddTome(tome, tome.GetUpgradeOffer(ERarity.Common), ERarity.Common);
            originalInventory.tomeInventory.AddTome(tome, tome.GetUpgradeOffer(ERarity.Common), ERarity.Common);
            yield return new WaitForSecondsRealtime(1f);
            weaponLevelPrepared = trackedWeapon.level;
            tomeLevelPrepared = originalInventory.tomeInventory.GetTomeLevel(ETome.Damage);
            weaponValuePrepared = trackedWeapon.GetValue(trackedWeaponStat);
            tomeValuePrepared = originalInventory.tomeInventory.tomeUpgrade[ETome.Damage].modification;
            damageMultiplierPrepared = originalInventory.playerStats.GetStat(EStat.DamageMultiplier);
            if (weaponLevelPrepared != 2 || tomeLevelPrepared != 2 || tomeValuePrepared == 0f)
            { Debug.LogError("Stage loadout probe INCONCLUSIVE: upgrade setup failed"); yield break; }
            Debug.Log($"Stage loadout probe prepared: weaponLevel={weaponLevelPrepared}, weaponStat={trackedWeaponStat}:{weaponValuePrepared}, tomeLevel={tomeLevelPrepared}, tomeModifier={tomeValuePrepared}, damageMultiplier={damageMultiplierPrepared}");
        }
        originalInventory.playerHealth.hp = Mathf.Min(37, originalInventory.playerHealth.maxHp);
        int hpPrepared = originalInventory.playerHealth.hp;
        trackedInventory = originalInventory;
        float speedBeforeStatus = originalInventory.playerStats.GetStat(EStat.MoveSpeedMultiplier);
        if (statusProbe)
        {
            var effects = originalInventory.statusEffects;
            effects.TestPickup(EPickup.Haste);
            effects.TestPickup(EPickup.Shield);
            effects.statusEffects[EStatusEffect.Haste].expirationTime = MyTime.time + 10000f;
            effects.statusEffects[EStatusEffect.Shield].expirationTime = MyTime.time + 10000f;
            float statusDeadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < statusDeadline && originalInventory.playerStats.GetStat(EStat.MoveSpeedMultiplier) <= speedBeforeStatus) yield return null;
            if (!effects.HasStatusEffect(EStatusEffect.Haste) || !effects.HasStatusEffect(EStatusEffect.Shield) || originalInventory.playerStats.GetStat(EStat.MoveSpeedMultiplier) <= speedBeforeStatus)
            { Debug.LogError("Stage status probe INCONCLUSIVE: status preparation failed"); yield break; }
        }
        bool preparationPassed = cloverPrepared == cloverBefore + 2 && Mathf.Approximately(luckPrepared - luckBefore, .2f) && hpPrepared > 0;
        Debug.Log($"Stage transition probe: before scene={SceneManager.GetActiveScene().name}, index={originalIndex}, stage={(originalStage != null ? originalStage.name : "null")}, inventory={RuntimeHelpers.GetHashCode(originalInventory)}, hp={hpPrepared}, clover={cloverBefore}->{cloverPrepared}, luck={luckBefore}->{luckPrepared}, player={oldPlayer.GetInstanceID()}, preparationPassed={preparationPassed}");
        if (!preparationPassed) { Debug.LogError("Stage transition probe: real item setup failed"); yield break; }
        requested = true;
        if (usePortal)
        {
            MapController.runConfig.mapTierIndex = 1;
            var altar = FindObjectOfType<InteractableBossSpawner>();
            if (altar == null || !altar.Interact()) { Debug.LogError("Stage portal probe: boss altar unavailable"); yield break; }
            var bosses = (System.Collections.Generic.List<Enemy>)typeof(InteractableBossSpawner).GetField("bossEnemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(altar);
            if (bosses == null || bosses.Count == 0) { Debug.LogError("Stage portal probe: no bosses spawned"); yield break; }
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && bosses.Exists(b => b != null && !b.CanTakeDamage())) yield return null;
            var spawned = new System.Collections.Generic.List<Enemy>(bosses);
            foreach (var boss in spawned) if (boss != null && boss.CanTakeDamage()) boss.Kill("StagePortalProbe");
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && altar != null && !altar.portal.activeInHierarchy) yield return null;
            var portal = altar != null ? altar.portal.GetComponent<InteractablePortal>() : null;
            if (portal == null || !portal.gameObject.activeInHierarchy || !portal.Interact()) { Debug.LogError("Stage portal probe: defeated boss did not yield an interactable portal"); yield break; }
            Debug.Log($"Stage portal probe: altar activated, killed={spawned.Count}, portal active and interaction accepted");
            if (portalXpProbe)
            {
                var xp = originalInventory.playerXp;
                if (!oldPlayer.isTeleporting || xp == null || xp.level > 9995) { Debug.LogError("Stage portal XP probe INCONCLUSIVE: teleport or XP unavailable"); yield break; }
                portalXpTargetLevel = xp.level + 3;
                int amount = XpUtility.LevelToXp(portalXpTargetLevel) - xp.xp;
                xp.AddXp(amount);
                bool queued = xp.level == portalXpTargetLevel && portalXpOpened == 0;
                Debug.Log($"Stage portal XP probe queued: teleporting={oldPlayer.isTeleporting}, amount={amount}, level={xp.level}, opened={portalXpOpened}, queued={queued}");
                if (!queued) { Debug.LogError("Stage portal XP probe FAIL: XP not deferred"); yield break; }
            }
        }
        else MapController.LoadNextStage();
        Debug.Log($"Stage transition probe: requested index={MapController.index}, transitionStarts={transitionStarts}, mapStarts={mapStarts}, transitioning={(TransitionUI.Instance != null && TransitionUI.Instance.isTransitioning)}");
        deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
            (!sawScene || MyPlayer.Instance == null || MyPlayer.Instance == oldPlayer || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null ||
             GameManager.Instance == null || !GameManager.Instance.IsPlaying()))
        {
            if (sawScene && EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
            if (sawScene && MyPlayer.Instance != null && MyPlayer.Instance != oldPlayer)
            {
                if (invincible != null) invincible.SetValue(MyPlayer.Instance, true);
                if (invincibleUntil != null) invincibleUntil.SetValue(MyPlayer.Instance, MyTime.time + 10000f);
            }
            yield return null;
        }
        var newPlayer = MyPlayer.Instance;
        if (!sawScene || newPlayer == null || newPlayer == oldPlayer || !newPlayer.hasStarted || newPlayer.inventory == null)
        {
            Debug.LogError($"Stage transition probe: timeout scene={sawScene}, index={MapController.index}, player={(newPlayer != null)}, starts={transitionStarts}, mapStarts={mapStarts}, ends={transitionEnds}");
            yield break;
        }
        yield return new WaitForSecondsRealtime(2f);
        bool sameInventory = ReferenceEquals(originalInventory, newPlayer.inventory);
        Debug.Log($"Stage transition probe stats: level={newPlayer.inventory.playerXp.level}, maxHealth={newPlayer.inventory.playerStats.GetStat(EStat.MaxHealth):F3}, duration={newPlayer.inventory.playerStats.GetStat(EStat.DurationMultiplier):F3}");
        bool oldPlayerDestroyed = oldPlayer == null;
        int cloverAfter = newPlayer.inventory.itemInventory.GetAmount(EItem.Clover);
        float luckAfter = newPlayer.inventory.playerStats.GetStat(EStat.Luck);
        int hpAfter = newPlayer.inventory.playerHealth.hp;
        // This probe starts as Fox: three injected levels add 3 * 0.015 luck
        // through his real level-up passive while preserving the two Clovers.
        float expectedLuck = luckPrepared + (portalXpProbe ? .045f : 0f);
        bool itemPreserved = cloverAfter == cloverPrepared && Mathf.Approximately(luckAfter, expectedLuck);
        bool healthNotReset = hpAtSceneLoad > 0 && hpAtSceneLoad < originalInventory.playerHealth.maxHp;
        bool passed = sameInventory && oldPlayerDestroyed && MapController.index == originalIndex + 1 &&
            MapController.currentStage != originalStage && transitionStarts == 1 && mapStarts == 1 && itemPreserved && healthNotReset;
        Debug.Log($"Stage transition probe: after scene={SceneManager.GetActiveScene().name}, index={MapController.index}, stage={(MapController.currentStage != null ? MapController.currentStage.name : "null")}, inventory={RuntimeHelpers.GetHashCode(newPlayer.inventory)}, hp={hpAfter}, hpAtSceneLoad={hpAtSceneLoad}, clover={cloverAfter}, luck={luckAfter}, player={newPlayer.GetInstanceID()}, oldPlayerDestroyed={oldPlayerDestroyed}, sameInventory={sameInventory}, itemPreserved={itemPreserved}, healthNotReset={healthNotReset}, transitionStarts={transitionStarts}, mapStarts={mapStarts}, transitionEnds={transitionEnds}, passed={passed}");
        if (mapLuaProbe)
        {
            var summoner = EnemyManager.Instance?.summonerController?.stageSummoner;
            float after = summoner != null ? summoner.MapSpawnMultiplier : -1f;
            float rateBase = (1f + MyTime.stageTimer / 60f * .5f) * Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(EStat.EnemyAmountMultiplier);
            var getMultiplier = typeof(Assets.Scripts.Game.Spawning.New.BaseSummoner).GetMethod("GetMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
            float difficulty = summoner != null && getMultiplier != null ? (float)getMultiplier.Invoke(summoner, null) : -1f;
            float progress = Mathf.Clamp01(MyTime.stageTimer / MapController.currentMap.stageDuration);
            int expectedTarget = Mathf.RoundToInt((5f + 200f * progress) * difficulty * Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(EStat.EnemyAmountMultiplier) * .5f);
            bool ok = passed && Mathf.Approximately(mapSpawnBefore, 2f) && Mathf.Approximately(after, .5f) && summoner != null &&
                Mathf.Approximately(summoner.GetBaseCreditsPerSecondUncapped(), rateBase * .5f) && summoner.GetNumTargetEnemies() == expectedTarget;
            Debug.Log($"External map Lua transition {(ok ? "PASS" : "FAIL")}: before={mapSpawnBefore}, after={after}, rate={(summoner != null ? summoner.GetBaseCreditsPerSecondUncapped() : -1f)}, baseline={rateBase}, target={(summoner != null ? summoner.GetNumTargetEnemies() : -1)}, expectedTarget={expectedTarget}");
        }
        if (portalXpProbe)
        {
            int selected = 0;
            for (int i = 0; i < 3; i++)
            {
                deadline = Time.realtimeSinceStartup + 25f;
                LevelupScreen screen = null;
                while (Time.realtimeSinceStartup < deadline)
                {
                    screen = FindObjectOfType<LevelupScreen>();
                    if (portalXpOpened > i && screen != null && screen.window != null && screen.window.activeInHierarchy && screen.upgradePicker != null && screen.upgradePicker.GetNumUpgrades() > 0) break;
                    yield return null;
                }
                if (screen == null || portalXpOpened <= i || screen.upgradePicker == null) break;
                deadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < deadline && Time.time <= screen.upgradePicker.banishCooldownOverAtTime) yield return null;
                UpgradeButton button = null;
                foreach (var candidate in screen.upgradePicker.buttons) if (candidate != null && candidate.gameObject.activeInHierarchy) { button = candidate; break; }
                if (button == null) break;
                int prior = portalXpClosed; button.SelectUpgrade(); if (portalXpClosed > prior) selected++;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            bool ok = passed && newPlayer.inventory.playerXp.level == portalXpTargetLevel && portalXpOpened == 3 && portalXpClosed == 3 && selected == 3 && !UiManager.Instance.encounterWindows.HasEncounter() && !MyTime.paused;
            string result = $"Stage portal XP probe {(ok ? "PASS" : "FAIL")}: stage={(MapController.currentStage != null ? MapController.currentStage.name : "null")}, level={newPlayer.inventory.playerXp.level}, opened={portalXpOpened}, selected={selected}, closed={portalXpClosed}, pending={UiManager.Instance.encounterWindows.HasEncounter()}, paused={MyTime.paused}";
            if (ok) Debug.Log(result); else Debug.LogError(result);
            if (luaRefreshProbe && ok)
            {
                var inv = newPlayer.inventory;
                const string hpKey = "lua-mod/example-level-stats-mod/stat_add:MaxHealth";
                const string durationKey = "lua-mod/example-level-stats-mod/stat_mul:DurationMultiplier";
                bool beforeHpFound = inv.statInventory.movingStats.TryGetValue(hpKey, out var beforeHp);
                bool beforeDurationFound = inv.statInventory.movingStats.TryGetValue(durationKey, out var beforeDuration);
                bool beforeFound = beforeHpFound && beforeDurationFound;
                float beforeHealth = inv.playerStats.GetStat(EStat.MaxHealth), beforeDurationStat = inv.playerStats.GetStat(EStat.DurationMultiplier);
                float oldHp = beforeFound ? beforeHp.modification : float.NaN;
                float oldDuration = beforeFound ? beforeDuration.modification : float.NaN;
                inv.playerXp.AddXp(XpUtility.LevelToXp(4) - inv.playerXp.xp);
                inv.statInventory.Tick(); inv.playerStats.ForceUpdateStats();
                bool afterHpFound = inv.statInventory.movingStats.TryGetValue(hpKey, out var afterHp);
                bool afterDurationFound = inv.statInventory.movingStats.TryGetValue(durationKey, out var afterDuration);
                bool refreshed = inv.playerXp.level == 4 && beforeFound && afterHpFound && afterDurationFound && Mathf.Approximately(oldHp, 10f) && Mathf.Approximately(oldDuration, 1.2f) && Mathf.Approximately(afterHp.modification, 40f) && Mathf.Approximately(afterDuration.modification, 1.4f);
                string refreshResult = $"Stage Lua refresh {(refreshed ? "PASS" : "FAIL")}: level={inv.playerXp.level}, modifierHealth={oldHp}->{(afterHpFound ? afterHp.modification : float.NaN)}, modifierDuration={oldDuration}->{(afterDurationFound ? afterDuration.modification : float.NaN)}, actualHealth={beforeHealth}->{inv.playerStats.GetStat(EStat.MaxHealth)}, actualDuration={beforeDurationStat}->{inv.playerStats.GetStat(EStat.DurationMultiplier)}";
                if (refreshed) Debug.Log(refreshResult); else Debug.LogError(refreshResult);
            }
        }
        if (loadoutProbe)
        {
            var inv = newPlayer.inventory;
            bool found = inv.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out var afterWeapon);
            int tomeLevel = inv.tomeInventory.GetTomeLevel(ETome.Damage);
            float tomeValue = inv.tomeInventory.tomeUpgrade.TryGetValue(ETome.Damage, out var modifier) ? modifier.modification : float.NaN;
            float weaponValue = found ? afterWeapon.GetValue(trackedWeaponStat) : float.NaN;
            float damageMultiplier = inv.playerStats.GetStat(EStat.DamageMultiplier);
            bool ok = passed && found && ReferenceEquals(trackedWeapon, afterWeapon) && afterWeapon.level == weaponLevelPrepared && tomeLevel == tomeLevelPrepared &&
                Mathf.Approximately(weaponValue, weaponValuePrepared) && Mathf.Approximately(tomeValue, tomeValuePrepared) && Mathf.Approximately(damageMultiplier, damageMultiplierPrepared);
            string result = $"Stage loadout probe {(ok ? "PASS" : "FAIL")}: sameWeapon={ReferenceEquals(trackedWeapon, afterWeapon)}, weaponLevel={(found ? afterWeapon.level : 0)}, weaponStat={trackedWeaponStat}:{weaponValuePrepared}->{weaponValue}, tomeLevel={tomeLevel}, tomeModifier={tomeValuePrepared}->{tomeValue}, damageMultiplier={damageMultiplierPrepared}->{damageMultiplier}";
            if (ok) Debug.Log(result); else Debug.LogError(result);
            if (ok && EnemyManager.Instance != null)
            {
                Enemy.A_Damage += OnStageDamage;
                afterWeapon.Enable();
                EnemyManager.Instance.enabledWaves = true;
                deadline = Time.realtimeSinceStartup + 70f;
                while (Time.realtimeSinceStartup < deadline && stageFirestaffHits == 0 && newPlayer != null && !newPlayer.IsDead()) yield return null;
                Enemy.A_Damage -= OnStageDamage;
                Debug.Log($"Stage loadout combat {(stageFirestaffHits > 0 ? "OBSERVED" : "INCONCLUSIVE")}: firestaffHits={stageFirestaffHits}, totalDamage={stageFirestaffDamage:F2}, stage={(MapController.currentStage != null ? MapController.currentStage.name : "null")}");
            }
        }
        if (statusProbe)
        {
            var effects = newPlayer.inventory.statusEffects;
            float statusDeadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < statusDeadline && !Mathf.Approximately(newPlayer.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier), speedBeforeStatus)) yield return null;
            float speedAfterStatus = newPlayer.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier);
            bool ok = sameInventory && !effects.HasStatusEffect(EStatusEffect.Haste) && !effects.HasStatusEffect(EStatusEffect.Shield) && hasteRemoved == 1 && shieldRemoved == 1 && Mathf.Approximately(speedAfterStatus, speedBeforeStatus);
            string result = $"Stage status probe {(ok ? "PASS" : "FAIL")}: sameInventory={sameInventory}, hasteActive={effects.HasStatusEffect(EStatusEffect.Haste)}, shieldActive={effects.HasStatusEffect(EStatusEffect.Shield)}, hasteRemoved={hasteRemoved}, shieldRemoved={shieldRemoved}, speed={speedBeforeStatus}->{speedAfterStatus}";
            if (ok) Debug.Log(result); else Debug.LogError(result);
        }
        if (beaconProbe)
        {
            var placer = FindObjectOfType<RandomObjectPlacer>();
            int amount = placer != null && placer.chargeShrineSpawns != null ? placer.chargeShrineSpawns.amount : -1;
            int count = FindObjectsOfType<ChargeShrine>().Length;
            int beacons = newPlayer.inventory.itemInventory.GetAmount(EItem.Beacon);
            int expectedAmount = initialShrineAmount + beacons * 2;
            int expectedCount = Mathf.RoundToInt(expectedAmount * MapController.currentMap.numShrinesPotsAndOtherMultiplier);
            Debug.Log($"Stage beacon probe: initialAmount={initialShrineAmount}, amount={amount}, expectedAmount={expectedAmount}, generated={count}, expectedCount={expectedCount}, beacon={beaconBefore}->{beacons}, multiplier={MapController.currentMap.numShrinesPotsAndOtherMultiplier}, passed={sameInventory && beacons == beaconBefore + 1 && initialShrineAmount >= 0 && amount == expectedAmount && count == expectedCount}");
        }
        if (pumpkinProbe)
        {
            int pumpkins = newPlayer.inventory.itemInventory.GetAmount(EItem.Pumpkin);
            var item = newPlayer.inventory.itemInventory.GetItem(EItem.Pumpkin) as ItemPumpkin;
            var pots = FindObjectsOfType<InteractablePot>();
            int small = 0, big = 0;
            foreach (var pot in pots) { if (pot.isSilver) continue; if (pot.isBig) big++; else small++; }
            bool ok = sameInventory && pumpkins == pumpkinBefore + 2 && item != null && item.GetExtraPotsSmall() == pumpkins * 8 && item.GetExtraPotsBig() == pumpkins * 2 && small >= item.GetExtraPotsSmall() && big >= item.GetExtraPotsBig();
            string result = $"Stage pumpkin probe {(ok ? "PASS" : "FAIL")}: pumpkin={pumpkinBefore}->{pumpkins}, sameInventory={sameInventory}, bonusSmall={(item != null ? item.GetExtraPotsSmall() : -1)}, bonusBig={(item != null ? item.GetExtraPotsBig() : -1)}, actualSmall={small}, actualBig={big}";
            if (ok) Debug.Log(result); else Debug.LogError(result);
            var placer = FindObjectOfType<RandomObjectPlacer>();
            var entries = MapController.currentMap.randomObjectsOverride.Length == 0 ? placer.randomObjects : MapController.currentMap.randomObjectsOverride;
            RandomMapObject smallConfig = null, bigConfig = null;
            foreach (var entry in entries)
            {
                if (entry?.prefabs == null || entry.prefabs.Length == 0 || entry.prefabs[0] == null) continue;
                if (entry.prefabs[0].name == "PotSmall") smallConfig = entry;
                if (entry.prefabs[0].name == "PotBig") bigConfig = entry;
            }
            if (smallConfig == null || bigConfig == null) { Debug.LogError("Stage pumpkin comparison FAIL: pot configs unavailable"); yield break; }
            var inventory = newPlayer.inventory.itemInventory;
            for (int i = 0; i < pumpkins; i++) inventory.RemoveItem(EItem.Pumpkin, false);
            int[] baseline = GenerateControlled(placer, smallConfig, bigConfig);
            inventory.AddItem(EItem.Pumpkin, pumpkins);
            int[] withPumpkin = GenerateControlled(placer, smallConfig, bigConfig);
            bool sameSeedPass = baseline[0] == 3 && baseline[1] == 3 && withPumpkin[0] - baseline[0] == pumpkins * 8 && withPumpkin[1] - baseline[1] == pumpkins * 2;
            string comparison = $"Stage pumpkin comparison {(sameSeedPass ? "PASS" : "FAIL")}: baseline={baseline[0]}/{baseline[1]}, withPumpkin={withPumpkin[0]}/{withPumpkin[1]}, stacks={pumpkins}, seed=12345";
            if (sameSeedPass) Debug.Log(comparison); else Debug.LogError(comparison);
        }
    }

    static int[] GenerateControlled(RandomObjectPlacer placer, RandomMapObject small, RandomMapObject big)
    {
        int Count(bool isBig)
        {
            int n = 0; foreach (var pot in FindObjectsOfType<InteractablePot>()) if (!pot.isSilver && pot.isBig == isBig) n++; return n;
        }
        RandomMapObject Copy(RandomMapObject source) => new RandomMapObject {
            amount = 3, maxAmount = 0, checkRadius = source.checkRadius, scaleMin = source.scaleMin,
            scaleMax = source.scaleMax, maxSlopeAngle = source.maxSlopeAngle, upOffset = source.upOffset,
            prefabs = source.prefabs, randomRotationVector = source.randomRotationVector, alignWithNormal = source.alignWithNormal
        };
        int initialSmall = Count(false), initialBig = Count(true);
        Random.State oldState = Random.state; var oldRandom = global::Utility.MyRandom.random;
        try
        {
            Random.InitState(12345); global::Utility.MyRandom.random = new System.Random(12345);
            placer.Generate(new[] { Copy(small), Copy(big) }, 1f);
            return new[] { Count(false) - initialSmall, Count(true) - initialBig };
        }
        finally { Random.state = oldState; global::Utility.MyRandom.random = oldRandom; }
    }
}
#endif
