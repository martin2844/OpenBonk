#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Runs only when explicitly launched with megabonk_world_collider_probe in an isolated test run.
internal sealed class AndroidWorldColliderRecoveryProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_world_collider_probe", false)) return;
        var obj = new GameObject("World collider recovery probe"); DontDestroyOnLoad(obj); obj.AddComponent<AndroidWorldColliderRecoveryProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.playerMovement == null || MyPlayer.Instance.playerInput == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || !player.hasStarted || player.isTeleporting || player.playerMovement == null || player.playerInput == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null) { Debug.LogError("World collider probe: gameplay did not become ready"); yield break; }
        var movement = player.playerMovement; var body = movement.GetRb(); var playerCollider = movement.GetPlayerCollider();
        var world = FindAnyObjectByType<WorldColliderTrigger>(); var worldCollider = world != null ? world.GetComponent<Collider>() : null;
        if (body == null || playerCollider == null || worldCollider == null || !worldCollider.enabled) { Debug.LogError("World collider probe: actual colliders unavailable"); yield break; }
        float groundedDeadline = Time.realtimeSinceStartup + 15f;
        while (!movement.grounded && Time.realtimeSinceStartup < groundedDeadline) yield return null;
        if (!movement.grounded) { Debug.LogError("World collider probe: initial player ground state unavailable"); yield break; }
        Vector3 safe = movement.lastGroundedPosition;
        Bounds bounds = worldCollider.bounds;
        Vector3 castOrigin = new Vector3(bounds.center.x, bounds.max.y + 10f, bounds.center.z);
        if (!worldCollider.Raycast(new Ray(castOrigin, Vector3.down), out RaycastHit hit, 20f + bounds.size.y) || Vector3.Distance(safe, hit.point) < 25f)
        { Debug.LogError($"World collider probe: unsuitable actual collider or safe point; bounds={bounds}, safe={safe:F2}"); yield break; }
        int playerLayer = LayerMask.NameToLayer("Player"), groundLayer = LayerMask.NameToLayer("Ground"), enemyLayer = LayerMask.NameToLayer("Enemy");
        bool oldGroundIgnore = Physics.GetIgnoreLayerCollision(playerLayer, groundLayer);
        bool oldEnemyIgnore = Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
        bool oldInputEnabled = player.playerInput.enabled, oldWaves = EnemyManager.Instance.enabledWaves, oldGravity = body.useGravity, oldKinematic = body.isKinematic;
        bool collisionRestored = false;
        try
        {
            EnemyManager.Instance.enabledWaves = false; player.playerInput.enabled = false;
            Physics.IgnoreLayerCollision(playerLayer, groundLayer, true); Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
            body.isKinematic = false; body.useGravity = true; body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            Vector3 testPosition = hit.point + Vector3.up * (playerCollider.bounds.extents.y + 0.8f);
            body.position = testPosition; Physics.SyncTransforms();
            Debug.Log($"World collider probe: actual={world.name}, collider={worldCollider.GetType().Name}, safe={safe:F2}, surface={hit.point:F2}, start={testPosition:F2}");
            float recoverDeadline = Time.realtimeSinceStartup + 12f; bool recovered = false;
            while (Time.realtimeSinceStartup < recoverDeadline)
            {
                yield return new WaitForFixedUpdate();
                if (Vector3.Distance(body.position, safe) < 24f && Vector3.Distance(body.position, hit.point) > 25f) { recovered = true; break; }
            }
            Physics.IgnoreLayerCollision(playerLayer, groundLayer, oldGroundIgnore); collisionRestored = true;
            Debug.Log($"World collider probe: recovered={recovered}, position={body.position:F2}, saved={safe:F2}, velocity={body.velocity:F2}");
            if (!recovered) yield break;
            // Teleporting sets velocity to zero, and the old grounded flag can linger.
            // Require a real ground ray and allow time for physics to resume.
            yield return new WaitForSecondsRealtime(0.3f);
            float landDeadline = Time.realtimeSinceStartup + 10f; bool landed = false;
            while (Time.realtimeSinceStartup < landDeadline)
            {
                yield return new WaitForFixedUpdate();
                Bounds playerBounds = playerCollider.bounds;
                bool groundBelow = Physics.Raycast(playerBounds.center, Vector3.down, playerBounds.extents.y + 0.3f, movement.whatIsGround, QueryTriggerInteraction.Ignore);
                if (groundBelow && movement.grounded && Mathf.Abs(body.velocity.y) < 1f && Vector3.Distance(body.position, safe) < 25f) { landed = true; break; }
            }
            Debug.Log($"World collider probe: landed={landed}, grounded={movement.grounded}, position={body.position:F2}, velocity={body.velocity:F2}");
        }
        finally
        {
            if (!collisionRestored) Physics.IgnoreLayerCollision(playerLayer, groundLayer, oldGroundIgnore);
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, oldEnemyIgnore);
            body.useGravity = oldGravity; body.isKinematic = oldKinematic; player.playerInput.enabled = oldInputEnabled;
            if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = oldWaves;
        }
    }
}
#endif
