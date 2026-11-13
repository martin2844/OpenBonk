#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using System.Reflection;
using UnityEngine;

// Opt-in visual check; dispatches controlled pickup events after normal scene startup.
internal sealed class AndroidPowerupOverlayProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_powerup_overlay_probe", false)) return;
        var root = new GameObject("Powerup overlay probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPowerupOverlayProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.statusEffects == null)) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("Powerup overlay probe: game unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        var holder = new GameObject("controlled overlay pickup"); holder.SetActive(false); var pickup = holder.AddComponent<Pickup>();
        pickup.ePickup = EPickup.Stonks; Pickup.A_PickupTriggered?.Invoke(pickup); Debug.Log("Powerup overlay probe: dispatched Stonks");
        yield return new WaitForSecondsRealtime(6f);
        pickup.ePickup = EPickup.Time; Pickup.A_PickupTriggered?.Invoke(pickup); Debug.Log("Powerup overlay probe: dispatched Time");
        yield return new WaitForSecondsRealtime(8f);
        Destroy(holder);
    }
}

// Disposable controlled pickups: checks repeated status effects through actual event subscribers.
internal sealed class AndroidRepeatedPowerupProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_repeated_powerup_probe", false)) return;
        var root = new GameObject("repeated powerup probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidRepeatedPowerupProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() ||
               MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null ||
               MyPlayer.Instance.inventory.statusEffects == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Repeated powerup probe INCONCLUSIVE: no game"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, MyTime.time + 10000f);
        var holder = new GameObject("controlled powerup pickup"); holder.SetActive(false); var pickup = holder.AddComponent<Pickup>();
        foreach (var spec in new[] { (EPickup.Haste, EStatusEffect.Haste), (EPickup.Rage, EStatusEffect.Rage), (EPickup.Shield, EStatusEffect.Shield) })
        {
            pickup.ePickup = spec.Item1;
            Pickup.A_PickupTriggered?.Invoke(pickup);
            yield return new WaitForSecondsRealtime(1f);
            int first = Count(player, spec.Item1);
            float oldExpiry = player.inventory.statusEffects.statusEffects[spec.Item2].expirationTime;
            Pickup.A_PickupTriggered?.Invoke(pickup);
            yield return new WaitForSecondsRealtime(1f);
            int second = Count(player, spec.Item1);
            bool active = player.inventory.statusEffects.HasStatusEffect(spec.Item2);
            float newExpiry = active ? player.inventory.statusEffects.statusEffects[spec.Item2].expirationTime : 0f;
            bool ok = first == 1 && second == 1 && active && newExpiry > oldExpiry;
            string message = $"Repeated powerup probe {(ok ? "PASS" : "FAIL")}: type={spec.Item1}, effects={first}->{second}, active={active}, expiry={oldExpiry:F3}->{newExpiry:F3}";
            if (ok) Debug.Log(message); else Debug.LogError(message);
        }
        deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline &&
            (player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste) || player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Rage) ||
             player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Shield) || Count(player, EPickup.Haste) != 0 ||
             Count(player, EPickup.Rage) != 0 || Count(player, EPickup.Shield) != 0)) yield return null;
        bool cleared = !player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste) && !player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Rage) &&
            !player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Shield) && Count(player, EPickup.Haste) == 0 && Count(player, EPickup.Rage) == 0 && Count(player, EPickup.Shield) == 0;
        if (!cleared) { Debug.LogError("Repeated powerup lifecycle INCONCLUSIVE: status or visual effect did not expire before deadline"); Destroy(holder); yield break; }
        pickup.ePickup = EPickup.Haste;
        Pickup.A_PickupTriggered?.Invoke(pickup);
        yield return new WaitForSecondsRealtime(1f);
        int reacquired = Count(player, EPickup.Haste);
        bool activeAgain = player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste);
        bool lifecycleOk = activeAgain && reacquired == 1;
        string lifecycleMessage = $"Repeated powerup lifecycle {(lifecycleOk ? "PASS" : "FAIL")}: allCleared={cleared}, hasteActiveAgain={activeAgain}, hasteEffects={reacquired}";
        if (lifecycleOk) Debug.Log(lifecycleMessage); else Debug.LogError(lifecycleMessage);
        Destroy(holder);
    }

    static int Count(MyPlayer player, EPickup type)
    {
        int count = 0;
        foreach (var effect in FindObjectsOfType<StatusEffectPickup>())
            if (effect.ePickup == type && effect.transform.IsChildOf(player.transform)) count++;
        return count;
    }
}

// Opt-in: creates two real Haste pickups next to the player and lets pickup physics collect them.
internal sealed class AndroidPhysicalRepeatedHasteProbe : MonoBehaviour
{
    int collected;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_physical_repeated_haste_probe", false)) return;
        var root = new GameObject("physical repeated haste probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPhysicalRepeatedHasteProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() ||
               MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null ||
               MyPlayer.Instance.inventory.statusEffects == null || PickupManager.Instance == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || PickupManager.Instance == null || EnemyManager.Instance == null)
        { Debug.LogError("Physical repeated haste INCONCLUSIVE: no game"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, MyTime.time + 10000f);
        if (player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste)) { Debug.LogError("Physical repeated haste INCONCLUSIVE: already active"); yield break; }
        Pickup.A_PickupTriggered += OnPickup;
        try
        {
            var first = PickupManager.Instance.SpawnPickup(EPickup.Haste, player.transform.position, 1, false, 0f);
            if (first == null) { Debug.LogError("Physical repeated haste INCONCLUSIVE: first spawn failed"); yield break; }
            deadline = Time.realtimeSinceStartup + 40f;
            while (Time.realtimeSinceStartup < deadline && collected < 1) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            if (collected != 1 || !player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste))
            { Debug.LogError($"Physical repeated haste INCONCLUSIVE: first collection count={collected}"); yield break; }
            int firstEffects = Count(player);
            float firstExpiry = player.inventory.statusEffects.statusEffects[EStatusEffect.Haste].expirationTime;
            var second = PickupManager.Instance.SpawnPickup(EPickup.Haste, player.transform.position, 1, false, 0f);
            if (second == null) { Debug.LogError("Physical repeated haste INCONCLUSIVE: second spawn failed"); yield break; }
            deadline = Time.realtimeSinceStartup + 40f;
            while (Time.realtimeSinceStartup < deadline && collected < 2) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            int secondEffects = Count(player);
            bool active = player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste);
            float secondExpiry = active ? player.inventory.statusEffects.statusEffects[EStatusEffect.Haste].expirationTime : 0f;
            bool ok = collected == 2 && firstEffects == 1 && secondEffects == 1 && active && secondExpiry > firstExpiry;
            string message = $"Physical repeated haste {(ok ? "PASS" : "FAIL")}: collected={collected}, effects={firstEffects}->{secondEffects}, active={active}, expiry={firstExpiry:F3}->{secondExpiry:F3}";
            if (ok) Debug.Log(message); else Debug.LogError(message);
        }
        finally { Pickup.A_PickupTriggered -= OnPickup; }
    }
    void OnPickup(Pickup pickup) { if (pickup != null && pickup.ePickup == EPickup.Haste) collected++; }
    void OnDestroy() { Pickup.A_PickupTriggered -= OnPickup; }
    static int Count(MyPlayer player)
    {
        int count = 0;
        foreach (var effect in FindObjectsOfType<StatusEffectPickup>())
            if (effect.ePickup == EPickup.Haste && effect.transform.IsChildOf(player.transform)) count++;
        return count;
    }
}
#endif
