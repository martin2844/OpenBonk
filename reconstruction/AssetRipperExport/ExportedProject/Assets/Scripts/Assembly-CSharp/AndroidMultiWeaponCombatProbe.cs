#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable opt-in combat: simultaneous attacks and item procs, normal waves.
internal sealed class AndroidMultiWeaponCombatProbe : MonoBehaviour
{
    readonly Dictionary<string,int> sources = new Dictionary<string,int>();
    int hits,deaths,spawned;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_multi_weapon_combat_probe",false)) return;
        var root=new GameObject("Multi-weapon combat probe");DontDestroyOnLoad(root);root.AddComponent<AndroidMultiWeaponCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance==null || DataManager.Instance==null || SaveManager.Instance==null)) yield return null;
        var player=MyPlayer.Instance;
        if(player==null || player.inventory==null || EnemyManager.Instance==null || DataManager.Instance==null) { Debug.LogError("Multi-weapon combat probe INCONCLUSIVE: game unavailable");yield break; }
        typeof(MyPlayer).GetField("isInvincible",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,float.MaxValue);
        var settings=SaveManager.Instance.config.cfGameSettings; settings.auto_select_upgrades=1;settings.auto_select_after_level=0;
        var inventory=player.inventory;
        foreach(var weapon in new[]{EWeapon.Aura,EWeapon.Frostwalker,EWeapon.PoisonFlask,EWeapon.Revolver,EWeapon.LightningStaff,EWeapon.Flamewalker,EWeapon.Aegis,EWeapon.BlackHole}) {
            var data=DataManager.Instance.GetWeapon(weapon);
            if(data==null) { Debug.LogError("Multi-weapon combat probe INCONCLUSIVE: missing weapon "+weapon);yield break; }
            if(!inventory.weaponInventory.weapons.ContainsKey(weapon)) inventory.weaponInventory.AddWeapon(data,null);
        }
        foreach(var item in new[]{EItem.MoldyCheese,EItem.LightningOrb,EItem.Dragonfire,EItem.GrandmasSecretTonic,EItem.SpicyMeatball,EItem.GiantFork,EItem.Battery,EItem.GymSauce}) inventory.itemInventory.AddItem(item);
        Enemy.A_EnemySpawned+=OnSpawn;Enemy.A_Damage+=OnDamage;Enemy.A_EnemyDied+=OnDeath;
        Debug.Log($"Multi-weapon combat probe START: weapons={inventory.weaponInventory.weapons.Count} grantedItems=8 autoSelect=true");
        float start=Time.realtimeSinceStartup;deadline=start+120f;
        while(Time.realtimeSinceStartup<deadline && MyPlayer.Instance==player && GameManager.Instance!=null && GameManager.Instance.IsPlaying()) yield return null;
        bool valid=Time.realtimeSinceStartup-start>=115f && spawned>10 && deaths>5 && hits>50 && sources.Count>=4;
        Debug.Log($"Multi-weapon combat probe {(valid ? "OBSERVED" : "INCONCLUSIVE")}: seconds={Time.realtimeSinceStartup-start:F1} spawned={spawned} deaths={deaths} hits={hits} sources={sources.Count} level={inventory.playerXp.level}");
        foreach(var pair in sources) Debug.Log($"Multi-weapon combat source: {pair.Key} hits={pair.Value}");
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if(enemy!=null) spawned++; }
    void OnDamage(Enemy enemy,DamageContainer damage) { if(enemy==null || damage==null) return; hits++;string source=damage.damageSource ?? "<null>";sources.TryGetValue(source,out int count);sources[source]=count+1; }
    void OnDeath(Enemy enemy,DamageContainer damage) { if(enemy!=null) deaths++; }
    void Unsubscribe(){Enemy.A_EnemySpawned-=OnSpawn;Enemy.A_Damage-=OnDamage;Enemy.A_EnemyDied-=OnDeath;}
    void OnDestroy()=>Unsubscribe();
}
#endif
