using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

public static class ChallengeSmolPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var go = new GameObject("challenge size test");
        var capsule = go.AddComponent<CapsuleCollider>(); go.AddComponent<Rigidbody>();
        var player = go.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var movement = go.AddComponent<PlayerMovement>(); player.playerMovement = movement;
        player.feet = new GameObject("feet").transform; player.head = new GameObject("head").transform;
        player.feet.SetParent(go.transform); player.head.SetParent(go.transform);
        movement.feet = player.feet; movement.head = player.head; movement.orientation = new GameObject("orientation").transform; movement.orientation.SetParent(go.transform);
        player.playerRenderer = new GameObject("renderer").AddComponent<PlayerRenderer>(); player.playerRenderer.transform.SetParent(go.transform);
        player.character = ECharacter.Calcium; player.spawnDir = Vector3.forward;
        var data = ScriptableObject.CreateInstance<CharacterData>(); data.eCharacter = player.character; data.colliderHeight = 4f; data.colliderWidth = 0.8f;
        var managerObject = new GameObject("data manager"); var manager = managerObject.AddComponent<DataManager>(); DataManager.Instance = manager;
        var characters = (Dictionary<ECharacter, CharacterData>)typeof(DataManager).GetField("characterData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager); characters.Add(player.character, data);
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        typeof(MapController).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, inventory); player.inventory = inventory;
        player.RefreshSize(data, player.spawnDir); Check(Mathf.Approximately(capsule.height, 4f), "normal height");
        var modifier = ScriptableObject.CreateInstance<ChallengeModifierSmolBoi>(); modifier.Init(null);
        MyPlayer.A_PlayerInventoryInitialized?.Invoke(inventory);
        Check(Mathf.Approximately(capsule.height, 2f) && Mathf.Approximately(capsule.radius, .4f) && Mathf.Approximately(player.width, .4f), "half-size collider and width");
        Check(player.playerRenderer.transform.localScale == Vector3.one * .5f, "half-size renderer");
        Check(Mathf.Approximately(player.head.position.y - player.feet.position.y, 2f), "head and feet follow resized collider");
        modifier.Cleanup(); player.RefreshSize(data, player.spawnDir); MyPlayer.A_PlayerInventoryInitialized?.Invoke(inventory);
        Check(Mathf.Approximately(capsule.height, 4f) && player.playerRenderer.transform.localScale == Vector3.one, "cleanup removes resize callback");
        MyPlayer.Instance = null; DataManager.Instance = null; typeof(MapController).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
        UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(managerObject); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(modifier);
        Debug.Log("ChallengeSmolPortCheck passed: player event, collider, renderer, head and feet, cleanup");
    }
}
