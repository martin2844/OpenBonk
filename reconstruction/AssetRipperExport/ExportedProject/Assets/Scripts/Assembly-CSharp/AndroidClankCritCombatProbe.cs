#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Intent-gated diagnostic: forced XP level, ordinary Revolver attacks against normal waves.
internal sealed class AndroidClankCritCombatProbe : MonoBehaviour
{
    int spawned, normal, critical, mismatch; WeaponBase weapon;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"); using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"); using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_clank_crit_combat_probe",false)) return;
        var go=new GameObject("Clank crit combat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidClankCritCombatProbe>();
    }
    IEnumerator Start() {
        float deadline=Time.realtimeSinceStartup+180;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance==null || PickupManager.Instance==null)) yield return null;
        var player=MyPlayer.Instance;
        if(player==null || player.character!=ECharacter.Cl4nk || player.inventory==null || !player.inventory.HasPassive(EPassive.CritHappens) || !player.inventory.weaponInventory.weapons.TryGetValue(EWeapon.Revolver,out weapon)) { Debug.LogError("Clank crit combat probe INCONCLUSIVE: character or Revolver unavailable"); yield break; }
        var xp=player.inventory.playerXp; if(xp.level!=0) { Debug.LogError("Clank crit combat probe INCONCLUSIVE: unexpected level"); yield break; }
        EnemyManager.Instance.enabledWaves=false; weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,float.MaxValue);
        xp.xp=XpUtility.LevelToXp(49)-1; xp.level=48; UiManager.Instance.encounterWindows.currentLevel=48; typeof(PlayerXp).GetField("leftOverXp",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(xp,0f);
        var pickup=PickupManager.Instance.SpawnPickup(EPickup.Xp,player.transform.position,1,false,0f);
        deadline=Time.realtimeSinceStartup+40; LevelupScreen screen=null;
        while(Time.realtimeSinceStartup<deadline) { screen=FindObjectOfType<LevelupScreen>(); if(xp.level==49 && screen!=null && screen.window!=null && screen.window.activeInHierarchy && screen.upgradePicker!=null && screen.upgradePicker.GetNumUpgrades()>0) break; yield return null; }
        if(xp.level!=49 || screen==null || screen.window==null || !screen.window.activeInHierarchy || screen.upgradePicker==null || screen.upgradePicker.GetNumUpgrades()==0) { Debug.LogError("Clank crit combat probe INCONCLUSIVE: level screen unavailable"); yield break; }
        float until=Time.realtimeSinceStartup+15; var button=screen.upgradePicker.buttons[0]; while(Time.realtimeSinceStartup<until && !button.GetButton().interactable) yield return null;
        if(!button.GetButton().interactable) { Debug.LogError("Clank crit combat probe INCONCLUSIVE: offer unavailable"); yield break; }
        button.SelectUpgrade(); yield return null; yield return null;
        if(screen.window.activeInHierarchy) { Debug.LogError("Clank crit combat probe INCONCLUSIVE: level screen remained open"); yield break; }
        weapon.Enable(); Enemy.A_EnemySpawned+=OnSpawn; Enemy.A_Damage+=OnDamage; EnemyManager.Instance.enabledWaves=true;
        float chance=WeaponUtility.GetCritChance(weapon); Debug.Log($"Clank crit combat probe START: level={xp.level} collected={(pickup!=null&&!pickup.gameObject.activeInHierarchy)} chance={chance:F3}");
        deadline=Time.realtimeSinceStartup+120; while(Time.realtimeSinceStartup<deadline && (normal<2 || critical<2)) yield return null;
        bool pass=spawned>0 && normal>=2 && critical>=2 && mismatch==0 && chance>.49f && chance<.60f;
        Debug.Log($"Clank crit combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: spawned={spawned} normal={normal} critical={critical} mismatches={mismatch} chance={chance:F3}");
        Enemy.A_EnemySpawned-=OnSpawn; Enemy.A_Damage-=OnDamage;
    }
    void OnSpawn(Enemy e) { if(e==null || e.IsBoss()) return; e.SetDiagnosticHealth(1000f); spawned++; }
    void OnDamage(Enemy e,DamageContainer dc) {
        if(e==null || dc==null || dc.damageSource!="Revolver" || e.IsElite() || e.IsBoss()) return;
        float expected=WeaponUtility.GetDamage(weapon)*(dc.crit ? 2f*WeaponUtility.GetCritDamageMultiplier(weapon) : 1f);
        if(Mathf.Abs(dc.damage-expected)>.02f) mismatch++; if(dc.crit) critical++; else normal++;
        Debug.Log($"Clank crit combat probe HIT: crit={dc.crit} actual={dc.damage:F2} expected={expected:F2}");
    }
    void OnDestroy() { Enemy.A_EnemySpawned-=OnSpawn; Enemy.A_Damage-=OnDamage; }
}
#endif
