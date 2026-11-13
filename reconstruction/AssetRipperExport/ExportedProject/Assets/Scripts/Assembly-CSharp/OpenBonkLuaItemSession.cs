using System;
using System.Runtime.InteropServices;
using System.Text;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// One bounded, persistent Lua table per acquired item type in a run.
internal sealed class OpenBonkLuaItemSession : IDisposable
{
#if UNITY_ANDROID && !UNITY_EDITOR
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr megabonk_lua_session_create(byte[] source, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_event(IntPtr state, byte[] name, double input, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_tick_actions(IntPtr state, double stacks, [Out] double[] values, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_tick_context_v2(IntPtr state, double stacks, int healthAvailable, double hp, double maxHp, double shield, double maxShield, double time, int levelAvailable, int level, int managerAvailable, double managerTime, [Out] double[] values, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_tick_context_v3(IntPtr state, double stacks, int healthAvailable, double hp, double maxHp, double shield, double maxShield, double time, int levelAvailable, int level, int managerAvailable, double managerTime, [Out] double[] values, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_map_enemy(IntPtr state, byte[] name, int stage, byte[] enemy, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_pre_attack(IntPtr state, double stacks, int hasTarget, int boss, int elite,
        double targetHealth, int element, int critical, double procCoefficient, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_on_hit(IntPtr state, double stacks, int hasTarget, int boss, int elite,
        double targetHealth, int element, int critical, double procCoefficient, double damage, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_on_hit_actions(IntPtr state, double stacks, int hasTarget, int boss, int elite,
        double targetHealth, int element, int critical, double procCoefficient, double damage, [Out] double[] values, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_enemy_killed(IntPtr state, double stacks, int hasTarget, int boss, int elite,
        double targetHealth, int element, int critical, double procCoefficient, double damage, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_enemy_killed_actions_v2(IntPtr state, double stacks, int hasTarget, int boss, int elite,
        double targetHealth, int element, int critical, double procCoefficient, double damage, [Out] double[] values, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_player_damage(IntPtr state, int after, double damage, double stacks,
        double hp, double maxHp, double shield, double maxShield, int shieldHit,
        int hasEnemy, int boss, int elite, int element, int critical, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern void megabonk_lua_session_destroy(IntPtr state);
#endif
    private readonly string id;
    private IntPtr state;
    private bool unavailable;
    internal bool IsAvailable => !unavailable && state != IntPtr.Zero;
    public OpenBonkLuaItemSession(string id, string source)
    {
        this.id = id;
        try
        {
            if (string.IsNullOrEmpty(source) || source.IndexOf('\0') >= 0 || Encoding.UTF8.GetByteCount(source) > 65536) throw new ArgumentException("invalid script");
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256];
            state = megabonk_lua_session_create(Encoding.UTF8.GetBytes(source + "\0"), error, (UIntPtr)error.Length);
            if (state == IntPtr.Zero) throw new InvalidOperationException(Error(error));
#else
            throw new PlatformNotSupportedException("Lua item sessions require Android");
#endif
        }
        catch (Exception e) { unavailable = true; Debug.LogWarning($"Lua content unavailable: {id}: {e.Message}"); }
    }
    public bool Invoke(string name, double amount, out float result, DamageContainer context = null, Enemy eventTarget = null,
        PlayerHealth playerHealth = null, int stacks = 0, bool shieldHit = false)
    {
        result = 0f;
        if (unavailable || state == IntPtr.Zero) return false;
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256];
            int status;
            double value;
            if ((name == "player_damaged" || name == "before_player_damage") && playerHealth != null && context != null)
            {
                var enemy = context.enemy;
                status = megabonk_lua_session_player_damage(state, name == "player_damaged" ? 1 : 0, amount, stacks,
                    playerHealth.hp, playerHealth.maxHp, playerHealth.shield, playerHealth.maxShield, shieldHit ? 1 : 0,
                    enemy != null ? 1 : 0, enemy != null && enemy.IsBoss() ? 1 : 0, enemy != null && enemy.IsElite() ? 1 : 0,
                    (int)context.element, context.crit ? 1 : 0, out value, error, (UIntPtr)error.Length);
            }
            else if (name == "pre_attack" || name == "on_hit" || name == "enemy_killed")
            {
                var target = eventTarget != null ? eventTarget : context?.enemy;
                int hasTarget = target != null ? 1 : 0, boss = target != null && target.IsBoss() ? 1 : 0, elite = target != null && target.IsElite() ? 1 : 0;
                double targetHealth = target != null ? target.hp : 0d, proc = context != null ? context.procCoefficient : 0d;
                int element = context != null ? (int)context.element : 0, crit = context != null && context.crit ? 1 : 0;
                status = name == "enemy_killed"
                    ? megabonk_lua_session_enemy_killed(state, amount, hasTarget, boss, elite, targetHealth, element, crit, proc, context != null ? context.damage : 0d, out value, error, (UIntPtr)error.Length)
                    : name == "on_hit"
                    ? megabonk_lua_session_on_hit(state, amount, hasTarget, boss, elite, targetHealth, element, crit, proc, context != null ? context.damage : 0d, out value, error, (UIntPtr)error.Length)
                    : megabonk_lua_session_pre_attack(state, amount, hasTarget, boss, elite, targetHealth, element, crit, proc, out value, error, (UIntPtr)error.Length);
            }
            else status = megabonk_lua_session_event(state, Encoding.UTF8.GetBytes(name + "\0"), amount, out value, error, (UIntPtr)error.Length);
            if (status != 0) throw new InvalidOperationException(status == 2 ? "missing " + name + " callback" : Error(error));
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 10d) throw new InvalidOperationException("multiplier must be between 0 and 10");
            result = (float)value; return true;
#else
            return false;
#endif
        }
        catch (Exception e) { Debug.LogWarning($"Lua content stopped: {id}: {e.Message}"); Dispose(); return false; }
    }
    public bool InvokeMapEnemy(string name, int stage, string enemy, out float result)
    {
        result = 0f;
        if (!IsAvailable) return false;
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256];
            int status = megabonk_lua_session_map_enemy(state, Encoding.UTF8.GetBytes(name + "\0"), stage,
                Encoding.UTF8.GetBytes(enemy + "\0"), out double value, error, (UIntPtr)error.Length);
            if (status != 0) throw new InvalidOperationException(status == 2 ? "missing " + name + " callback" : Error(error));
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 10d) throw new InvalidOperationException("multiplier must be between 0 and 10");
            result = (float)value; return true;
#else
            return false;
#endif
        }
        catch (Exception e) { Debug.LogWarning($"Lua content stopped: {id}: {e.Message}"); Dispose(); return false; }
    }
    public bool InvokeEnemyKilled(int stacks, Enemy enemy, DamageContainer context, out float scale, out float heal, out int grantXp, out int grantGold)
    {
        scale = 0f; heal = 0f; grantXp = 0; grantGold = 0;
        if (!IsAvailable) return false;
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256]; var values = new double[4];
            var target = enemy != null ? enemy : context?.enemy;
            int status = megabonk_lua_session_enemy_killed_actions_v2(state, stacks, target != null ? 1 : 0,
                target != null && target.IsBoss() ? 1 : 0, target != null && target.IsElite() ? 1 : 0,
                target != null ? target.hp : 0, context != null ? (int)context.element : 0,
                context != null && context.crit ? 1 : 0, context != null ? context.procCoefficient : 0,
                context != null ? context.damage : 0, values, error, (UIntPtr)error.Length);
            if (status != 0) throw new InvalidOperationException(status == 2 ? "missing enemy_killed callback" : Error(error));
            scale = (float)values[0]; heal = (float)values[1]; grantXp = (int)values[2]; grantGold = (int)values[3]; return true;
#else
            return false;
#endif
        }
        catch (Exception e) { Debug.LogWarning($"Lua content stopped: {id}: {e.Message}"); Dispose(); return false; }
    }
    public bool InvokeTick(int stacks, PlayerHealth health, float time, out float scale, out float heal, PlayerXp xp = null, GameManager manager = null)
        => InvokeTick(stacks, health, time, out scale, out heal, out _, xp, manager);
    public bool InvokeTick(int stacks, PlayerHealth health, float time, out float scale, out float heal, out int grantXp, PlayerXp xp = null, GameManager manager = null)
    {
        scale = 0f; heal = 0f; grantXp = 0;
        if (!IsAvailable) return false;
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256]; var values = new double[3];
            int status = megabonk_lua_session_tick_context_v3(state, stacks, health != null ? 1 : 0,
                health != null ? health.hp : 0, health != null ? health.maxHp : 0,
                health != null ? health.shield : 0, health != null ? health.maxShield : 0,
                time, xp != null ? 1 : 0, xp != null ? xp.level : 0,
                manager != null ? 1 : 0, manager != null ? manager.GetAliveTime() : 0,
                values, error, (UIntPtr)error.Length);
            if (status != 0) throw new InvalidOperationException(status == 2 ? "missing tick callback" : Error(error));
            scale = (float)values[0]; heal = (float)values[1]; grantXp = (int)values[2]; return true;
#else
            return false;
#endif
        }
        catch (Exception e) { Debug.LogWarning($"Lua content stopped: {id}: {e.Message}"); Dispose(); return false; }
    }
    public bool InvokeOnHit(int stacks, DamageContainer hit, out double[] actions)
    {
        actions = null;
        if (unavailable || state == IntPtr.Zero || hit == null || hit.enemy == null) return false;
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256]; var values = new double[5]; var target = hit.enemy;
            int status = megabonk_lua_session_on_hit_actions(state, stacks, 1, target.IsBoss() ? 1 : 0, target.IsElite() ? 1 : 0,
                target.hp, (int)hit.element, hit.crit ? 1 : 0, hit.procCoefficient, hit.damage, values, error, (UIntPtr)error.Length);
            if (status != 0) throw new InvalidOperationException(status == 2 ? "missing on_hit callback" : Error(error));
            actions = values; return true;
#else
            return false;
#endif
        }
        catch (Exception e) { Debug.LogWarning($"Lua content stopped: {id}: {e.Message}"); Dispose(); return false; }
    }
    private static string Error(byte[] buffer) { int end = Array.IndexOf(buffer, (byte)0); return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end); }
    public void Dispose()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (state != IntPtr.Zero) megabonk_lua_session_destroy(state);
#endif
        state = IntPtr.Zero; unavailable = true;
    }
}
