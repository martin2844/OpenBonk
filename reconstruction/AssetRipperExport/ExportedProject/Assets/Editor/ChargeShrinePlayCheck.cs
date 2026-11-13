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
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ChargeShrinePlayCheck
{
    const string Pending = "ChargeShrinePlayCheck.pending";
    static ChargeShrinePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("charge test runner").AddComponent<ChargeShrinePlayRunner>().Begin(); } }
}
public class ChargeShrineTestWindow : BaseEncounterWindow
{
    public EEncounter opened; public EncounterOffer[] offers;
    public override void Open(EEncounter type) { opened = type; offers = DataManager.Instance.GetEncounter(type).GetOffers(); }
    public override void OnClose() { }
    public override void ChooseOffer(int index) { }
}
public class ChargeShrinePlayRunner : MonoBehaviour
{
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception("ChargeShrinePlayCheck: " + message); }
    static void Call(ChargeShrine shrine, string name) => typeof(ChargeShrine).GetMethod(name, Private).Invoke(shrine, null);
    static float Field(ChargeShrine shrine, string name) => (float)typeof(ChargeShrine).GetField(name, Private).GetValue(shrine);
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        ChargeShrine shrine = null; EncounterWindows queue = null; ChargeShrineTestWindow window = null; int spawned = 0, charged = 0; bool uninterrupted = true;
        try
        {
            MyTime.time = 0f; MyTime.deltaTime = 0f; MyTime.paused = false;
            var root = new GameObject("synthetic runtime state"); root.SetActive(false);
            GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
            inventory.statInventory = new StatInventory();
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 100; health.maxHp = 100; inventory.playerHealth = health;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.Luck] = 1f }; inventory.playerStats = stats;
            var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
            var itemData = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", Private).GetValue(dataManager);
            itemData[EItem.Wrench] = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Wrench.asset");
            itemData[EItem.Beacon] = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Beacon.asset");
            Require(itemData[EItem.Wrench] != null && itemData[EItem.Beacon] != null, "original item assets");
            inventory.itemInventory = new ItemInventory(); inventory.itemInventory.AddItem(EItem.Wrench, 2); inventory.itemInventory.AddItem(EItem.Beacon);
            Require(inventory.itemInventory.GetItem(EItem.Wrench) is ItemWrench && inventory.itemInventory.GetAmount(EItem.Wrench) == 2, "Wrench acquired through inventory");
            var map = (Dictionary<EEncounter, EncounterData>)typeof(DataManager).GetField("encounterData", Private).GetValue(dataManager); map[EEncounter.RandomStats] = AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/MonoBehaviour/RandomStatsEncounter.asset");
            var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui; ui.scoreUi = root.AddComponent<ScoreUi>(); ui.shrineLogs = root.AddComponent<ShrineLogs>(); queue = root.AddComponent<EncounterWindows>(); ui.encounterWindows = queue; window = root.AddComponent<ChargeShrineTestWindow>(); queue.genericEncounterWindow = window;
            ChargeShrine.A_ChargeShrineSpawned += () => spawned++; ChargeShrine.A_Charged += value => { charged++; uninterrupted = value; };
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChargeShrine.prefab"); var instance = Instantiate(prefab, root.transform); instance.transform.SetParent(null); shrine = instance.GetComponent<ChargeShrine>();
            Require(shrine != null && shrine.zoneRenderer != null && shrine.circleProgress != null, "original prefab references");
            typeof(ChargeShrine).GetField("<isGolden>k__BackingField", Private).SetValue(shrine, true);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(spawned == 1 && !shrine.zoneRenderer.enabled && shrine.circleParent.alpha == 0f, "original start state and spawn event");
            Require(!shrine.CanInteract() && !shrine.Interact(), "charge shrine uses trigger, not interaction button");
            Physics.simulationMode = SimulationMode.Script;
            var playerProxy = new GameObject("synthetic player collider"); playerProxy.layer = LayerMask.NameToLayer("Player"); var playerCollider = playerProxy.AddComponent<SphereCollider>(); playerCollider.radius = .1f; var body = playerProxy.AddComponent<Rigidbody>(); body.isKinematic = true;
            playerProxy.transform.position = shrine.transform.position + Vector3.right * 100f; Physics.SyncTransforms(); Physics.Simulate(.02f);
            playerProxy.transform.position = shrine.transform.position; Physics.SyncTransforms(); Physics.Simulate(.02f);
            Require(shrine.zoneRenderer.enabled && shrine.circleParent.alpha == 1f, "physics trigger entry starts charge");
            Require(Mathf.Abs(Field(shrine, "currentChargeTime") - 4.6f) < .0001f, "Wrench reduces five second charge");
            MyTime.deltaTime = .5f;
            for (int i = 0; i < 4; i++) { MyTime.time += .5f; Call(shrine, "Update"); }
            Require(Mathf.Abs(Field(shrine, "chargeProgress") - 2f / 4.6f) < .001f, "charging progresses over time");
            playerProxy.transform.position = shrine.transform.position + Vector3.right * 100f; Physics.SyncTransforms(); Physics.Simulate(.02f); MyTime.time += .5f; Call(shrine, "Update");
            Require(Mathf.Abs(Field(shrine, "chargeProgress")) < .0001f, "exit decays charge four times faster");
            MyTime.time += .5f; Call(shrine, "Update");
            Require(Field(shrine, "chargeProgress") == 0f && !shrine.zoneRenderer.enabled, "empty charge clears indicators");
            playerProxy.transform.position = shrine.transform.position; Physics.SyncTransforms(); Physics.Simulate(.02f);
            for (int i = 0; i < 10; i++) { MyTime.time += .5f; Call(shrine, "Update"); }
            Require(charged == 1 && !uninterrupted && !shrine.minimapIcon.activeSelf && shrine.circleParent.alpha == 0f, "completion event records interruption and hides indicators");
            Require(shrine.healingZone.activeSelf && shrine.healingZone.transform.parent == null, "Beacon detaches and activates the original healing zone on completion");
            Require(!queue.encounterInProgress, "reward is delayed after completion");
            global::Utility.MyRandom.random = new System.Random(394); UnityEngine.Random.InitState(394);
            var baseOffers = EncounterUtility.GetRandomStatOffers(3, true);
            global::Utility.MyRandom.random = new System.Random(394); UnityEngine.Random.InitState(394);
            MyTime.time += .5f; Call(shrine, "Update");
            Require(queue.encounterInProgress && window.opened == EEncounter.RandomStats && ChargeShrine.lastRewardShrine == shrine && window.offers.Length == 3, "completed shrine queues original stat offers");
            for (int i = 0; i < window.offers.Length; i++)
            {
                var offer = window.offers[i]; var actual = offer.effects[0].statModifier; var baseline = baseOffers[i].effects[0].statModifier;
                Require(offer.rarity == ERarity.Legendary, "golden charge shrine forces legendary rewards");
                Require(actual.stat == baseline.stat && Mathf.Abs(actual.modification - baseline.modification * 1.15f) < .0001f, "two Wrenches scale generated reward");
            }
            Call(shrine, "Update"); Require(charged == 1 && window.offers.Length == 3, "completion and reward occur once");
            var selected = window.offers[1].effects[0].statModifier;
            var inventory = MyPlayer.Instance.inventory;
            inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
            inventory.tomeInventory = new TomeInventory(); inventory.statusEffects = new PlayerStatusEffects();
            inventory.playerStats = new PlayerStatsNew(inventory, true);
            var components = inventory.playerStats.statValuesMap[selected.stat];
            float flat = components.baseValue, additive = components.additiveValue, multiplier = components.multiplicativeValue;
            if (selected.modifyType == EStatModifyType.Flat) flat += selected.modification;
            else if (selected.modifyType == EStatModifyType.Addition) additive += selected.modification;
            else if (selected.modifyType == EStatModifyType.Multiplication) multiplier *= selected.modification;
            float expectedStat = flat * additive * multiplier;
            var uiObject = new GameObject("actual encounter handler"); uiObject.SetActive(false);
            var actualUi = uiObject.AddComponent<EncounterUi>(); actualUi.particles = new GameObject("particles");
            typeof(EncounterUi).GetField("offers", Private).SetValue(actualUi, window.offers);
            typeof(EncounterWindows).GetField("activeEncounterWindow", Private).SetValue(queue, actualUi);
            actualUi.ChooseOffer(1);
            var applied = MyPlayer.Instance.inventory.statInventory.permanentChanges;
            Require(applied.TryGetValue(selected.stat, out var changes) && changes.Count == 1 && ReferenceEquals(changes[0], selected), "actual choice applies exactly the Wrench-scaled reward");
            inventory.statInventory.Tick(); inventory.playerStats.Tick();
            Require(Mathf.Abs(inventory.playerStats.GetRawStat(selected.stat) - expectedStat) < .0001f, "selected reward reaches calculated player stat");
            Require(!queue.encounterInProgress && !MyTime.paused, "actual choice closes encounter and resumes gameplay");
            Require(UiManager.Instance.scoreUi.GetQueueCount() == 1, "actual choice queues the stat notification");
            inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy();
            Destroy(actualUi.particles); Destroy(uiObject);
            Debug.Log("ChargeShrinePlayCheck passed: original prefab charging, interruption, Wrench, completion and golden reward"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
