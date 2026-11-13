using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BeaconHealingPlayCheck
{
    const string Pending = "BeaconHealingPlayCheck.pending";
    static BeaconHealingPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("beacon test runner").AddComponent<BeaconHealingPlayRunner>().Begin(); } }
}
public class BeaconHealingPlayRunner : MonoBehaviour
{
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception("BeaconHealingPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        HealingZone zone = null; PlayerHealth health = null; ItemBeacon beacon = null; ItemInventory items = null;
        try
        {
            MyTime.time = 0f; var root = new GameObject("synthetic runtime state"); root.SetActive(false);
            GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>(); MyPlayer.Instance.transform.position = new Vector3(100f,0f,0f);
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
            health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 100; health.maxHp = 200; inventory.playerHealth = health;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.HealingMultiplier] = 1f }; inventory.playerStats = stats;
            items = new ItemInventory(); inventory.itemInventory = items; items.AddItem(EItem.Beacon, 2); beacon = (ItemBeacon)items.GetItem(EItem.Beacon);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChargeShrine.prefab"); var shrine = Instantiate(prefab, root.transform).GetComponent<ChargeShrine>();
            var zoneObject = shrine.healingZone; Require(zoneObject != null && zoneObject.GetComponent<HealingZone>() != null, "original shrine healing zone"); zoneObject.transform.SetParent(null); zoneObject.transform.position = Vector3.zero; zoneObject.SetActive(true); zone = zoneObject.GetComponent<HealingZone>();
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(Mathf.Abs(beacon.GetRadius() - 12f) < .0001f && Mathf.Abs(beacon.GetHealingPerInterval() - 10f) < .0001f && Mathf.Abs(zone.transform.localScale.x - 12f) < .0001f, "two Beacons set original radius and healing");
            typeof(HealingZone).GetField("nextHealTime", Private).SetValue(zone, 0f); MyTime.time = 0f; MyPlayer.Instance.transform.position = new Vector3(12f,0f,0f);
            var tick = typeof(HealingZone).GetMethod("FixedUpdate", Private); tick.Invoke(zone, null); Require(health.hp == 110, "heals at radius boundary");
            MyTime.time = .25f; tick.Invoke(zone, null); Require(health.hp == 110, "healing interval prevents early repeat");
            MyTime.time = .5f; tick.Invoke(zone, null); Require(health.hp == 120, "heals again after interval");
            MyPlayer.Instance.transform.position = new Vector3(13f,0f,0f); MyTime.time = 1f; tick.Invoke(zone, null); Require(health.hp == 120, "does not heal outside radius");
            items.AddItem(EItem.Beacon);
            Require(Mathf.Abs(zone.transform.localScale.x - 14f) < .0001f && Mathf.Abs(beacon.GetHealingPerInterval() - 15f) < .0001f, "item update refreshes radius and healing");
            MyTime.time = 1.5f; tick.Invoke(zone, null); Require(health.hp == 135, "updated Beacon heals within expanded radius");
            items.RemoveItem(EItem.Beacon); Require(zone.gameObject.activeSelf && Mathf.Abs(zone.transform.localScale.x - 12f) < .0001f, "removing one Beacon updates zone");
            items.RemoveItem(EItem.Beacon); Require(zone.gameObject.activeSelf && Mathf.Abs(zone.transform.localScale.x - 10f) < .0001f, "removing second Beacon updates zone");
            items.RemoveItem(EItem.Beacon); Require(items.GetItem(EItem.Beacon) == null && !zone.gameObject.activeSelf, "removing final Beacon disables zone");
            Debug.Log("BeaconHealingPlayCheck passed: original shrine zone range, timed healing, item changes and removal"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
