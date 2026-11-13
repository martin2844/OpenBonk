#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

internal sealed class AndroidLampPoisonProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_lamp_poison_probe",false)) return;
        var root=new GameObject("lamp poison probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidLampPoisonProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance==null || DataManager.Instance==null)) yield return null;
        var player=MyPlayer.Instance; var manager=EnemyManager.Instance;
        if(player==null || player.inventory==null || manager==null || DataManager.Instance==null) { Debug.LogError("Lamp poison probe: gameplay unavailable"); yield break; }
        bool waves=manager.enabledWaves; manager.enabledWaves=false;
        var disabled=new List<WeaponBase>(); foreach(var weapon in player.inventory.weaponInventory.weapons.Values) if(weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var enemy=manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin),player.transform.position+player.transform.forward*6f,-1,true,canBeElite:false);
        deadline=Time.realtimeSinceStartup+15f;
        while(Time.realtimeSinceStartup<deadline && enemy!=null && !enemy.CanTakeDamage()) yield return null;
        if(enemy==null || !enemy.CanTakeDamage()) { Debug.LogError("Lamp poison probe: target unavailable"); Restore(); yield break; }
        enemy.SetDiagnosticHealth(10000f);
        var inventory=player.inventory.itemInventory;
        inventory.AddItem(EItem.MoldyCheese,5);
        var hit=new DamageContainer(1f,"test") { enemy=enemy,damage=1f,procCoefficient=1f };
        int Run() { enemy.debuffsToAdd.Clear(); enemy.DamageFromPlayerWeapon(hit); return enemy.debuffsToAdd.TryGetValue(EDebuff.Poison,out var pending)?pending.stacks:0; }
        int baseline=Run(); inventory.AddItem(EItem.OverpoweredLamp); int one=Run(); inventory.AddItem(EItem.OverpoweredLamp); int two=Run();
        hit.procCoefficient=0f; int zero=Run(); hit.procCoefficient=1f;
        inventory.RemoveItem(EItem.OverpoweredLamp,false); int afterOne=Run(); inventory.RemoveItem(EItem.OverpoweredLamp,false); int afterBoth=Run();
        bool ok=baseline==2 && one==4 && two==6 && zero==0 && afterOne==4 && afterBoth==2;
        Debug.Log($"Lamp poison probe {(ok?"PASS":"FAIL")}: baseline={baseline}, one={one}, two={two}, zeroCoefficient={zero}, afterOne={afterOne}, afterBoth={afterBoth}");
        enemy.debuffsToAdd.Clear(); for(int i=0;i<5;i++) inventory.RemoveItem(EItem.MoldyCheese,false); Restore();
        void Restore() { if(manager!=null) manager.enabledWaves=waves; foreach(var weapon in disabled) if(weapon!=null) weapon.Enable(); }
    }
}
#endif
