#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

internal sealed class AndroidLaserBufferProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_laser_buffer_probe", false)) return;
        var go = new GameObject("laser buffer probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLaserBufferProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var p = MyPlayer.Instance; var m = EnemyManager.Instance;
        if (p == null || p.inventory == null || m == null) { Debug.LogError("Laser buffer INCONCLUSIVE: run unavailable"); yield break; }
        m.enabledWaves = false; foreach (var w in p.inventory.weaponInventory.weapons.Values) w.Disable();
        var target = m.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), p.transform.position + Vector3.right * 5f, -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && (target == null || !target.CanTakeDamage())) yield return null;
        if (target == null || !target.CanTakeDamage()) { Debug.LogError("Laser buffer INCONCLUSIVE: target unavailable"); yield break; }
        target.SetDiagnosticHealth(10000f); target.enemyMovement.StopMovement();
        var rb = target.GetComponent<Rigidbody>(); if (rb != null) rb.constraints = RigidbodyConstraints.FreezeAll;
        var weapons = p.inventory.weaponInventory; weapons.AddWeapon(DataManager.Instance.GetWeapon(EWeapon.SpaceNoodle), null); weapons.ToggleWeapon(EWeapon.SpaceNoodle, true);
        var field = typeof(LaserBeamAttack).GetField("whipPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        Vector3[] previous = null; int samples = 0; bool same = true, valid = true;
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && samples < 3)
        {
            yield return new WaitForEndOfFrame();
            foreach (var laser in Resources.FindObjectsOfTypeAll<LaserBeamAttack>())
            {
                if (laser == null || !laser.gameObject.activeInHierarchy || laser.linerenderer == null || !laser.linerenderer.enabled || laser.linerenderer.positionCount <= 2) continue;
                var points = field.GetValue(laser) as Vector3[];
                if (points == null) { valid = false; continue; }
                if (previous != null && !ReferenceEquals(previous, points)) same = false;
                if (points.Length != laser.linerenderer.positionCount) valid = false;
                for (int i = 0; i < points.Length; i++) if (Vector3.Distance(points[i], laser.linerenderer.GetPosition(i)) > .001f) valid = false;
                previous = points; samples++;
            }
        }
        Debug.Log($"Laser buffer {(samples >= 3 && same && valid ? "PASS" : "FAIL")}: samples={samples} reused={same} positions={valid} points={(previous == null ? 0 : previous.Length)}");
    }
}
#endif
