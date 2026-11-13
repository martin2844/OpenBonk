#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in diagnostic on a real generated map; it grants Aura and fixes the player on a slope.
internal sealed class AndroidAuraSlopeProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_aura_slope_probe", false)) return;
        var root = new GameObject("Aura slope probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidAuraSlopeProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || DataManager.Instance == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Aura slope probe INCONCLUSIVE: no player"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        var data = DataManager.Instance.GetWeapon(EWeapon.Aura);
        if (data == null) { Debug.LogError("Aura slope probe INCONCLUSIVE: no weapon"); yield break; }
        player.inventory.weaponInventory.AddWeapon(data, null);
        RaycastHit best = default; bool found = false; Vector3 start = player.transform.position;
        for (int radius = 5; radius <= 120 && !found; radius += 5)
        for (int a = 0; a < 36 && !found; a++)
        {
            float angle = a * Mathf.PI * 2f / 36f;
            var origin = start + new Vector3(Mathf.Cos(angle) * radius, 200f, Mathf.Sin(angle) * radius);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 400f, GameManager.Instance.whatIsGround) && hit.collider.name == "CombinedCollisionMesh" && !hit.collider.CompareTag("CameraIgnore"))
            {
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope >= 12f && slope <= 35f) { best = hit; found = true; }
            }
        }
        if (!found) { Debug.LogError("Aura slope probe INCONCLUSIVE: no suitable ground slope"); yield break; }
        var rb = player.GetComponent<Rigidbody>();
        if (rb == null) { Debug.LogError("Aura slope probe INCONCLUSIVE: no rigidbody"); yield break; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", flags).SetValue(player, float.PositiveInfinity);
        typeof(MyPlayer).GetField("isInvincible", flags).SetValue(player, true);
        var capsule = player.GetComponent<CapsuleCollider>();
        float centerHeight = capsule != null ? capsule.height * .5f : 1.5f;
        rb.position = best.point + Vector3.up * (centerHeight + .1f); player.transform.position = rb.position;
        rb.velocity = Vector3.zero; rb.constraints |= RigidbodyConstraints.FreezePosition; Physics.SyncTransforms();
        player.playerRenderer.ForceRotation(Vector3.forward);
        deadline = Time.realtimeSinceStartup + 30f;
        CombatAura aura = null;
        while (Time.realtimeSinceStartup < deadline && ((aura = FindFirstObjectByType<CombatAura>()) == null || Vector3.Angle(player.playerRenderer.transform.up, best.normal) > 5f)) yield return null;
        if (aura == null) { Debug.LogError("Aura slope probe INCONCLUSIVE: no aura"); yield break; }
        // Separate the renderer axis from the actual slope to exercise the new
        // grounded path; otherwise slow frames make both axes coincide.
        var auraController = FindFirstObjectByType<AuraAttacks>();
        var lateUpdate = typeof(AuraAttacks).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var originalPosition = rb.position;
        for (int i = 0; i < 8 && !player.playerMovement.IsTouchingGround(); i++)
        {
            rb.position -= Vector3.up * .05f; player.transform.position = rb.position; Physics.SyncTransforms();
        }
        bool grounded = player.playerMovement.IsTouchingGround();
        bool special = player.playerMovement.IsGrinding() || player.playerMovement.IsWallClimbing();
        var rendererTransformForBranch = player.playerRenderer.transform;
        var oldRotation = rendererTransformForBranch.rotation;
        if (auraController != null && lateUpdate != null && grounded && !special &&
            Physics.Raycast(rendererTransformForBranch.position + Vector3.up, Vector3.down, out var branchGround, 51f, GameManager.Instance.whatIsGround) &&
            !branchGround.collider.CompareTag("CameraIgnore"))
        {
            rendererTransformForBranch.rotation = Quaternion.identity;
            float rendererError = Vector3.Angle(rendererTransformForBranch.up, branchGround.normal);
            lateUpdate.Invoke(auraController, null);
            float groundError = Vector3.Angle(aura.transform.up, branchGround.normal);
            bool passBranch = rendererError > 10f && groundError < .1f;
            Debug.Log($"Aura grounded branch {(passBranch ? "PASS" : "FAIL")}: grounded={grounded}, rendererError={rendererError:F3}, groundError={groundError:F3}");
            rb.position = originalPosition + Vector3.up * 10f; player.transform.position = rb.position; Physics.SyncTransforms();
            rendererTransformForBranch.rotation = Quaternion.identity;
            bool airborne = !player.playerMovement.IsTouchingGround();
            lateUpdate.Invoke(auraController, null);
            float fallbackError = Vector3.Angle(aura.transform.up, rendererTransformForBranch.up);
            Debug.Log($"Aura airborne fallback {(airborne && fallbackError < .1f ? "PASS" : "FAIL")}: airborne={airborne}, rendererError={fallbackError:F3}");
        }
        else Debug.LogError($"Aura grounded branch INCONCLUSIVE: controller={auraController != null}, grounded={grounded}, special={special}");
        rb.position = originalPosition; player.transform.position = rb.position; rendererTransformForBranch.rotation = oldRotation; Physics.SyncTransforms();
        yield return null;
        float maxError = 0f; int samples = 0; float maxGroundError = 0f;
        deadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            float rendererError = Vector3.Angle(aura.transform.up, player.playerRenderer.transform.up);
            maxError = Mathf.Max(maxError, rendererError);
            maxGroundError = Mathf.Max(maxGroundError, Vector3.Angle(aura.transform.up, best.normal));
            samples++;
        }
        bool pass = samples > 2 && maxError < 1f && maxGroundError < 6f;
        Debug.Log($"Aura slope probe {(pass ? "PASS" : "INCONCLUSIVE")}: collider={best.collider.name}, slope={Vector3.Angle(best.normal,Vector3.up):F2}, rendererErrorMax={maxError:F3}, groundErrorMax={maxGroundError:F3}, samples={samples}, position={rb.position}");
        // Probe a controlled traversal across the generated mesh. Move the locked
        // body in short steps, allowing the real renderer and aura to update each
        // frame, and compare with the ground ray used by PlayerRenderer.
        Vector3 direction = Vector3.zero; float largestChange = 0f;
        for (int i = 0; i < 36; i++)
        {
            float angle = i * Mathf.PI * 2f / 36f;
            Vector3 candidate = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 origin = best.point + candidate * 8f + Vector3.up * 40f;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 80f, GameManager.Instance.whatIsGround) || hit.collider != best.collider) continue;
            float change = Vector3.Angle(best.normal, hit.normal);
            if (change > largestChange) { largestChange = change; direction = candidate; }
        }
        if (largestChange < 10f) { Debug.Log("Aura traversal probe INCONCLUSIVE: no nearby change in terrain normal"); yield break; }
        float maxRendererError = 0f, maxSurfaceError = 0f, lastSurfaceError = 0f; int traversalSamples = 0;
        for (int i = 0; i <= 40; i++)
        {
            Vector3 origin = best.point + direction * (i * .2f) + Vector3.up * 40f;
            if (!Physics.Raycast(origin, Vector3.down, out var ground, 80f, GameManager.Instance.whatIsGround) || ground.collider != best.collider) continue;
            rb.position = ground.point + Vector3.up * (centerHeight + .1f); player.transform.position = rb.position; Physics.SyncTransforms();
            yield return null;
            yield return new WaitForEndOfFrame();
            var renderTransform = player.playerRenderer.transform;
            if (!Physics.Raycast(renderTransform.position + Vector3.up, Vector3.down, out var surface, 51f, GameManager.Instance.whatIsGround) || surface.collider.CompareTag("CameraIgnore")) continue;
            maxRendererError = Mathf.Max(maxRendererError, Vector3.Angle(aura.transform.up, renderTransform.up));
            lastSurfaceError = Vector3.Angle(aura.transform.up, surface.normal);
            Debug.Log($"Aura traversal sample: step={i}, dt={Time.deltaTime:F4}, auraSurface={lastSurfaceError:F3}, rendererSurface={Vector3.Angle(renderTransform.up, surface.normal):F3}, normalChange={Vector3.Angle(best.normal, surface.normal):F3}");
            maxSurfaceError = Mathf.Max(maxSurfaceError, lastSurfaceError); traversalSamples++;
        }
        Debug.Log($"Aura traversal probe: samples={traversalSamples}, endpointNormalChange={largestChange:F2}, rendererErrorMax={maxRendererError:F3}, surfaceErrorMax={maxSurfaceError:F3}, surfaceErrorLast={lastSurfaceError:F3}");
        yield return new WaitForSecondsRealtime(15f);
    }
}
#endif
