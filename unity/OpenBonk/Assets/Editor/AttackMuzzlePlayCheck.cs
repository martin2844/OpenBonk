using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AttackMuzzlePlayCheck
{
    const string Pending = "AttackMuzzlePlayCheck.pending";
    static AttackMuzzlePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("muzzle test").AddComponent<AttackMuzzlePlayRunner>().Begin(); } }
}
public class AttackMuzzlePlayRunner : MonoBehaviour
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("AttackMuzzlePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/IceRing.prefab");
        var obj = Instantiate(prefab); var aura = obj.GetComponent<IceAura>(); aura.enabled = false;
        var muzzle = aura.attackMuzzle; var sfx = muzzle.randomSfx; var particle = muzzle.ps;
        Check(sfx != null && sfx.sounds.Length > 0 && sfx.sounds[0] != null && sfx.s != null && particle != null, "exported prefab references");
        sfx.playOnAwake = false; sfx.randomVolume = 0; sfx.s.Stop(); sfx.s.clip = null; particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzle.Set(5, Time.fixedDeltaTime);
        float volume = (float)typeof(AttackMuzzle).GetField("volumeMultiplier", Private).GetValue(muzzle);
        float length = (float)typeof(AttackMuzzle).GetField("cliplength", Private).GetValue(muzzle);
        Check(Mathf.Abs(volume - muzzle.minVolumeMultiplier) < .0001f && Mathf.Approximately(length, sfx.sounds[0].length), "first clip and minimum volume");
        typeof(AttackMuzzle).GetField("lastPlayedTime", Private).SetValue(muzzle, Time.time - muzzle.minAudioCooldown - 1f);
        muzzle.Play(); Check(sfx.s.clip == sfx.sounds[0] && particle.isPlaying, "sound selected and particle playing");
        sfx.s.clip = null; particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        muzzle.Play(); Check(sfx.s.clip == null && !particle.isPlaying, "cooldown throttles sound and particles");
        sfx.enabled = false; typeof(AttackMuzzle).GetField("lastPlayedTime", Private).SetValue(muzzle, Time.time - muzzle.minAudioCooldown - 1f);
        muzzle.Play(); Check(sfx.s.clip == null && particle.isPlaying, "disabled sound still plays particle");
        muzzle.Set(1, muzzle.maxInterval); volume = (float)typeof(AttackMuzzle).GetField("volumeMultiplier", Private).GetValue(muzzle);
        Check(Mathf.Approximately(volume, 1f), "full volume at maximum interval");
        Debug.Log("AttackMuzzlePlayCheck passed: original prefab references, audio selection, particle dispatch, cooldown and volume"); EditorApplication.Exit(0); yield break;
    }
}
