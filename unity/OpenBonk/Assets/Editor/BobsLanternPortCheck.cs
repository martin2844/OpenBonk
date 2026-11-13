using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class BobsLanternPortCheck
{
    const string Pending="BobsLanternPortCheck.pending";
    static BobsLanternPortCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
    static void State(PlayModeStateChange s) { if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("lantern check").AddComponent<BobsLanternPortRunner>().Begin(); } }
}
public class BobsLanternPortRunner:MonoBehaviour
{
    const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic; string error;
    void Capture(string m,string s,LogType t) { if(t==LogType.Exception || t==LogType.Error || t==LogType.Assert) error=m+"\n"+s; }
    static void Check(bool b,string s) { if(!b) throw new Exception("BobsLanternPortCheck: "+s); }
    static void Near(float a,float b,string s) { Check(Mathf.Abs(a-b)<.001f,s+": "+a+" expected "+b); }
    public void Begin()=>StartCoroutine(Guard());
    IEnumerator Guard() { var e=Run(); while(true) { bool more; try { more=e.MoveNext(); } catch(Exception x) { Debug.LogException(x); EditorApplication.Exit(1); yield break; } if(!more) yield break; yield return e.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived+=Capture; var root=new GameObject("state"); root.SetActive(false);
        var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; GameManager.Instance=root.AddComponent<GameManager>(); var manager=root.AddComponent<EnemyManager>(); EnemyManager.Instance=manager; var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
        var effects=root.AddComponent<EffectManager>(); EffectManager.Instance=effects; effects.lanternExplosion=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LanternExplosion.prefab"); Check(effects.lanternExplosion!=null,"original explosion prefab");
        var inv=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory=inv; inv.itemInventory=new ItemInventory(); var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Bobs Lantern.asset");
        if(asset==null) asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Bob's Lantern.asset"); Check(asset!=null && asset.eItem==EItem.BobsLantern,"original item asset"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
        LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string description=asset.GetDummyItem().GetDescription(asset.localizedDescription); Check(description.Contains("45s") && description.Contains("3s") && description.Contains("10m") && !description.Contains("{"),"description: "+description);
        MyTime.time=10f; MyTime.fixedDeltaTime=0f; inv.itemInventory.AddItem(EItem.BobsLantern,2); var item=inv.itemInventory.GetItem(EItem.BobsLantern); var explosionField=item.GetType().GetField("explosionPrefab",P);
        MyTime.time=11.99f; inv.itemInventory.Tick(); Check(explosionField.GetValue(item)==null,"two-second initial delay");
        MyTime.time=12f; inv.itemInventory.Tick(); var explosion=(GameObject)explosionField.GetValue(item); Check(explosion!=null && explosion.activeSelf,"creates active explosion"); var lantern=explosion.GetComponent<LanternExplosion>(); var killer=explosion.GetComponent<KillEnemyTrigger>(); Check(lantern!=null && killer!=null && killer.customDamageSource=="BobsLantern","original components and source");
        Near(((Vector3)typeof(LanternExplosion).GetField("desiredSize",P).GetValue(lantern)).x,70f,"two-item radius"); Near(explosion.transform.localScale.x,0f,"starts small");
        MyTime.fixedDeltaTime=.5f; typeof(LanternExplosion).GetMethod("FixedUpdate",P).Invoke(lantern,null); Near(explosion.transform.localScale.x,35f,"halfway expansion"); MyTime.fixedDeltaTime=0f;
        var enemyObj=new GameObject("boss target"); enemyObj.SetActive(false); var collider=enemyObj.AddComponent<CapsuleCollider>(); var enemy=enemyObj.AddComponent<Enemy>(); enemy.enabled=false; enemy.collider=collider; enemy.renderer=enemyObj.AddComponent<MeshRenderer>(); enemy.hp=enemy.maxHp=1000f; typeof(Enemy).GetField("controlHp",P).SetValue(enemy,1000f); typeof(Enemy).GetField("enemyFlag",P).SetValue(enemy,EEnemyFlag.Boss); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy,new object[]{ScriptableObject.CreateInstance<EnemyData>()}); enemy.rb=enemyObj.AddComponent<Rigidbody>(); enemy.enemyMovement=enemyObj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb=enemy.rb; enemy.enemyMovement.enemy=enemy; manager.collidersToEnemies.Add(collider,enemy);
        Check(killer.knockback==99999f,"prefab knockback preserved"); killer.knockback=0f; EffectManager.Instance=null; typeof(KillEnemyTrigger).GetMethod("OnTriggerEnter",P).Invoke(killer,new object[]{collider}); Near(enemy.hp,960f,"boss takes prefab's four-percent health damage");
        MyTime.fixedDeltaTime=.5f; typeof(LanternExplosion).GetMethod("FixedUpdate",P).Invoke(lantern,null); Check(!explosion.activeSelf,"explosion deactivates at lifetime"); MyTime.fixedDeltaTime=0f;
        MyTime.time=50.99f; inv.itemInventory.Tick(); Check(!explosion.activeSelf,"39-second cooldown"); MyTime.time=51f; inv.itemInventory.Tick(); Check(explosion.activeSelf && explosionField.GetValue(item)==explosion,"cooldown boundary reuses explosion");
        inv.itemInventory.AddItem(EItem.BobsLantern,30); Near(((Vector3)typeof(LanternExplosion).GetField("desiredSize",P).GetValue(lantern)).x,250f,"radius cap"); Near((float)item.GetType().GetField("cooldown",P).GetValue(item),5f,"cooldown floor"); Check(error==null,"unexpected log: "+error);
        Debug.Log("BobsLanternPortCheck passed: original item and prefab, delay, radius, boss trigger, cooldown, reuse and caps"); yield return null; EditorApplication.Exit(0);
    }
}
