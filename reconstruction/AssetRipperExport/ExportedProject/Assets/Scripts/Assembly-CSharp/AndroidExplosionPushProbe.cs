#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in controlled spawn of the real AmericanExplosion prefab in an isolated run.
internal sealed class AndroidExplosionPushProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_explosion_push_probe", false)) return;
        var go = new GameObject("Explosion push probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExplosionPushProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.playerMovement == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EffectManager.Instance == null || EffectManager.Instance.americanExplosionEffect == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.playerMovement == null || EffectManager.Instance == null || EffectManager.Instance.americanExplosionEffect == null || EnemyManager.Instance == null) { Debug.LogError("Explosion push probe: scene unavailable"); yield break; }
        var movement = player.playerMovement; var body = movement.GetRb();
        if (body == null) { Debug.LogError("Explosion push probe: body unavailable"); yield break; }
        bool oldWaves = EnemyManager.Instance.enabledWaves; EnemyManager.Instance.enabledWaves = false;
        bool oldMovement = movement.enabled; movement.enabled = false;
        body.velocity = Vector3.zero; Physics.SyncTransforms();
        Vector3 position = player.transform.position - Vector3.right * 2f;
        bool overlap = Physics.CheckSphere(position, 8f, GameManager.Instance.whatIsPlayer);
        var effect = Instantiate(EffectManager.Instance.americanExplosionEffect, position, Quaternion.identity);
        var explosion = effect.GetComponent<CombatExplosion>();
        Debug.Log($"Explosion push probe: spawned={explosion != null}, overlap={overlap}, radius={(explosion != null ? explosion.radius : -1f)}, force={(explosion != null ? explosion.playerKnockback : -1f)}");
        yield return null;
        yield return new WaitForEndOfFrame();
        Debug.Log($"Explosion push probe: velocity={body.velocity.ToString("F2")}, playerHp={(player.inventory != null ? player.inventory.playerHealth.hp : -1)}");
        movement.enabled = oldMovement; EnemyManager.Instance.enabledWaves = oldWaves;
    }
}
#endif
