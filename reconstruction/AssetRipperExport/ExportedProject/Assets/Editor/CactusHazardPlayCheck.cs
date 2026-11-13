using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CactusHazardPlayCheck
{
    const string Pending = "CactusHazardPlayCheck.pending";
    static CactusHazardPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("cactus hazard runner").AddComponent<CactusHazardPlayRunner>().Begin(); } }
}
public class CactusHazardPlayRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception("CactusHazardPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        Action<PlayerHealth, DamageContainer, bool> observer = null;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var state = new GameObject("synthetic player state"); state.SetActive(false);
            GameManager.Instance = state.AddComponent<GameManager>(); MyPlayer.Instance = state.AddComponent<MyPlayer>();
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
            inventory.statusEffects = new PlayerStatusEffects();
            inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            inventory.playerStats.stats = new Dictionary<EStat, float> { [EStat.Armor] = 0, [EStat.Evasion] = 0, [EStat.DamageReductionMultiplier] = 0 };
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 100; inventory.playerHealth = health;
            int hits = 0; string source = ""; float force = 0; Vector3 direction = Vector3.zero;
            observer = (h, dc, shield) => { hits++; source = dc.damageSource; force = dc.knockback; direction = dc.direction; };
            PlayerHealth.A_TakeDamage += observer;
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Cactus1.prefab");
            var cactus = Instantiate(original); cactus.SetActive(false); cactus.transform.position = new Vector3(0, 100, 0);
            foreach (var component in cactus.GetComponentsInChildren<MonoBehaviour>(true)) if (!(component is DamagePlayerCollider)) component.enabled = false;
            foreach (var col in cactus.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            var hazard = cactus.GetComponent<DamagePlayerCollider>(); Check(hazard != null && hazard.damage == 1f && hazard.knockbackForce == 25f && hazard.refreshTime == .5f, "original cactus configuration");
            cactus.AddComponent<BoxCollider>(); cactus.layer = 0; cactus.SetActive(true);
            int playerLayer = LayerMask.NameToLayer("Player"); Physics.IgnoreLayerCollision(0, playerLayer, false);
            for (int i = 0; i < 4; i++)
            {
                MyTime.time = i == 0 ? 10f : i == 1 ? 10.2f : i == 2 ? 10.6f : 11.2f;
                var other = new GameObject("collision " + i); other.layer = i == 3 ? 0 : playerLayer;
                other.transform.position = cactus.transform.position + Vector3.right * .7f;
                other.AddComponent<SphereCollider>().radius = .5f;
                var rb = other.AddComponent<Rigidbody>(); rb.useGravity = false;
                Physics.SyncTransforms(); Physics.Simulate(.02f);
                if (i == 0) Check(hits == 1 && health.hp == 99 && source == "Cactus" && force == 25f && direction.x > 0 && direction.y > 0, "real collision damages and knocks back player");
                if (i == 1) Check(hits == 1, "cooldown blocks second collision");
                if (i == 2) Check(hits == 2 && health.hp == 98, "collision works after cooldown");
                if (i == 3) Check(hits == 2, "non-player collision ignored");
                DestroyImmediate(other);
            }
            Debug.Log("CactusHazardPlayCheck passed: original cactus damage and knockback, collision cooldown, player-layer gate"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally { if (observer != null) PlayerHealth.A_TakeDamage -= observer; }
    }
}
