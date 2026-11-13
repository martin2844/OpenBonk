#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using UnityEngine;

internal sealed class AndroidThunderboltAppearanceProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_thunderbolt_appearance_probe", false)) return;
        var go = new GameObject("Thunderbolt appearance observer"); DontDestroyOnLoad(go); go.AddComponent<AndroidThunderboltAppearanceProbe>();
    }
    private IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < until)
        {
            var player = MyPlayer.Instance;
            if (player != null && player.inventory != null && player.inventory.weaponInventory != null && player.inventory.weaponInventory.weapons != null)
            {
                foreach (var w in player.inventory.weaponInventory.weapons.Values)
                {
                    if (!(w.weaponData is OpenBonkLuaWeaponData data) || data.externalId != "pikachu:thunderbolt") continue;
                    foreach (var effect in FindObjectsOfType<OpenBonkLightningAppearance>())
                    {
                        var audio = effect.GetComponent<AudioSource>();
                        var particles = effect.GetComponentsInChildren<ParticleSystem>(true);
                        if (audio == null || particles.Length == 0 || data.lightningSound == null || audio.clip != data.lightningSound) continue;
                        Color color = particles[0].main.startColor.color;
                        bool pass = data.hasLightningColor && color.b > .95f && color.g > .7f && color.r < .4f && data.icon != null && data.lightningSound.name == "thunderbolt-zap";
                        Debug.Log($"Thunderbolt appearance runtime {(pass ? "PASS" : "FAIL")}: color={color} sound={audio.clip.name} playing={audio.isPlaying} icon={data.icon.name}");
                        ScreenCapture.CaptureScreenshot("thunderbolt-appearance-runtime.png");
                        yield break;
                    }
                }
            }
            yield return null;
        }
        Debug.LogWarning("Thunderbolt appearance runtime INCONCLUSIVE: no observed strike");
    }
}
#endif
