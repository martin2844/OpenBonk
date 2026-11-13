using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in diagnostic: compare stationary motion with and without enemy collisions.
public sealed class AndroidStationaryProbe : MonoBehaviour
{
    private MyPlayer player;
    private Rigidbody body;
    private FieldInfo pushed;
    private int contacts;
    public static void Begin(MyPlayer player)
    {
        var component = player.playerMovement.GetRb().gameObject.AddComponent<AndroidStationaryProbe>();
        component.player = player;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (contacts++ >= 30) return;
        Debug.Log($"Stationary probe collision: object={collision.gameObject.name}, layer={LayerMask.LayerToName(collision.gameObject.layer)}, relative={collision.relativeVelocity}");
    }
    private IEnumerator Start()
    {
        body = player.playerMovement.GetRb();
        pushed = typeof(PlayerMovement).GetField("pushed", BindingFlags.NonPublic | BindingFlags.Instance);
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var invincibleUntil = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        var manager = EnemyManager.Instance; bool oldWaves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        int playerLayer = body.gameObject.layer, enemyLayer = LayerMask.NameToLayer("Enemy");
        bool validLayers = playerLayer >= 0 && enemyLayer >= 0;
        bool oldIgnore = validLayers && Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
        if (validLayers) Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        Vector3 origin = body.position;
        Debug.Log($"Stationary probe ready: origin={origin}, playerLayer={LayerMask.LayerToName(playerLayer)}, enemyLayer={enemyLayer}, previousIgnore={oldIgnore}");
        for (int i = 0; i <= 30 && player != null; i++)
        {
            invincible.SetValue(player, true); invincibleUntil.SetValue(player, float.MaxValue);
            if (i == 15 && validLayers)
            {
                Physics.IgnoreLayerCollision(playerLayer, enemyLayer, oldIgnore);
                var enemyData = DataManager.Instance != null ? DataManager.Instance.GetEnemyData(Actors.Enemies.EEnemy.Goblin) : null;
                int spawned = 0;
                if (enemyData != null)
                    foreach (Vector3 direction in new[] { Vector3.forward, Vector3.right, Vector3.left })
                        if (manager.SpawnEnemy(enemyData, body.position + direction * 5f, -1, true, canBeElite: false) != null) spawned++;
                Debug.Log($"Stationary probe: restored enemy collision, spawned={spawned}");
            }
            var movement = player.playerMovement;
            bool hit = Physics.Raycast(movement.GetRbFeetPosition() + Vector3.up * .5f, Vector3.down, out RaycastHit ground, 3f, movement.whatIsGround, QueryTriggerInteraction.Ignore);
            float x = MyInputManager.GetAxis(MyInputManager.MoveHorizontal), y = MyInputManager.GetAxis(MyInputManager.MoveVertical);
            Debug.Log($"Stationary probe sample: t={i}, pos={body.position}, moved={Vector3.Distance(origin, body.position):F2}, velocity={body.velocity}, input=({x:F3},{y:F3}), grounded={movement.grounded}, pushed={(bool)pushed.GetValue(movement)}, ground={(hit ? ground.collider.name : "none")}, normal={(hit ? ground.normal : Vector3.zero)}");
            yield return new WaitForSecondsRealtime(1f);
        }
        if (validLayers) Physics.IgnoreLayerCollision(playerLayer, enemyLayer, oldIgnore);
        if (player != null) { invincible.SetValue(player, false); invincibleUntil.SetValue(player, 0f); }
        manager.enabledWaves = oldWaves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable();
        Debug.Log("Stationary probe finished"); Destroy(this);
    }
}
