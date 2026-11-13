#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Tools;
using Assets.Scripts._Data;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in diagnostic for an isolated test run; the normal transition is untouched.
public sealed class AndroidFinalFightProbe : MonoBehaviour
{
    private static readonly FieldInfo Invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo InvincibleUntil = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic);

    private bool physicalPylons;
    private bool weaponFight;
    private bool weaponTransfer;
    private bool bossRim;
    private bool keepStageForComparison;
    private bool instantBossForComparison;

    public static void Begin(bool physicalPylons)
    {
        var go = new GameObject("Android final fight probe");
        DontDestroyOnLoad(go);
        var probe = go.AddComponent<AndroidFinalFightProbe>();
        probe.physicalPylons = physicalPylons;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        probe.weaponFight = intent.Call<bool>("getBooleanExtra", "megabonk_final_weapon_fight_probe", false);
        probe.weaponTransfer = intent.Call<bool>("getBooleanExtra", "megabonk_final_transfer_probe", false);
        probe.bossRim = intent.Call<bool>("getBooleanExtra", "megabonk_final_rim_probe", false);
        probe.keepStageForComparison = intent.Call<bool>("getBooleanExtra", "megabonk_final_keep_stage_probe", false);
        probe.instantBossForComparison = intent.Call<bool>("getBooleanExtra", "megabonk_final_instant_boss_probe", false);
    }

    private IEnumerator Start()
    {
        var oldInventory = MyPlayer.Instance.inventory;
        if (weaponTransfer)
        {
            var sword=DataManager.Instance.GetWeapon(EWeapon.Sword);
            if(sword!=null && !oldInventory.weaponInventory.weapons.ContainsKey(EWeapon.Sword)) oldInventory.weaponInventory.AddWeapon(sword,null);
        }
        var stages = MapController.currentMap.stages;
        int last = keepStageForComparison ? MapController.index : MapController.runConfig.challenge != null ? MapController.runConfig.mapTierIndex : stages.Length - 1;
        MapController.index = last;
        typeof(MapController).GetProperty("currentStage", BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new object[] { stages[last] });
        MapController.runConfig.stageData = stages[last];
        Debug.Log($"Final fight probe: transitioning map={MapController.currentMap.name}, stage={MapController.currentStage.name}, index={last}, inventory={oldInventory != null}");
        MapController.LoadFinalStage();
        StartCoroutine(ExerciseFight());
        for (int i = 0; i < (weaponFight ? 110 : 30); i++)
        {
            yield return new WaitForSecondsRealtime(4f);
            var player = MyPlayer.Instance;
            var fight = FindObjectOfType<FinalFightController>();
            var generator = FindObjectOfType<MapGenerationFinalBoss>();
            bool sameInventory = player != null && ReferenceEquals(player.inventory, oldInventory);
            Debug.Log($"Final fight probe: scene={SceneManager.GetActiveScene().name}, generator={generator != null}, player={player != null}, sameInventory={sameInventory}, started={(player != null && player.hasStarted)}, fight={fight != null}, boss={(fight != null && fight.boss != null)}, bossHp={(fight != null && fight.boss != null ? fight.boss.hp : -1f)}, phase={(fight != null ? fight.currentPhase : -1)}, gameTime={MyTime.time}, paused={MyTime.paused}");
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "FinalBossMap" || MyPlayer.Instance == null) return;
        Invincible.SetValue(MyPlayer.Instance, true);
        InvincibleUntil.SetValue(MyPlayer.Instance, float.MaxValue);
    }

    private IEnumerator ExerciseFight()
    {
        FinalFightController fight = null;
        float deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline)
        {
            fight = FindObjectOfType<FinalFightController>();
            if (fight != null && fight.boss != null && fight.boss.CanTakeDamage()) break;
            yield return new WaitForSecondsRealtime(.5f);
        }
        if (fight == null || fight.boss == null || !fight.boss.CanTakeDamage()) { Debug.Log("Final fight probe: boss did not become damageable"); yield break; }
        var boss = fight.boss;
        if (bossRim)
        {
            yield return CheckBossRim(boss);
            yield break;
        }
        if (weaponTransfer)
        {
            yield return ExerciseTransfers(fight);
            yield break;
        }
        if (weaponFight)
        {
            yield return ExerciseWeaponFight(fight, boss);
            yield break;
        }
        if (MapController.runConfig.challenge != null)
        {
            var stats = MyPlayer.Instance.inventory.playerStats;
            var counter = FindObjectOfType<KillsAndGoldCounter>();
            Debug.Log($"Challenge final-stage probe: chestPrice={stats.GetStat(Assets.Scripts.Menu.Shop.EStat.ChestPriceMultiplier)}, enemyScaling={stats.GetStat(Assets.Scripts.Menu.Shop.EStat.EnemyScalingMultiplier)}, silver={stats.GetStat(Assets.Scripts.Menu.Shop.EStat.SilverIncreaseMultiplier)}, cost={Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney.MoneyUtility.GetChestPrice()}, hud={counter?.t_chestPrice?.text}");
        }
        Debug.Log($"Final fight probe: exercising boss={boss.enemyData.name}, hp={boss.hp}, max={boss.maxHp}");
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            if (intent.Call<bool>("getBooleanExtra", "megabonk_energy_core_probe", false))
            {
                Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.SetValueForce("finalBossKills", 1f);
                Debug.Log("Boss achievement probe: seeded one previous final boss kill");
            }
        LogBossAchievements("before");
        var activeField = typeof(FinalFightController).GetField("activePylons", BindingFlags.Instance | BindingFlags.NonPublic);
        var complete = typeof(BossPylon).GetMethod("Complete", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (float fraction in instantBossForComparison ? new float[0] : new[] { .74f, .49f, .24f })
        {
            var dc = new DamageContainer(0f, "final_fight_probe") { enemy = boss, damage = Mathf.Max(0f, boss.hp - boss.maxHp * fraction) };
            boss.DamageExternal(dc);
            var pylons = ((List<BossPylon>)activeField.GetValue(fight)).ToArray();
            Debug.Log($"Final fight probe: threshold={fraction}, phase={fight.currentPhase}, activePylons={pylons.Length}, bossDamageable={boss.CanTakeDamage()}");
            if (physicalPylons && fight.currentPhase == 1)
            {
                var body = MyPlayer.Instance.playerMovement.GetRb();
                var oldConstraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
                var completed = typeof(BossPylon).GetField("completed", BindingFlags.Instance | BindingFlags.NonPublic);
                var charging = typeof(BossPylon).GetField("charging", BindingFlags.Instance | BindingFlags.NonPublic);
                var progress = typeof(BossPylon).GetField("chargeProgress", BindingFlags.Instance | BindingFlags.NonPublic);
                yield return new WaitForSecondsRealtime(3f);
                bool checkedInterruption = false;
                foreach (var pylon in pylons)
                {
                    var trigger = pylon.GetComponent<Collider>();
                    if (trigger == null) { Debug.Log("Final pylon physics probe: no collider"); break; }
                    Vector3 center = trigger.bounds.center;
                    if (!Physics.Raycast(center + Vector3.up * 100f, Vector3.down, out RaycastHit ground, 500f, GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore))
                    {
                        Debug.Log($"Final pylon physics probe: no ground below trigger={center}");
                        break;
                    }
                    body.position = center + Vector3.up * 100f; body.velocity = Vector3.zero; Physics.SyncTransforms();
                    yield return new WaitForSecondsRealtime(.5f);
                    body.position = MyPlayer.Instance.playerMovement.FeetPositionToRb(ground.point); body.velocity = Vector3.zero; Physics.SyncTransforms();
                    yield return new WaitForSecondsRealtime(1f);
                    Debug.Log($"Final pylon physics probe: entered charging={charging.GetValue(pylon)}, progress={progress.GetValue(pylon)}, player={body.position}, ground={ground.point}, triggerCenter={trigger.bounds.center}, triggerSize={trigger.bounds.size}");
                    if (!checkedInterruption)
                    {
                        checkedInterruption = true;
                        float beforeExit = (float)progress.GetValue(pylon);
                        body.position = center + Vector3.up * 100f; body.velocity = Vector3.zero; Physics.SyncTransforms();
                        yield return new WaitForSecondsRealtime(.8f);
                        float afterExit = (float)progress.GetValue(pylon);
                        Debug.Log($"Final pylon interruption probe: before={beforeExit:F3}, after={afterExit:F3}, charging={charging.GetValue(pylon)}, completed={completed.GetValue(pylon)}");
                        body.position = MyPlayer.Instance.playerMovement.FeetPositionToRb(ground.point); body.velocity = Vector3.zero; Physics.SyncTransforms();
                        yield return new WaitForSecondsRealtime(.5f);
                        Debug.Log($"Final pylon interruption probe: reentered charging={charging.GetValue(pylon)}, progress={(float)progress.GetValue(pylon):F3}");
                    }
                    float until = Time.realtimeSinceStartup + 12f;
                    while (!(bool)completed.GetValue(pylon) && Time.realtimeSinceStartup < until) yield return new WaitForSecondsRealtime(.5f);
                    Debug.Log($"Final pylon physics probe: completed={completed.GetValue(pylon)}, charging={charging.GetValue(pylon)}, progress={progress.GetValue(pylon)}");
                }
                body.constraints = oldConstraints;
            }
            else foreach (var pylon in pylons) complete.Invoke(pylon, null);
            Debug.Log($"Final fight probe: completedPylons phase={fight.currentPhase}, remaining={((List<BossPylon>)activeField.GetValue(fight)).Count}, bossDamageable={boss.CanTakeDamage()}");
            yield return new WaitForSecondsRealtime(1f);
        }
        boss.Kill("final_fight_probe");
        Debug.Log($"Final fight probe: forced lethal hp={boss.hp}, dead={boss.IsDead()}");
        if (MapController.runConfig.challenge != null) StartCoroutine(CheckChallengeNotification());
        yield return new WaitForSecondsRealtime(20f);
        LogBossAchievements("after");
        if (MapController.runConfig.challenge != null)
        {
            var challenge = MapController.runConfig.challenge;
            float completed = Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.challengesCompleted.ToString());
            Debug.Log($"Challenge completion probe: challenge={challenge.internalName}, unlocked={challenge.IsUnlocked()}, completedCount={completed}");
        }
        var cinematic = FindObjectOfType<FianlBossCinematic>();
        Debug.Log($"Final fight probe: afterDefeat bossCleared={fight.boss == null}, portal={(cinematic != null && cinematic.finalPortal != null && cinematic.finalPortal.activeInHierarchy)}, playerAlive={(MyPlayer.Instance != null && MyPlayer.Instance.inventory.playerHealth.hp > 0)}");
        var exit = FindObjectOfType<InteractablePortalFinal>();
        if (exit == null) { Debug.Log("Final fight probe: active exit interaction not found"); yield break; }
        bool accepted = exit.Interact();
        Debug.Log($"Final fight probe: portalInteraction={accepted}");
        yield return new WaitForSecondsRealtime(12f);
        Debug.Log($"Final fight probe: afterPortal scene={SceneManager.GetActiveScene().name}, paused={MyTime.paused}, playerAlive={(MyPlayer.Instance != null && MyPlayer.Instance.inventory.playerHealth.hp > 0)}, deathScreen={(UiManager.Instance != null && UiManager.Instance.deathScreen != null && UiManager.Instance.deathScreen.gameObject.activeInHierarchy)}");
        AndroidVictorySaveProbe.Log("afterPortal");
    }

    private IEnumerator CheckBossRim(Assets.Scripts.Actors.Enemies.Enemy boss)
    {
        var component=boss.enemyRenderer; var renderer=component!=null ? component.enemyRenderer : null;
        if(renderer==null || renderer.sharedMaterial==null) { Debug.LogError("Final rim probe INCONCLUSIVE: renderer unavailable"); yield break; }
        int originalLayer=renderer.gameObject.layer; var originalBlock=new MaterialPropertyBlock(); renderer.GetPropertyBlock(originalBlock);
        var root=new GameObject("Final rim camera"); var camera=root.AddComponent<Camera>(); camera.enabled=false;
        var bounds=renderer.bounds; camera.orthographic=true; camera.orthographicSize=Mathf.Max(.5f,bounds.extents.magnitude*1.3f);
        root.transform.position=bounds.center+new Vector3(0,bounds.extents.y*.3f,-bounds.extents.magnitude*3f); root.transform.LookAt(bounds.center);
        camera.cullingMask=1<<31; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black; camera.allowHDR=false; camera.allowMSAA=false;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); var image=new Texture2D(256,256,TextureFormat.RGBA32,false); camera.targetTexture=target;
        renderer.gameObject.layer=31; yield return null;
        try
        {
            Color32[] Capture() { camera.Render(); var old=RenderTexture.active; RenderTexture.active=target; image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active=old; return image.GetPixels32(); }
            component.SetInvulnerable(false); var plain=Capture();
            component.SetInvulnerable(true); var shielded=Capture();
            int changed=0,cyan=0;
            for(int i=0;i<plain.Length;i++)
            {
                var a=plain[i]; var b=shielded[i];
                if(System.Math.Abs(a.r-b.r)+System.Math.Abs(a.g-b.g)+System.Math.Abs(a.b-b.b)>12) changed++;
                if(b.g>b.r+20 && b.b>b.r+20) cyan++;
            }
            System.IO.File.WriteAllBytes(Application.persistentDataPath+"/final-boss-rim.png",image.EncodeToPNG());
            Debug.Log($"Final rim probe {(changed>0 ? "PASS" : "FAIL")}: material={renderer.sharedMaterial.name}, shader={renderer.sharedMaterial.shader.name}, rim={renderer.sharedMaterial.GetFloat("_Rim")}, changedPixels={changed}, cyanPixels={cyan}, bounds={bounds.size}");
        }
        finally { renderer.SetPropertyBlock(originalBlock); renderer.gameObject.layer=originalLayer; camera.targetTexture=null; Destroy(root); Destroy(image); target.Release(); Destroy(target); }
    }

    private IEnumerator ExerciseTransfers(FinalFightController fight)
    {
        var inventory=MyPlayer.Instance.inventory.weaponInventory;
        var sword=DataManager.Instance.GetWeapon(EWeapon.Sword);
        if(sword==null) { Debug.LogError("Final transfer probe INCONCLUSIVE: Sword data missing"); yield break; }
        if(!inventory.weapons.TryGetValue(EWeapon.Sword,out var weapon)) { Debug.LogError("Final transfer probe INCONCLUSIVE: Sword unavailable"); yield break; }
        float deadline=Time.realtimeSinceStartup+60f;
        while(weapon.enabled && Time.realtimeSinceStartup<deadline) yield return null;
        var theft=FindObjectsByType<StealWeaponWui>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).FirstOrDefault();
        bool stolen=!weapon.enabled, icon=theft!=null && theft.icon!=null && theft.icon.texture==sword.GetIcon();
        Debug.Log($"Final transfer probe THEFT: disabled={stolen}, effect={(theft!=null)}, icon={icon}, gameTime={MyTime.time:F2}");
        deadline=Time.realtimeSinceStartup+45f;
        while(theft!=null && Time.realtimeSinceStartup<deadline) yield return null;
        bool theftFinished=theft==null;
        var takenField=typeof(FinalFightController).GetField("numWeaponsTaken",BindingFlags.Instance|BindingFlags.NonPublic);
        var limitField=typeof(FinalFightController).GetField("numWeaponsToTake",BindingFlags.Instance|BindingFlags.NonPublic);
        deadline=Time.realtimeSinceStartup+45f;
        while((int)takenField.GetValue(fight)<(int)limitField.GetValue(fight) && Time.realtimeSinceStartup<deadline) yield return null;
        var boss=fight.boss;
        if(boss==null || !boss.CanTakeDamage()) { Debug.LogError("Final transfer probe INCONCLUSIVE: boss unavailable"); yield break; }
        boss.DamageExternal(new DamageContainer(0f,"transfer_probe") { enemy=boss, damage=Mathf.Max(0f,boss.hp-boss.maxHp*.74f) });
        var activeField=typeof(FinalFightController).GetField("activePylons",BindingFlags.Instance|BindingFlags.NonPublic);
        var pylons=((List<BossPylon>)activeField.GetValue(fight)).ToArray();
        int completedCount=0;
        if(physicalPylons)
        {
            var body=MyPlayer.Instance.playerMovement.GetRb();
            var oldConstraints=body.constraints; body.constraints=RigidbodyConstraints.FreezeAll;
            var completed=typeof(BossPylon).GetField("completed",BindingFlags.Instance|BindingFlags.NonPublic);
            yield return new WaitForSecondsRealtime(3f);
            foreach(var pylon in pylons)
            {
                var trigger=pylon.GetComponent<Collider>();
                if(trigger==null || !Physics.Raycast(trigger.bounds.center+Vector3.up*100f,Vector3.down,out RaycastHit ground,500f,GameManager.Instance.whatIsGroundAndObjects,QueryTriggerInteraction.Ignore)) break;
                body.position=trigger.bounds.center+Vector3.up*100f; body.velocity=Vector3.zero; Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(.5f);
                body.position=MyPlayer.Instance.playerMovement.FeetPositionToRb(ground.point); body.velocity=Vector3.zero; Physics.SyncTransforms();
                deadline=Time.realtimeSinceStartup+35f;
                while(!(bool)completed.GetValue(pylon) && Time.realtimeSinceStartup<deadline) yield return null;
                bool charged=(bool)completed.GetValue(pylon); if(charged) completedCount++;
                Debug.Log($"Final transfer probe PYLON: charged={charged}, count={completedCount}");
                if(!charged) break;
            }
            body.constraints=oldConstraints;
        }
        else
        {
            var complete=typeof(BossPylon).GetMethod("Complete",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var pylon in pylons) { complete.Invoke(pylon,null); completedCount++; }
        }
        var returned=FindObjectsByType<ReturnWeaponWui>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).FirstOrDefault();
        bool restored=weapon.enabled, returnedIcon=returned!=null && returned.icon!=null && returned.icon.texture==sword.GetIcon();
        Debug.Log($"Final transfer probe RETURN: restored={restored}, effect={(returned!=null)}, icon={returnedIcon}, theftFinished={theftFinished}, pylons={pylons.Length}");
        deadline=Time.realtimeSinceStartup+45f;
        while(returned!=null && Time.realtimeSinceStartup<deadline) yield return null;
        Debug.Log($"Final transfer probe {(stolen && icon && theftFinished && pylons.Length==3 && completedCount==3 && restored && returnedIcon && returned==null ? "PASS" : "FAIL")}: returnedFinished={(returned==null)}, completedPylons={completedCount}");
    }

    private IEnumerator ExerciseWeaponFight(FinalFightController fight, Assets.Scripts.Actors.Enemies.Enemy boss)
    {
        var inventory = MyPlayer.Instance.inventory;
        float boost = boss.maxHp * .008f;
        inventory.statInventory.ChangeMovingStat("final fight probe damage", new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier {
            stat = Assets.Scripts.Menu.Shop.EStat.DamageMultiplier,
            modifyType = Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType.Flat,
            modification = boost
        });
        var activeField = typeof(FinalFightController).GetField("activePylons", BindingFlags.Instance | BindingFlags.NonPublic);
        var completed = typeof(BossPylon).GetField("completed", BindingFlags.Instance | BindingFlags.NonPublic);
        var body = MyPlayer.Instance.playerMovement.GetRb();
        var oldConstraints = body.constraints;
        body.constraints = RigidbodyConstraints.FreezeAll;
        int hits = 0, charges = 0, phases = 0;
        Assets.Scripts.Actors.Enemies.Enemy.A_Damage += OnDamage;
        float deadline = Time.realtimeSinceStartup + 300f;
        Debug.Log($"Final weapon fight probe START: boss={boss.enemyData.enemyName}, hp={boss.hp:F1}, addedMultiplier={boost:F1}");
        while (Time.realtimeSinceStartup < deadline && fight != null && fight.boss != null)
        {
            var pylons = ((List<BossPylon>)activeField.GetValue(fight)).ToArray();
            if (pylons.Length > 0)
            {
                phases = Mathf.Max(phases, fight.currentPhase);
                yield return new WaitForSecondsRealtime(3f);
                foreach (var pylon in pylons)
                {
                    if (pylon == null || (bool)completed.GetValue(pylon)) continue;
                    var trigger = pylon.GetComponent<Collider>();
                    if (trigger == null || !Physics.Raycast(trigger.bounds.center + Vector3.up * 100f, Vector3.down, out RaycastHit ground, 500f, GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore))
                    { Debug.LogError("Final weapon fight probe FAIL: pylon ground unavailable"); break; }
                    body.position = trigger.bounds.center + Vector3.up * 100f; body.velocity = Vector3.zero; Physics.SyncTransforms();
                    yield return new WaitForSecondsRealtime(.5f);
                    body.position = MyPlayer.Instance.playerMovement.FeetPositionToRb(ground.point); body.velocity = Vector3.zero; Physics.SyncTransforms();
                    float limit = Time.realtimeSinceStartup + 30f;
                    while (Time.realtimeSinceStartup < limit && !(bool)completed.GetValue(pylon)) yield return new WaitForSecondsRealtime(.5f);
                    bool done = (bool)completed.GetValue(pylon); if (done) charges++;
                    Debug.Log($"Final weapon fight probe PYLON: phase={fight.currentPhase}, completed={done}, total={charges}");
                    if (!done) break;
                }
            }
            else
            {
                // Keep the real weapon in range without aiming or applying damage ourselves.
                Vector3 point = boss.transform.position + Vector3.forward * 8f;
                if (Physics.Raycast(point + Vector3.up * 100f, Vector3.down, out RaycastHit ground, 500f, GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore))
                { body.position = MyPlayer.Instance.playerMovement.FeetPositionToRb(ground.point); body.velocity = Vector3.zero; Physics.SyncTransforms(); }
                yield return new WaitForSecondsRealtime(1f);
            }
        }
        Assets.Scripts.Actors.Enemies.Enemy.A_Damage -= OnDamage;
        body.constraints = oldConstraints;
        bool defeated = fight != null && fight.boss == null;
        Debug.Log($"Final weapon fight probe RESULT: defeated={defeated}, firestaffHits={hits}, phases={phases}, chargedPylons={charges}");
        if (!defeated) yield break;
        InteractablePortalFinal exit = null;
        float portalDeadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < portalDeadline && exit == null)
        {
            exit = FindObjectOfType<InteractablePortalFinal>();
            if (exit == null) yield return new WaitForSecondsRealtime(.5f);
        }
        bool accepted = exit != null && exit.Interact();
        yield return new WaitForSecondsRealtime(12f);
        Debug.Log($"Final weapon fight probe {(accepted && hits > 0 && phases == 3 && charges == 9 ? "PASS" : "INCONCLUSIVE")}: portal={accepted}, hits={hits}, phases={phases}, charges={charges}, deathScreen={(UiManager.Instance != null && UiManager.Instance.deathScreen != null && UiManager.Instance.deathScreen.gameObject.activeInHierarchy)}");
        if (accepted)
        {
            AndroidVictorySaveProbe.Log("weaponFightAfterPortal");
            LogBossAchievements("weaponFightAfterPortal");
        }
        void OnDamage(Assets.Scripts.Actors.Enemies.Enemy enemy, DamageContainer dc)
        { if (enemy == boss && dc.damageSource == "FireStaff") { hits++; Debug.Log($"Final weapon fight probe HIT: damage={dc.damage:F1}, hp={enemy.hp:F1}, phase={fight.currentPhase}"); } }
    }

    private static void LogBossAchievements(string stage)
    {
        var player = MyPlayer.Instance;
        var save = SaveManager.Instance != null ? SaveManager.Instance.progression : null;
        if (player == null || save == null) { Debug.Log("Boss achievement probe: missing player or save"); return; }
        string speed = AchievementGenerator.GetSkinAchievementName(player.character, ESkinType.Speedrun);
        string final = AchievementGenerator.GetSkinAchievementName(player.character, ESkinType.FinalBoss);
        float finalBossKills = Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat("finalBossKills");
        Debug.Log($"Boss achievement probe: {stage} character={player.character}, time={MyTime.runTimer:F2}, finalStage={MapController.IsFinalBossStage()}, cursed={save.achievements.Contains("a_cursedTome")}, holy={save.achievements.Contains("a_holyBook")}, speed={speed}:{save.achievements.Contains(speed)}, final={final}:{save.achievements.Contains(final)}, finalBossKills={finalBossKills}, energyCore={save.achievements.Contains("a_energyCore")}");
    }

    private IEnumerator CheckChallengeNotification()
    {
        yield return new WaitForSecondsRealtime(2f);
        var ui = FindObjectOfType<ChallengeCompletedUi>();
        Debug.Log($"Challenge notification probe: active={(ui != null && ui.content != null && ui.content.activeSelf)}, header={(ui != null && ui.t_header != null ? ui.t_header.text : "<missing>")}, particles={(ui != null && ui.particles != null && ui.particles.activeSelf)}");
        yield return new WaitForSecondsRealtime(5f);
        Debug.Log($"Challenge notification probe: afterAnimation active={(ui != null && ui.content != null && ui.content.activeSelf)}, particles={(ui != null && ui.particles != null && ui.particles.activeSelf)}");
    }
}
#endif
