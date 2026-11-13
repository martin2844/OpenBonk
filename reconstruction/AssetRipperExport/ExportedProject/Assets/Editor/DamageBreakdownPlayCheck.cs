using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DamageBreakdownPlayCheck
{
    const string Pending = "DamageBreakdownPlayCheck.pending", Path = "Assets/DamageBreakdownTemporary.unity";
    static DamageBreakdownPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<GameOverDamageSourcesUi>(FindObjectsInactive.Include);
        if (original == null || !original.damageSourceParent.IsChildOf(original.transform) || !original.damageSourcePrefab.transform.IsChildOf(original.transform)) throw new Exception("unexpected original damage panel hierarchy");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); var clone = UnityEngine.Object.Instantiate(original.gameObject); clone.SetActive(false); SceneManager.MoveGameObjectToScene(clone, scene);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, Path); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("damage breakdown runner").AddComponent<DamageBreakdownRunner>().Begin(); } }
}
public class DamageBreakdownRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    sealed class Provider : ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable { var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset"); return Addressables.ResourceManager.CreateCompletedOperation(table as T, null); } }
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("DamageBreakdownPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture; LocalizationSettings.StringDatabase.TableProvider = new Provider(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en");
        var state = new GameObject("state"); state.SetActive(false); var data = state.AddComponent<DataManager>(); DataManager.Instance = data;
        var fire = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponFireStaff.asset"); var bow = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBow.asset");
        var weapons = (Dictionary<EWeapon, WeaponData>)typeof(DataManager).GetField("weapons", Private).GetValue(data); weapons[EWeapon.FireStaff] = fire; weapons[EWeapon.Bow] = bow;
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.weaponInventory = new WeaponInventory(); player.inventory.itemInventory = new ItemInventory();
        var fireWeapon = (WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); fireWeapon.level = 3; player.inventory.weaponInventory.weapons[EWeapon.FireStaff] = fireWeapon;
        var bowWeapon = (WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase)); bowWeapon.level = 1; player.inventory.weaponInventory.weapons[EWeapon.Bow] = bowWeapon;
        var icons = state.AddComponent<IconManager>(); IconManager.Instance = icons; icons.poisonIcon = Texture2D.whiteTexture;
        RunStats.Init(); MyTime.time = 10f; Enemy.A_Damage?.Invoke(null, new DamageContainer(1, "Bow") { damage = 100 }); MyTime.time = 20f; Enemy.A_Damage?.Invoke(null, new DamageContainer(1, "FireStaff") { damage = 250 }); Enemy.A_Damage?.Invoke(null, new DamageContainer(1, "Poison") { damage = 120 }); Enemy.A_Damage?.Invoke(null, new DamageContainer(1, "Unknown") { damage = 999 });
        MyTime.time = 30f; var panel = FindAnyObjectByType<GameOverDamageSourcesUi>(FindObjectsInactive.Include); Check(panel != null, "original panel"); int before = panel.damageSourceParent.childCount; panel.gameObject.SetActive(true); yield return null;
        Check(panel.damageSourceParent.childCount == before + 3, "three valid rows and unknown filtered");
        var first = panel.damageSourceParent.GetChild(before).GetComponent<DamageSourceEntry>(); var second = panel.damageSourceParent.GetChild(before + 1).GetComponent<DamageSourceEntry>(); var third = panel.damageSourceParent.GetChild(before + 2).GetComponent<DamageSourceEntry>();
        Check(first != null && first.icon.texture == fire.GetIcon() && first.t_sourceName.text == fire.GetName() && first.t_lvl.text == "3" && first.t_dmg.text == "250" && first.t_dps.text == "25", "highest-damage weapon row");
        Check(second != null && second.icon.texture == icons.poisonIcon && second.t_lvl.text == "1" && second.t_dmg.text == "120" && second.t_dps.text == "12", "debuff row");
        Check(third != null && third.icon.texture == bow.GetIcon() && third.t_lvl.text == "1" && third.t_dmg.text == "100" && third.t_dps.text == "5", "sorted second weapon and elapsed DPS");
        Check(error == null, "unexpected Unity error: " + error); RunStats.Cleanup(); Application.logMessageReceived -= Capture;
        Debug.Log("DamageBreakdownPlayCheck passed: real damage events, original scene panel, sorted weapon/debuff rows, icons, levels and DPS"); EditorApplication.Exit(0);
    }
}
