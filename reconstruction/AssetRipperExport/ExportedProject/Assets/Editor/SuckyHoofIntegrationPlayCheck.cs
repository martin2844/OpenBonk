using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class SuckyHoofIntegrationPlayCheck
{
    const string Pending = "SuckyHoofIntegrationPlayCheck.pending";
    static SuckyHoofIntegrationPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("sucky hoof runner").AddComponent<SuckyHoofIntegrationRunner>().Begin(); } }
}
public class SuckyHoofIntegrationRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SuckyHoofIntegrationPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try
        {
            var state = new GameObject("state"); state.SetActive(false);
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.itemInventory = new ItemInventory(); inv.playerXp = new PlayerXp(); inv.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); inv.playerHealth.hp = 100;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.XpIncreaseMultiplier] = 1f }; inv.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.player = player;
            var data = state.AddComponent<DataManager>(); DataManager.Instance = data;
            var itemData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Sucky Magnet.asset");
            Check(itemData != null && itemData.eItem == EItem.SuckyMagnet, "original item data");
            typeof(DataManager).GetField("itemData", P).SetValue(data, new Dictionary<EItem,ItemData> { [EItem.SuckyMagnet] = itemData });
            var audio = state.AddComponent<AudioManager>(); AudioManager.Instance = audio; audio.xp = new GameObject("xp audio").AddComponent<AudioSource>();
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab"); Check(prefab != null, "original XP prefab");
            pool.xpPool = new ObjectPool<GameObject>(() => Instantiate(prefab), obj => obj.SetActive(true), obj => obj.SetActive(false));
            var pickupManager = new GameObject("pickup manager").AddComponent<PickupManager>(); pickupManager.whatIsPickups = 1 << prefab.layer;
            var effects = state.AddComponent<EffectManager>(); EffectManager.Instance = effects;
            XpUtility.Init(); MyTime.time = 1f; MyTime.paused = false;
            inv.itemInventory.AddItem(EItem.SuckyMagnet); Check(inv.itemInventory.GetItem(EItem.SuckyMagnet) is ItemSuckyHoof, "factory and inventory acquisition");
            Pickup xp = pickupManager.SpawnPickup(EPickup.Xp, Vector3.right * 40, 5, false); Check(xp != null && xp.collider.enabled, "original pooled pickup is initially available");
            Physics.SyncTransforms(); inv.itemInventory.Tick(); Check(!xp.collider.enabled, "inventory tick attracts original XP pickup");
            xp.transform.position = player.transform.position; typeof(Pickup).GetMethod("FixedUpdate", P).Invoke(xp, null); inv.LateUpdate();
            Check(inv.playerXp.xp == 5 && pool.xpPool.CountInactive == 1, "pickup awards XP and returns to pool");
            inv.itemInventory.RemoveItem(EItem.SuckyMagnet, false); Check(inv.itemInventory.GetAmount(EItem.SuckyMagnet) == 0, "item removal");
            inv.itemInventory.Cleanup(); Debug.Log("SuckyHoofIntegrationPlayCheck passed: real item acquisition, inventory tick, original pooled XP pickup, award and removal"); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
