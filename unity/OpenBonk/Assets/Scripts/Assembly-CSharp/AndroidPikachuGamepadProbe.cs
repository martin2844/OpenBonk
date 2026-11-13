#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in observer on disposable emulator data; actual input arrives from an external virtual controller.
internal sealed class AndroidPikachuGamepadProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_pikachu_gamepad_probe", false)) return;
        var go = new GameObject("Pikachu gamepad probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPikachuGamepadProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.character != (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu") || player.playerInput == null) { Debug.LogError("Pikachu gamepad probe INCONCLUSIVE: player unavailable"); yield break; }
        var data = DataManager.Instance.GetCharacterData(player.character) as OpenBonkExternalCharacterData;
        var animator = player.playerRenderer.GetComponentInChildren<OpenBonkExternalAnimator>(true);
        var current = typeof(OpenBonkExternalAnimator).GetField("current", BindingFlags.NonPublic | BindingFlags.Instance);
        if (data == null || animator == null || current == null) { Debug.LogError("Pikachu gamepad probe INCONCLUSIVE: animator unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        int weakWalk = 0, weakRun = 0, strongWalk = 0, strongRun = 0;
        float minWeak = 1f, maxWeak = 0f, minStrong = 1f, maxStrong = 0f;
        Debug.Log("Pikachu gamepad probe READY");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline)
        {
            float magnitude = player.playerInput.MovementInputMagnitude;
            var clip = current.GetValue(animator) as AnimationClip;
            if (player.playerMovement.IsTouchingGround() && magnitude >= .15f && magnitude < .45f)
            {
                minWeak = Mathf.Min(minWeak, magnitude); maxWeak = Mathf.Max(maxWeak, magnitude);
                if (clip == data.walkAnimation) weakWalk++; else if (clip == data.runAnimation) weakRun++;
            }
            if (player.playerMovement.IsTouchingGround() && magnitude >= .7f)
            {
                minStrong = Mathf.Min(minStrong, magnitude); maxStrong = Mathf.Max(maxStrong, magnitude);
                if (clip == data.walkAnimation) strongWalk++; else if (clip == data.runAnimation) strongRun++;
            }
            if (weakWalk >= 4 && strongRun >= 4) break;
            yield return null;
        }
        bool pass = weakWalk >= 4 && strongRun >= 4;
        Debug.Log($"Pikachu gamepad probe {(pass ? "PASS" : "FAIL")}: weakWalk={weakWalk} weakRun={weakRun} weakInput={minWeak:F3}-{maxWeak:F3} strongWalk={strongWalk} strongRun={strongRun} strongInput={minStrong:F3}-{maxStrong:F3}");
    }
}
#endif
