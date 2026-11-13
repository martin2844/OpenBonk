#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in observer: grants Aura, but leaves player movement and enemies untouched.
internal sealed class AndroidAuraMovementProbe : MonoBehaviour
{
    private bool startOnSlope;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_aura_movement_probe", false)) return;
        var root = new GameObject("Aura movement probe");
        DontDestroyOnLoad(root);
        var probe = root.AddComponent<AndroidAuraMovementProbe>();
        probe.startOnSlope = intent.Call<bool>("getBooleanExtra", "megabonk_aura_start_slope", false);
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || player.playerRenderer == null || DataManager.Instance == null) { Debug.LogError("Aura movement probe INCONCLUSIVE: player unavailable"); yield break; }
        var data = DataManager.Instance.GetWeapon(EWeapon.Aura);
        if (data == null) { Debug.LogError("Aura movement probe INCONCLUSIVE: weapon unavailable"); yield break; }
        player.inventory.weaponInventory.AddWeapon(data, null);
        deadline = Time.realtimeSinceStartup + 20f;
        CombatAura aura = null;
        while (Time.realtimeSinceStartup < deadline && (aura = FindFirstObjectByType<CombatAura>()) == null) yield return null;
        if (aura == null) { Debug.LogError("Aura movement probe INCONCLUSIVE: effect unavailable"); yield break; }
        if (startOnSlope)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb == null || EnemyManager.Instance == null) { Debug.LogError("Aura movement slope setup INCONCLUSIVE: missing body or manager"); yield break; }
            var start = player.transform.position; RaycastHit best = default; bool found = false;
            for (int radius = 5; radius <= 120 && !found; radius += 5)
                for (int a = 0; a < 36 && !found; a++)
                {
                    float angle = a * Mathf.PI * 2f / 36f;
                    Vector3 origin = start + new Vector3(Mathf.Cos(angle) * radius, 200f, Mathf.Sin(angle) * radius);
                    if (!Physics.Raycast(origin, Vector3.down, out var hit, 400f, GameManager.Instance.whatIsGround) || hit.collider.name != "CombinedCollisionMesh" || hit.collider.CompareTag("CameraIgnore")) continue;
                    float slope = Vector3.Angle(hit.normal, Vector3.up);
                    if (slope >= 12f && slope <= 35f) { best = hit; found = true; }
                }
            if (!found) { Debug.LogError("Aura movement slope setup INCONCLUSIVE: no suitable slope"); yield break; }
            EnemyManager.Instance.enabledWaves = false;
            var capsule = player.GetComponent<CapsuleCollider>();
            float centerHeight = capsule != null ? capsule.height * .5f : 1.5f;
            rb.position = best.point + Vector3.up * (centerHeight + .1f); player.transform.position = rb.position;
            rb.velocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(2f);
            Debug.Log($"Aura movement slope setup: slope={Vector3.Angle(best.normal, Vector3.up):F2}, position={player.transform.position}");
        }
        Debug.Log("Aura movement probe START: move normally across terrain for 45 seconds");
        int samples = 0, moving = 0, overFive = 0, sloped = 0, movingSloped = 0;
        float distance = 0f, maxSurface = 0f, maxRenderer = 0f, maxNormalChange = 0f, maxSlope = 0f, maxMovingSurface = 0f;
        Vector3 lastPosition = player.transform.position, lastNormal = Vector3.zero;
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && player != null && !player.IsDead() && aura != null)
        {
            yield return new WaitForEndOfFrame();
            if (player == null || aura == null || player.playerRenderer == null) break;
            Vector3 position = player.transform.position;
            float step = Vector3.Distance(position, lastPosition); lastPosition = position;
            if (step > 2f) continue; // Exclude teleports from traversal distance.
            distance += step;
            var renderer = player.playerRenderer.transform;
            if (!Physics.Raycast(renderer.position + Vector3.up, Vector3.down, out var surface, 51f, GameManager.Instance.whatIsGround) || surface.collider.CompareTag("CameraIgnore")) continue;
            float rendererAngle = Vector3.Angle(aura.transform.up, renderer.up);
            float surfaceAngle = Vector3.Angle(aura.transform.up, surface.normal);
            float slope = Vector3.Angle(Vector3.up, surface.normal);
            float normalChange = lastNormal == Vector3.zero ? 0f : Vector3.Angle(lastNormal, surface.normal); lastNormal = surface.normal;
            samples++; if (step > .001f) moving++; if (surfaceAngle > 5f) overFive++;
            if (slope > 5f) { sloped++; if (step > .001f) movingSloped++; }
            if (step > .001f) maxMovingSurface = Mathf.Max(maxMovingSurface, surfaceAngle);
            maxSlope = Mathf.Max(maxSlope, slope);
            maxSurface = Mathf.Max(maxSurface, surfaceAngle); maxRenderer = Mathf.Max(maxRenderer, rendererAngle); maxNormalChange = Mathf.Max(maxNormalChange, normalChange);
            if (normalChange > 5f || (samples % 60 == 0)) Debug.Log($"Aura movement sample: dt={Time.deltaTime:F4}, step={step:F3}, slope={slope:F2}, auraSurface={surfaceAngle:F2}, auraRenderer={rendererAngle:F2}, normalChange={normalChange:F2}");
        }
        Debug.Log($"Aura movement probe {(samples > 30 && moving > 10 && distance > 5f && movingSloped > 0 ? "SLOPE OBSERVED" : "INCONCLUSIVE")}: samples={samples}, moving={moving}, distance={distance:F2}, sloped={sloped}, movingSloped={movingSloped}, maxSlope={maxSlope:F2}, surfaceOver5={overFive}, maxSurface={maxSurface:F2}, maxMovingSurface={maxMovingSurface:F2}, maxRenderer={maxRenderer:F2}, maxNormalChange={maxNormalChange:F2}");
    }
}
#endif
