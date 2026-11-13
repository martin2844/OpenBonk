#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in diagnostic: observes the real player's values without changing them.
internal sealed class AndroidLuaTickContextProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_tick_context_probe", false)) return;
        var go = new GameObject("Lua tick context probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaTickContextProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || MyPlayer.Instance.inventory.playerHealth == null)) yield return null;
        var health = MyPlayer.Instance?.inventory?.playerHealth;
        if (health == null || health.maxHp <= 0) { Debug.LogError("Lua tick context INCONCLUSIVE: player unavailable"); yield break; }
        const string script = "return {tick=function(n,c) if not c.health_available then return {scale=0,heal=0} end if n~=2 or c.time<0 or c.max_hp<=0 then return 'bad' end local h=c.hp c.hp=999 return {scale=h/c.max_hp,heal=c.shield} end}";
        using var session = new OpenBonkLuaItemSession("tick-context-probe", script);
        int oldHp = health.hp;
        try
        {
            health.hp = Math.Max(1, health.maxHp / 2);
            float expected = (float)health.hp / health.maxHp, shield = health.shield;
            int snapshotHp = health.hp;
            bool called = session.InvokeTick(2, health, MyTime.time, out float scale, out float heal);
            bool missing = session.InvokeTick(2, null, MyTime.time, out float missingScale, out float missingHeal);
            bool pass = called && missing && health.hp == snapshotHp && Math.Abs(scale - expected) < .0001f && Math.Abs(heal - shield) < .0001f && missingScale == 0f && missingHeal == 0f;
            Debug.Log($"Lua tick context {(pass ? "PASS" : "FAIL")}: scale={scale:F4} expected={expected:F4} shield={heal:F4} expectedShield={shield:F4} missing={missing}");
        }
        finally { health.hp = oldHp; }
        var xp = MyPlayer.Instance?.inventory?.playerXp;
        var manager = GameManager.Instance;
        if (xp == null || manager == null) { Debug.LogError("Lua extended tick context INCONCLUSIVE: game state unavailable"); yield break; }
        float clockDeadline = Time.realtimeSinceStartup + 20f;
        while (manager != null && manager.GetAliveTime() < 2f && Time.realtimeSinceStartup < clockDeadline) yield return null;
        if (manager == null || manager.GetAliveTime() < 2f) { Debug.LogError("Lua extended tick context INCONCLUSIVE: manager clock did not advance"); yield break; }
        const string extended = "return {tick=function(n,c) if not c.level_available or not c.manager_time_available then return {scale=0,heal=0} end local v=c.level c.level=999 return {scale=math.min(v/100,10),heal=math.min(c.manager_time,100)} end}";
        using var extendedSession = new OpenBonkLuaItemSession("tick-context-v2-probe", extended);
        int previousLevel = xp.level;
        try
        {
            xp.level = 7;
            float expectedTime = manager.GetAliveTime();
            bool extendedCalled = extendedSession.InvokeTick(2, health, MyTime.time, out float levelScale, out float managerTime, xp, manager);
            bool extendedPass = extendedCalled && xp.level == 7 && Math.Abs(levelScale - .07f) < .0001f && Math.Abs(managerTime - Math.Min(expectedTime, 100f)) < .01f;
            Debug.Log($"Lua extended tick context {(extendedPass ? "PASS" : "FAIL")}: level=7 scale={levelScale:F4} managerTime={managerTime:F4} expectedTime={expectedTime:F4}");
        }
        finally { xp.level = previousLevel; }
    }
}
#endif
