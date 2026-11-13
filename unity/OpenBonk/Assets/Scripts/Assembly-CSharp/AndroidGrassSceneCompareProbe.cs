#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in only: keep the scene still and temporarily mark the existing batched grass.
internal sealed class AndroidGrassSceneCompareProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_grass_scene_compare_probe", false)) return;
        var root = new GameObject("grass scene compare probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidGrassSceneCompareProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        GrassChunkManager grass = null;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || (grass = FindObjectOfType<GrassChunkManager>()) == null)) yield return null;
        if (grass == null || MyPlayer.Instance == null || EnemyManager.Instance == null) { Debug.LogError("Grass scene compare: unavailable"); yield break; }
        var player = MyPlayer.Instance; var enemies = EnemyManager.Instance;
        bool oldWaves = enemies.enabledWaves; enemies.enabledWaves = false;
        var weapons = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); weapons.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var body = player.playerMovement.GetRb(); var oldConstraints = body.constraints; body.velocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll;
        yield return new WaitForSecondsRealtime(4f);
        var fallback = typeof(GrassChunkManager).GetField("fallbackMaterial", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(grass) as Material;
        if (fallback == null || !fallback.HasProperty("_GrassDebug")) Debug.LogError("Grass scene compare: fallback unavailable");
        else
        {
            Debug.Log("Grass scene compare: baseline ready");
            yield return new WaitForSecondsRealtime(12f);
            fallback.SetFloat("_GrassDebug", 1f); Debug.Log("Grass scene compare: highlighted");
            yield return new WaitForSecondsRealtime(12f);
            fallback.SetFloat("_GrassDebug", 0f); Debug.Log("Grass scene compare: restored");
        }
        body.constraints = oldConstraints; invincible.SetValue(player, oldInvincible); enemies.enabledWaves = oldWaves;
        foreach (var weapon in weapons) if (weapon != null) weapon.Enable();
    }
}
#endif
