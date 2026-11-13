using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class SilverPickupAudioPlayCheck
{
    const string Pending = "SilverPickupAudioPlayCheck.pending";
    static SilverPickupAudioPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("Silver audio check").AddComponent<SilverPickupAudioRunner>().Begin(); } }
}

public class SilverPickupAudioRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("SilverPickupAudioPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            var root = new GameObject("controlled managers"); root.SetActive(false);
            var audio = root.AddComponent<AudioManager>(); audio.enabled = false;
            audio.xp = root.AddComponent<AudioSource>(); audio.gold = root.AddComponent<AudioSource>();
            audio.silver = root.AddComponent<AudioSource>(); audio.silver.clip = AudioClip.Create("silver test", 441000, 1, 44100, false);
            var gm = root.AddComponent<GameManager>(); gm.enabled = false;
            var player = root.AddComponent<MyPlayer>(); player.enabled = false;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
            player.inventory.playerHealth.hp = 0; // Avoid writing to a save during this audio-path test.
            var manager = root.AddComponent<PickupManager>(); manager.enabled = false;
            var pool = root.AddComponent<PoolManager>(); pool.enabled = false;
            root.SetActive(true);
            AudioManager.Instance = audio; GameManager.Instance = gm; MyPlayer.Instance = player;
            PickupManager.Instance = manager; PoolManager.Instance = pool;
            var obj = new GameObject("controlled silver pickup"); obj.SetActive(false);
            var pickup = obj.AddComponent<Pickup>(); pickup.collider = obj.AddComponent<SphereCollider>(); pickup.ePickup = EPickup.Silver;
            pool.powerupPool = new ObjectPool<GameObject>(() => obj, go => go.SetActive(true), go => go.SetActive(false));
            pool.powerupPool.Get();
            typeof(Pickup).GetMethod("ApplyPickup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pickup, null);
            Require(audio.silver.isPlaying, "silver audio did not play");
            Require(pool.powerupPool.CountInactive == 1 && !obj.activeSelf, "pickup not returned to pool");
            Debug.Log("SilverPickupAudioPlayCheck passed: silver pickup starts audio and returns to pool with controlled player");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
