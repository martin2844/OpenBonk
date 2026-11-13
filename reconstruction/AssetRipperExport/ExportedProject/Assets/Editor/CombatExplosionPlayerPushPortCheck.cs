using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEngine;

public static class CombatExplosionPlayerPushPortCheck
{
    public static void Run()
    {
        var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance;
        var gameObject = new GameObject("explosion game"); gameObject.SetActive(false);
        var playerObject = new GameObject("explosion player");
        GameObject explosionObject = null;
        try
        {
            GameManager.Instance = gameObject.AddComponent<GameManager>(); GameManager.Instance.whatIsPlayer = 1;
            var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var body = playerObject.AddComponent<Rigidbody>(); body.useGravity = false;
            playerObject.AddComponent<SphereCollider>().radius = .25f;
            var movement = playerObject.AddComponent<PlayerMovement>(); player.playerMovement = movement;
            typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, body);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/AmericanExplosion.prefab");
            explosionObject = UnityEngine.Object.Instantiate(prefab); explosionObject.transform.position = Vector3.zero;
            var explosion = explosionObject.GetComponent<CombatExplosion>();
            if (explosion == null || explosion.playerKnockback != 35f || explosion.radius != 8f) throw new Exception("Original explosion prefab parameters missing");
            explosion.playerDamage = 0f; explosion.enemyKnockback = 0f;
            var start = typeof(CombatExplosion).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            playerObject.transform.position = Vector3.right * 2f; Physics.SyncTransforms();
            start.Invoke(explosion, null);
            Vector3 expected = (Vector3.right + Vector3.up * .75f).normalized * 35f;
            if (Vector3.Distance(body.velocity, expected) > .001f) throw new Exception("Explosion push differs from original: " + body.velocity);
            body.velocity = Vector3.zero; playerObject.transform.position = Vector3.zero; Physics.SyncTransforms();
            start.Invoke(explosion, null);
            if (Vector3.Distance(body.velocity, Vector3.up * 35f) > .001f) throw new Exception("Coincident player must be pushed upward");
            body.velocity = Vector3.zero; playerObject.transform.position = Vector3.right * 20f; Physics.SyncTransforms();
            start.Invoke(explosion, null);
            if (body.velocity != Vector3.zero) throw new Exception("Player outside radius was pushed");
            Debug.Log("CombatExplosionPlayerPushPortCheck passed");
        }
        finally
        {
            GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer;
            UnityEngine.Object.DestroyImmediate(explosionObject); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
