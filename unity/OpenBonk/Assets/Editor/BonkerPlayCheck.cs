using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class BonkerPlayCheck
{
    const string Pending = "BonkerPlayCheck.pending";
    static BonkerPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("bonker test runner").AddComponent<BonkerPlayRunner>().Begin(); } }
}
public class BonkerPlayRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool b, string m) { if (!b) throw new Exception("BonkerPlayCheck: " + m); }
    static void Near(float a, float b, string m) { Check(Mathf.Abs(a-b)<.001f, m+": "+a+" expected "+b); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test=Run(); while(true) { bool more; try { more=test.MoveNext(); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if(!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var root = new GameObject("test state"); root.SetActive(false);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save=root.AddComponent<SaveManager>(); save.Init(); save.config=new ConfigSaveFile(); save.config.cfVisualsSettings.damage_numbers=0;
        var player=root.AddComponent<MyPlayer>(); MyPlayer.Instance=player; player.playerRenderer=root.AddComponent<PlayerRenderer>(); player.inventory=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory=new ItemInventory();
        var stats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats=new Dictionary<EStat,float>(); foreach(EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat]=1f; stats.stats[EStat.KnockbackMultiplier]=0f; player.inventory.playerStats=stats;
        var data=root.AddComponent<DataManager>(); DataManager.Instance=data; var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Bonker.asset"); Check(asset!=null && asset.eItem==EItem.Bonker,"original item"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
        var pool=root.AddComponent<PoolManager>(); PoolManager.Instance=pool; pool.bonkFx=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BonkEffect.prefab"); Check(pool.bonkFx!=null,"original effect prefab"); typeof(PoolManager).GetMethod("Start",P).Invoke(pool,null);
        var origin=CreateEnemy(enemies,Vector3.zero); var near=CreateEnemy(enemies,new Vector3(3f,0f,0f)); var far=CreateEnemy(enemies,new Vector3(10f,0f,0f)); Physics.SyncTransforms();
        LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string desc=asset.GetDummyItem().GetDescription(asset.localizedDescription); Check(desc.Contains("2%") && desc.Contains("20x") && !desc.Contains("{"),"description: "+desc);
        player.inventory.itemInventory.AddItem(EItem.Bonker); var item=player.inventory.itemInventory.GetItem(EItem.Bonker); var type=item.GetType(); Near((float)type.GetField("radius",P).GetValue(item),7f,"first radius"); Near((float)type.GetField("chance",P).GetValue(item),.02f,"first chance"); Check(item.HasOnHitEffectProc(),"registered for hit effects");
        var dc=new DamageContainer(0f,"test") { damage=10f, enemy=origin, direction=Vector3.forward }; item.ProcOnHitEffects(dc); Near(near.hp,1000f,"zero proc coefficient");
        int direct=0, area=0; Action<Enemy,DamageContainer> observer=(e,d)=> { if(d.damageSource=="Bonker") { Near(d.procCoefficient,0f,"no recursive proc"); if(d.damageEffect==Assets.Scripts.Game.Combat.EDamageEffect.Bonk) { direct++; Near(d.damage,200f,"direct damage"); } else { area++; Near(d.damage,10f,"area damage"); } } }; Enemy.A_Damage+=observer;
        dc.procCoefficient=100f; item.ProcOnHitEffects(dc); Near(origin.hp,790f,"origin direct and splash damage"); Near(near.hp,990f,"nearby damage"); Near(far.hp,1000f,"outside radius"); Check(direct==1 && area==2 && pool.bonkPool.CountActive==1,"damage events and bonk effect");
        var counter=type.GetField("numProcsThisTick",P); counter.SetValue(item,4); item.ProcOnHitEffects(dc); Check(direct==2 && area==2,"fifth proc direct only"); item.Tick(); item.ProcOnHitEffects(dc); Check(direct==3 && area==4,"tick resets splash cap");
        player.inventory.itemInventory.AddItem(EItem.Bonker); Near((float)type.GetField("chance",P).GetValue(item),.035f,"stacked chance"); Near((float)type.GetField("damageMultiplier",P).GetValue(item),30f,"stacked direct multiplier"); Near((float)type.GetField("radius",P).GetValue(item),8f,"stacked radius"); player.inventory.itemInventory.AddItem(EItem.Bonker,10); Near((float)type.GetField("radius",P).GetValue(item),10f,"radius cap");
        Enemy.A_Damage-=observer; Debug.Log("BonkerPlayCheck passed: item localization, chance, direct and area damage, effect pool, stacking and splash limit"); EditorApplication.Exit(0); yield break;
    }
    static Enemy CreateEnemy(EnemyManager manager,Vector3 position)
    {
        var o=new GameObject("bonker target"); o.SetActive(false); o.layer=LayerMask.NameToLayer("Enemy"); o.transform.position=position; var col=o.AddComponent<CapsuleCollider>(); col.radius=.5f; col.height=2f; var e=o.AddComponent<Enemy>(); e.enabled=false; e.collider=col; e.renderer=o.AddComponent<MeshRenderer>(); e.hp=e.maxHp=1000f; typeof(Enemy).GetField("controlHp",P).SetValue(e,1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(e,new object[]{ScriptableObject.CreateInstance<EnemyData>()}); manager.collidersToEnemies.Add(col,e); o.SetActive(true); return e;
    }
}
