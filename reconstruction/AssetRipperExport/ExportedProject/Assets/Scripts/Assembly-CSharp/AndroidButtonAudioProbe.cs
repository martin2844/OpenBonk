#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using UnityEngine;

// Opt-in diagnostic; it only observes the normal menu button event.
internal sealed class AndroidButtonAudioProbe : MonoBehaviour
{
    private AndroidButtonAudioMeter meter;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_button_audio_probe", false)) return;
        var root = new GameObject("button audio probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidButtonAudioProbe>();
    }

    IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 60f;
        MainMenu menu = null;
        while (Time.realtimeSinceStartup < until && ((menu = FindObjectOfType<MainMenu>(true)) == null || AudioManager.Instance == null)) yield return null;
        if (menu == null || AudioManager.Instance == null) { Debug.LogError("Button audio probe: menu unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        var sound = AudioManager.Instance.uiClick;
        if (sound != null && sound.s != null)
        {
            meter = sound.s.gameObject.AddComponent<AndroidButtonAudioMeter>();
            Debug.Log($"Button audio probe meter: object={sound.s.gameObject.name}, sources={sound.s.gameObject.GetComponents<AudioSource>().Length}");
        }
        int count = 0;
        foreach (var item in FindObjectsOfType<MyButton>(true))
        {
            var target = item;
            var button = target.GetButton();
            if (button == null) continue;
            button.onClick.AddListener(() => Observe(target));
            count++;
        }
        Debug.Log($"Button audio probe: observing {count} buttons");
    }

    void Observe(MyButton button)
    {
        if (meter != null) meter.ResetMeasurement();
        var manager = AudioManager.Instance;
        bool queued = manager != null && (bool)typeof(AudioManager).GetField("queueEnter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
        Debug.Log($"Button audio probe: button={button.name}, custom={(button.customSfx != null)}, queued={queued}");
        StartCoroutine(AfterFrame());
    }

    IEnumerator AfterFrame()
    {
        yield return new WaitForEndOfFrame();
        var sound = AudioManager.Instance != null ? AudioManager.Instance.uiClick : null;
        var source = sound != null ? sound.s : null;
        Debug.Log($"Button audio probe: source={(source != null)}, clip={(source != null && source.clip != null ? source.clip.name : "none")}, playing={(source != null && source.isPlaying)}");
        if (source == null || source.clip == null) yield break;
        var sourceSamples = new float[256];
        var listenerSamples = new float[256];
        float sourcePeak = 0f, listenerPeak = 0f;
        int playingFrames = 0;
        float deadline = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < deadline && source != null)
        {
            if (source.isPlaying) playingFrames++;
            source.GetOutputData(sourceSamples, 0);
            AudioListener.GetOutputData(listenerSamples, 0);
            for (int i = 0; i < sourceSamples.Length; i++)
            {
                sourcePeak = Mathf.Max(sourcePeak, Mathf.Abs(sourceSamples[i]));
                listenerPeak = Mathf.Max(listenerPeak, Mathf.Abs(listenerSamples[i]));
            }
            yield return null;
        }
        Debug.Log($"Button audio probe output: playingFrames={playingFrames}, sourcePeak={sourcePeak:F6}, listenerPeak={listenerPeak:F6}, dspPeak={(meter != null ? meter.Peak : 0f):F6}, dspCallbacks={(meter != null ? meter.Callbacks : 0)}");
    }
}

// Records only a peak and callback count; invoked by Unity's audio thread.
internal sealed class AndroidButtonAudioMeter : MonoBehaviour
{
    public volatile float Peak;
    public volatile int Callbacks;
    public void ResetMeasurement() { Peak = 0f; Callbacks = 0; }
    private void OnAudioFilterRead(float[] data, int channels)
    {
        float peak = Peak;
        foreach (float sample in data) { float value = System.Math.Abs(sample); if (value > peak) peak = value; }
        Peak = peak;
        Callbacks++;
    }
}

// Opt-in measurement of the existing music source, without changing playback.
internal sealed class AndroidMusicAudioProbe : MonoBehaviour
{
    bool stage;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool stage = intent.Call<bool>("getBooleanExtra", "megabonk_music_stage_audio_probe", false);
        if (!stage && !intent.Call<bool>("getBooleanExtra", "megabonk_music_audio_probe", false)) return;
        var root = new GameObject("music audio probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMusicAudioProbe>().stage = stage;
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + (stage ? 180f : 60f);
        var currentTrack = typeof(MusicController).GetField("currentTrack", BindingFlags.Instance | BindingFlags.NonPublic);
        while (Time.realtimeSinceStartup < deadline &&
            (MusicController.Instance == null || MusicController.Instance.musicSource == null || !MusicController.Instance.musicSource.isPlaying ||
             (stage && (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GeneratedMap" ||
                        Assets.Scripts.Actors.Player.MyPlayer.Instance == null || !Assets.Scripts.Actors.Player.MyPlayer.Instance.hasStarted ||
                        currentTrack.GetValue(MusicController.Instance) == null ||
                        ReferenceEquals(currentTrack.GetValue(MusicController.Instance), MusicController.Instance.menuMusicTrack))))) yield return null;
        var source = MusicController.Instance != null ? MusicController.Instance.musicSource : null;
        if (source == null || !source.isPlaying || source.clip == null || (stage &&
            (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GeneratedMap" ||
             currentTrack.GetValue(MusicController.Instance) == null || ReferenceEquals(currentTrack.GetValue(MusicController.Instance), MusicController.Instance.menuMusicTrack))))
        { Debug.LogError("Music audio probe INCONCLUSIVE: requested music unavailable"); yield break; }
        var meter = source.gameObject.AddComponent<AndroidButtonAudioMeter>();
        yield return new WaitForSecondsRealtime(4f);
        bool ok = source.isPlaying && source.clip != null && meter.Callbacks > 0 && meter.Peak > .00001f;
        string message = $"Music audio probe {(ok ? "PASS" : "FAIL")}: stage={stage}, clip={(source.clip != null ? source.clip.name : "none")}, playing={source.isPlaying}, sourceVolume={source.volume:F3}, peak={meter.Peak:F6}, callbacks={meter.Callbacks}, sourceCount={source.gameObject.GetComponents<AudioSource>().Length}";
        if (ok) Debug.Log(message); else Debug.LogError(message);
        Destroy(meter);
    }
}
#endif
