#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Camera;
using Assets.Scripts.Actors;
using UnityEngine;

internal sealed class AndroidCameraShakeProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_camera_shake_probe", false)) return;
        var go = new GameObject("Camera shake probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidCameraShakeProbe>();
    }
    private IEnumerator Start()
    {
        CameraShakerController control = null;
        while (control == null || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.playerHealth == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying())
        {
            control = FindFirstObjectByType<CameraShakerController>(); yield return null;
        }
        yield return new WaitForSeconds(1f);
        var health = MyPlayer.Instance.inventory.playerHealth;
        if (control.shaker == null || control.damageShake == null) { Debug.Log("Camera shake probe: missing scene bindings"); yield break; }
        CurrentSettings.A_SettingUpdated?.Invoke("camera_shake", 0, 1);
        float before = health.hp;
        health.DamagePlayerExternal(10f, 0f, Vector3.up, true, "camera_shake_probe", DcFlags.BypassAll);
        float peak = 0f;
        for (int i = 0; i < 60; i++) { yield return null; peak = Mathf.Max(peak, control.shaker.transform.localPosition.magnitude); }
        Debug.Log($"Camera shake probe: hpBefore={before}, hpAfter={health.hp}, enabled={control.shaker.enabled}, peakPosition={peak}, presetStrength={control.damageShake.Strength}");
    }
}
#endif
