using System;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using UnityEngine;

public static class PlayerDeathAnimationPortCheck
{
    public static void Run()
    {
        var root = new GameObject("death animation test");
        var playerRenderer = root.AddComponent<PlayerRenderer>();
        var animator = root.AddComponent<Animator>();
        playerRenderer.animator = animator;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(PlayerRenderer).GetMethod("Awake", flags).Invoke(playerRenderer, null);
        animator.speed = 1f;
        PlayerHealth.A_Died?.Invoke();
        if (animator.speed != 0f) throw new Exception("Death event did not stop the animator");
        typeof(PlayerRenderer).GetMethod("OnDestroy", flags).Invoke(playerRenderer, null);
        animator.speed = 1f;
        PlayerHealth.A_Died?.Invoke();
        if (animator.speed != 1f) throw new Exception("Destroyed renderer still receives death events");
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("PlayerDeathAnimationPortCheck passed: death event stops animator and cleanup unsubscribes");
    }
}
