using System;
using UnityEditor;
using UnityEngine;

public static class AnimationEffectPortCheck
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Calcium.prefab");
        if (prefab == null) throw new Exception("Calcium prefab missing");
        var instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var effect = instance.GetComponentInChildren<AnimationEffect>(true);
            if (effect == null || effect.particles == null || effect.particles.Length == 0 || effect.particles[0] == null ||
                effect.audioSources == null || effect.audioSources.Length == 0 || effect.audioSources[0] == null)
                throw new Exception("Original Calcium effect references missing");
            var particles = effect.particles[0];
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.PlayEffect(0);
            if (!particles.isPlaying) throw new Exception("Animation did not play particles");
            var sfx = effect.audioSources[0];
            effect.PlayAudio(0);
            if (sfx.s == null || sfx.s.clip == null || Array.IndexOf(sfx.sounds, sfx.s.clip) < 0)
                throw new Exception("Animation did not select an original audio clip");
            effect.PlayEffect(-1); effect.PlayEffect(effect.particles.Length);
            effect.PlayAudio(-1); effect.PlayAudio(effect.audioSources.Length);
            Debug.Log("AnimationEffectPortCheck passed: Calcium prefab particle and audio dispatch, invalid index guards");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
