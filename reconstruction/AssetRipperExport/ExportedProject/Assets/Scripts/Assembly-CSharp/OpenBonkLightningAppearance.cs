using UnityEngine;

// Preserve the original appearance and sound when a shared pooled effect is
// returned. An external weapon's appearance never mutates shared materials.
internal sealed class OpenBonkLightningAppearance : MonoBehaviour
{
    private ParticleSystem[] particles;
    private ParticleSystem.MinMaxGradient[] colors;
    private LineRenderer[] lines;
    private Gradient[] lineGradients;
    private AudioSource source;
    private AudioClip originalClip;
    private float originalPitch;
    private bool configured;

    internal static void Apply(GameObject effect, Color? tint, AudioClip sound)
    {
        var appearance = effect.GetComponent<OpenBonkLightningAppearance>();
        if (appearance == null) appearance = effect.AddComponent<OpenBonkLightningAppearance>();
        appearance.Set(tint, sound);
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (particles != null) return;
        particles = GetComponentsInChildren<ParticleSystem>(true);
        colors = new ParticleSystem.MinMaxGradient[particles.Length];
        for (int i = 0; i < particles.Length; i++) colors[i] = particles[i].main.startColor;
        lines = GetComponentsInChildren<LineRenderer>(true);
        lineGradients = new Gradient[lines.Length];
        for (int i = 0; i < lines.Length; i++) lineGradients[i] = lines[i].colorGradient;
        source = GetComponent<AudioSource>();
        if (source != null) { originalClip = source.clip; originalPitch = source.pitch; }
    }

    private void Set(Color? tint, AudioClip sound)
    {
        Initialize();
        configured = true;
        if (tint.HasValue)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                var main = particles[i].main; main.startColor = tint.Value;
                particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles[i].Play(false);
            }
            for (int i = 0; i < lines.Length; i++)
            {
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(tint.Value, 0f), new GradientColorKey(tint.Value, 1f) }, lineGradients[i].alphaKeys);
                lines[i].colorGradient = gradient;
            }
        }
        if (sound != null && source != null) { source.Stop(); source.clip = sound; source.pitch = 1f; if (source.enabled) source.Play(); }
    }

    private void OnDisable()
    {
        if (!configured) return;
        for (int i = 0; i < particles.Length; i++) { var main = particles[i].main; main.startColor = colors[i]; }
        for (int i = 0; i < lines.Length; i++) lines[i].colorGradient = lineGradients[i];
        if (source != null) { source.Stop(); source.clip = originalClip; source.pitch = originalPitch; }
        configured = false;
    }
}
