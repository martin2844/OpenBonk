using System;
using System.Collections;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CryptKeyPickupPlayCheck
{
    private const string Pending = "CryptKeyPickupPlayCheck.pending";
    static CryptKeyPickupPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("crypt key pickup check").AddComponent<CryptKeyPickupRunner>().StartCheck();
    }
}
public class CryptKeyPickupRunner : MonoBehaviour
{
    public void StartCheck() { StartCoroutine(Check()); }
    private IEnumerator Check()
    {
        ItemInventory inventory = null;
        GameObject pickup = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CoffinTile.prefab");
            var original = prefab.GetComponentInChildren<InteractableCageKey>(true);
            Require(original != null && original.eItem == EItem.CryptKey && original.fx != null, "original coffin key and effect");
            var player = new GameObject("test player"); player.SetActive(false);
            MyPlayer.Instance = player.AddComponent<MyPlayer>();
            MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            inventory = new ItemInventory(); MyPlayer.Instance.inventory.itemInventory = inventory;
            pickup = Instantiate(original.gameObject);
            var key = pickup.GetComponent<InteractableCageKey>();
            var fx = key.fx;
            int events = 0;
            Action<EItem> listener = item => { if (item == EItem.CryptKey) events++; };
            ItemInventory.A_ItemAdded += listener;
            Require(key.Interact(), "first interaction succeeds");
            Require(inventory.GetAmount(EItem.CryptKey) == 1 && events == 1, "key added to inventory with event");
            Require(fx.activeSelf && fx.transform.parent == null, "effect detached and activated");
            Require(!key.Interact() && inventory.GetAmount(EItem.CryptKey) == 1, "repeat does not add another key");
            ItemInventory.A_ItemAdded -= listener;
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(pickup == null, "pickup destroyed at end of frame");
            inventory.Cleanup();
            Debug.Log("CryptKeyPickupPlayCheck passed: original coffin prefab awards one crypt key");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
    private static void Require(bool value, string description) { if (!value) throw new Exception("CryptKeyPickupPlayCheck failed: " + description); }
}
