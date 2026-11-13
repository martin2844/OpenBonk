using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AuraControllerPlayCheck
{
    const string Pending = "AuraControllerPlayCheck.pending";
    static AuraControllerPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("aura runner").AddComponent<AuraControllerPlayRunner>().Begin(); } }
}
public class AuraProbe : ConstantAttack
{
    public int initCount, statCount, weaponStatCount;
    protected override void Init() { initCount++; }
    protected override void OnWeaponStatUpdate(EStat stat, EWeapon weapon) { weaponStatCount++; }
    protected override void OnStatUpdate(EStat stat) { statCount++; }
    public override float GetAuraRotationSpeed() => 1f;
}
public class AuraControllerPlayRunner : MonoBehaviour
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("AuraControllerPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        AuraAttacks controller = null; PlayerInventory inv = null; WeaponBase weapon = null; Dictionary<EWeapon,ConstantAttack> auras = null; AuraProbe first = null;
        try
        {
            var root = new GameObject("player state"); root.SetActive(false); MyPlayer.Instance = root.AddComponent<MyPlayer>();
            MyPlayer.Instance.feet = new GameObject("feet").transform; var rendererObject = new GameObject("renderer"); rendererObject.SetActive(false); MyPlayer.Instance.playerRenderer = rendererObject.AddComponent<PlayerRenderer>();
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inv; inv.weaponInventory = new WeaponInventory();
            var template = new GameObject("aura prefab"); template.AddComponent<AuraProbe>();
            var data = ScriptableObject.CreateInstance<WeaponData>(); data.eWeapon = EWeapon.BlackHole; data.isAura = true; data.attack = template; data.Init(); weapon = new WeaponBase(data); inv.weaponInventory.weapons.Add(data.eWeapon, weapon);
            controller = new GameObject("aura controller").AddComponent<AuraAttacks>(); auras = (Dictionary<EWeapon,ConstantAttack>)typeof(AuraAttacks).GetField("auras", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
            MyTime.time = 1f;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(auras.Count == 1 && auras.ContainsKey(weapon.weaponData.eWeapon), "start refresh creates owned aura"); first = (AuraProbe)auras[weapon.weaponData.eWeapon];
            Require(first.weaponBase == weapon && first.initCount == 1 && first.transform.parent == MyPlayer.Instance.feet && Vector3.Distance(first.transform.localPosition, Vector3.up * .01f) < .0001f, "aura is initialized and attached at feet");
            var orient = typeof(AuraAttacks).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            Require(orient != null, "aura updates after the character renderer");
            for (int i = 0; i < 30; i++)
            {
                MyTime.time = .2f + i * .11f;
                MyPlayer.Instance.playerRenderer.transform.rotation = Quaternion.Euler(18f * Mathf.Sin(i * .2f), 42f + i * 7f, 25f * Mathf.Cos(i * .3f));
                orient.Invoke(controller, null);
                Require(Vector3.Angle(first.transform.up, MyPlayer.Instance.playerRenderer.transform.up) < .01f, "rotating aura follows changing ground-aligned axes");
            }
            var groundAxis = typeof(AuraAttacks).GetMethod("GroundAxis", BindingFlags.Static | BindingFlags.NonPublic);
            Require(groundAxis != null, "ground-axis resolver exists");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "aura slope fixture"; floor.layer = 8;
            floor.transform.position = Vector3.zero; floor.transform.localScale = new Vector3(10f, .5f, 10f);
            floor.transform.rotation = Quaternion.Euler(0f, 0f, 27f);
            Physics.SyncTransforms();
            var fallback = Vector3.up;
            var sampled = (Vector3)groundAxis.Invoke(null, new object[] { new Vector3(0f, 1f, 0f), fallback, (LayerMask)(1 << 8) });
            Require(Vector3.Angle(sampled, floor.transform.up) < .01f, "Aura samples the unsmoothed slope normal");
            var missed = (Vector3)groundAxis.Invoke(null, new object[] { new Vector3(0f, 1f, 0f), fallback, (LayerMask)(1 << 9) });
            Require(Vector3.Angle(missed, fallback) < .01f, "missing ground retains renderer axis");
            Destroy(floor);
            WeaponInventory.A_WeaponAdded?.Invoke(weapon); Require(auras.Count == 1 && first.initCount == 1, "duplicate weapon event does not duplicate aura");
            int oldStats = first.statCount, oldWeaponStats = first.weaponStatCount;
            PlayerStatsNew.A_StatUpdate?.Invoke(EStat.MaxHealth); WeaponBase.A_WeaponStatUpdate?.Invoke(EStat.DamageMultiplier, weapon.weaponData.eWeapon);
            Require(first.statCount == oldStats + 1 && first.weaponStatCount == oldWeaponStats + 1, "constant attack receives stat events");
            inv.weaponInventory.ToggleWeapon(weapon.weaponData.eWeapon, false); Require(auras.Count == 0, "weapon disable removes aura");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            inv.weaponInventory.ToggleWeapon(weapon.weaponData.eWeapon, true); Require(auras.Count == 1, "weapon enable recreates aura");
            var second = (AuraProbe)auras[weapon.weaponData.eWeapon]; Require(second != first && second.initCount == 1, "new aura initialized once");
            MyPlayer.A_PlayerInventoryInitialized?.Invoke(inv); Require(auras.Count == 1, "inventory refresh remains idempotent");
            Debug.Log("AuraControllerPlayCheck passed: aura lifecycle, stat events, toggle and refresh"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
