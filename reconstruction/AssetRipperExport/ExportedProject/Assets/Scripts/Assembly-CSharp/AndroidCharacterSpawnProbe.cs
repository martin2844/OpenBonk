#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Observes the regular player-spawn path for a character selected by a diagnostic intent.
internal sealed class AndroidCharacterSpawnProbe : MonoBehaviour
{
    string requested;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        string name = intent.Call<string>("getStringExtra", "megabonk_spawn_character");
        if (string.IsNullOrEmpty(name)) return;
        var go = new GameObject("character spawn probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidCharacterSpawnProbe>().requested = name;
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || !player.hasStarted) { Debug.LogError("Character spawn probe FAIL " + requested + ": timeout"); yield break; }
        yield return new WaitForSecondsRealtime(1f);
        try {
            var data = DataManager.Instance.GetCharacterData(player.character); var inventory = player.inventory;
            var body = player.GetComponent<Rigidbody>(); var capsule = player.GetComponent<CapsuleCollider>();
            bool valid = player.character.ToString() == requested && data != null && inventory != null && inventory.passiveAbility != null &&
                inventory.weaponInventory != null && data.weapon != null && inventory.weaponInventory.weapons.ContainsKey(data.weapon.eWeapon) &&
                player.playerRenderer != null && player.playerRenderer.renderer != null && player.playerRenderer.renderer.sharedMesh != null &&
                player.playerRenderer.animator != null && body != null && capsule != null && capsule.height > 0 && capsule.radius > 0 &&
                !float.IsNaN(player.baseMovementSpeed) && player.baseMovementSpeed > 0;
            Debug.Log($"Character spawn probe {(valid ? "PASS" : "FAIL")} requested={requested} actual={player.character} weapon={(data != null && data.weapon != null ? data.weapon.eWeapon.ToString() : "null")} passive={(inventory != null && inventory.passiveAbility != null ? inventory.passiveAbility.GetType().Name : "null")} speed={player.baseMovementSpeed} position={player.transform.position}");
        } catch (Exception e) { Debug.LogError("Character spawn probe FAIL " + requested + ": " + e); }
    }
}
#endif
