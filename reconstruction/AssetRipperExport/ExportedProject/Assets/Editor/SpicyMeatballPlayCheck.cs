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
public static class SpicyMeatballPlayCheck
{
    const string Pending = "SpicyMeatballPlayCheck.pending";
    static SpicyMeatballPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("meatball test runner").AddComponent<SpicyMeatballPlayRunner>().Begin(); } }
}
public class SpicyMeatballPlayRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool b, string m) { if (!b) throw new Exception("SpicyMeatballPlayCheck: " + m); }
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
        var data=root.AddComponent<DataManager>(); DataManager.Instance=data; var asset=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Spicy Meatball.asset"); Check(asset!=null && asset.eItem==EItem.SpicyMeatball,"original item"); ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",P).GetValue(data)).Add(asset.eItem,asset);
        var pool=root.AddComponent<PoolManager>(); PoolManager.Instance=pool; pool.spicyMeatballFx=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BombExplosion.prefab"); Check(pool.spicyMeatballFx!=null,"original effect prefab"); typeof(PoolManager).GetMethod("Start",P).Invoke(pool,null);
        var origin=CreateEnemy(enemies,Vector3.zero); var near=CreateEnemy(enemies,new Vector3(3f,0f,0f)); var far=CreateEnemy(enemies,new Vector3(10f,0f,0f)); Physics.SyncTransforms();
        LocalizationSettings.SelectedLocale=AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string desc=asset.GetDummyItem().GetDescription(asset.localizedDescription); Check(desc.Contains("25%") && desc.Contains("65%") && !desc.Contains("{"),"description: "+desc);
        player.inventory.itemInventory.AddItem(EItem.SpicyMeatball); var item=player.inventory.itemInventory.GetItem(EItem.SpicyMeatball); var radius=item.GetType().GetField("radius",P); Near((float)radius.GetValue(item),4f,"first radius"); Check(item.HasOnHitEffectProc(),"registered for hit effects");
        var dc=new DamageContainer(0f,"test") { damage=20f, enemy=origin }; item.ProcOnHitEffects(dc); Near(near.hp,1000f,"zero proc coefficient");
        int events=0; Action<Enemy,DamageContainer> observer=(e,d)=> { if(d.damageSource=="SpicyMeatball") { events++; Near(d.damage,13f,"spread damage"); Near(d.procCoefficient,0f,"no recursive proc"); } }; Enemy.A_Damage+=observer;
        dc.procCoefficient=4f; item.ProcOnHitEffects(dc); Near(origin.hp,987f,"origin damage"); Near(near.hp,987f,"nearby damage"); Near(far.hp,1000f,"outside radius"); Check(events==2 && pool.spicyMeatballPool.CountActive==1,"damage events and explosion effect");
        var counter=item.GetType().GetField("numProcsThisTick",P); counter.SetValue(item,50); item.ProcOnHitEffects(dc); Check(events==2,"per-tick cap"); item.Tick(); item.ProcOnHitEffects(dc); Check(events==4,"tick resets cap");
        stats.stats[EStat.SizeMultiplier]=2f; player.inventory.itemInventory.AddItem(EItem.SpicyMeatball); Near((float)radius.GetValue(item),8f,"size and radius cap"); player.inventory.itemInventory.RemoveItem(EItem.SpicyMeatball); Near((float)radius.GetValue(item),8f,"radius after amount change");
        Enemy.A_Damage-=observer; Debug.Log("SpicyMeatballPlayCheck passed: item localization, proc chance, area damage, effect pool, radius and per-tick limit"); EditorApplication.Exit(0); yield break;
    }
    static Enemy CreateEnemy(EnemyManager manager,Vector3 position)
    {
        var o=new GameObject("meatball target"); o.SetActive(false); o.layer=LayerMask.NameToLayer("Enemy"); o.transform.position=position; var col=o.AddComponent<CapsuleCollider>(); col.radius=.5f; col.height=2f; var e=o.AddComponent<Enemy>(); e.enabled=false; e.collider=col; e.renderer=o.AddComponent<MeshRenderer>(); e.hp=e.maxHp=1000f; typeof(Enemy).GetField("controlHp",P).SetValue(e,1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(e,new object[]{ScriptableObject.CreateInstance<EnemyData>()}); manager.collidersToEnemies.Add(col,e); o.SetActive(true); return e;
    }
}
