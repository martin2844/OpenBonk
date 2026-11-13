#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

// Disposable comparison with the original-PC controlled EntStage2 sample.
internal sealed class AndroidBossHpParityProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_boss_hp_parity_probe", false)) return;
        var root = new GameObject("Boss HP parity probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidBossHpParityProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance==null || EnemyManager.Instance.summonerController==null || DataManager.Instance==null || MapController.runConfig==null)) yield return null;
        var altar=FindFirstObjectByType<InteractableBossSpawner>();
        if(altar==null || EnemyManager.Instance==null || MyPlayer.Instance==null || DataManager.Instance==null) { Debug.LogError("Boss HP parity probe INCONCLUSIVE: setup unavailable");yield break; }
        EnemyManager.Instance.enabledWaves=false;
        foreach(var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) if(weapon.enabled) weapon.Disable();
        if(!altar.Interact()) { Debug.LogError("Boss HP parity probe INCONCLUSIVE: altar rejected");yield break; }
        var list=(List<Enemy>)typeof(InteractableBossSpawner).GetField("bossEnemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(altar);
        var data=DataManager.Instance.GetEnemyData((EEnemy)20);
        if(list==null || list.Count!=1 || data==null) { Debug.LogError("Boss HP parity probe INCONCLUSIVE: boss or data unavailable");yield break; }
        var boss=list[0];var prop=typeof(Enemy).GetProperty("enemyData");var setter=prop.GetSetMethod(true);
        var oldData=boss.enemyData;int oldStage=MapController.index,oldTier=MapController.runConfig.mapTierIndex;
        float oldTime=MyTime.difficultyTimer,oldSwarm=MyTime.finalSwarmTimer;
        try
        {
            setter.Invoke(boss,new object[]{data});MapController.index=1;MapController.runConfig.mapTierIndex=2;MyTime.difficultyTimer=120f;MyTime.finalSwarmTimer=0f;
            float difficulty=PlayerStats.GetStat(EStat.Difficulty),hpStat=PlayerStats.GetStat(EStat.EnemyHpMultiplier),scaling=PlayerStats.GetStat(EStat.EnemyScalingMultiplier);
            float hp=EnemyStats.GetHp(boss);
            bool pass=(int)data.enemyName==20 && data.hp==8000 && boss.IsBoss() && difficulty==0f && hpStat==1f && scaling==1f && Mathf.Abs(hp-592200f)<.1f;
            Debug.Log($"Boss HP parity probe {(pass ? "PASS" : "INCONCLUSIVE")}: enemy={(int)data.enemyName} baseHp={data.hp} stage=1 tier=2 difficultyTime=120 swarmTime=0 difficulty={difficulty:R} hpStat={hpStat:R} scaling={scaling:R} hp={hp:R}");
        }
        finally { setter.Invoke(boss,new object[]{oldData});MapController.index=oldStage;MapController.runConfig.mapTierIndex=oldTier;MyTime.difficultyTimer=oldTime;MyTime.finalSwarmTimer=oldSwarm; }
    }
}
#endif
