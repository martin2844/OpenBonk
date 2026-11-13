using System;
using System.Runtime.InteropServices;
using System.Text;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// One persistent Lua session per equipped external weapon. The initial host
// effect strikes a target and optionally chains using the normal combat path.
internal sealed class OpenBonkLuaWeaponSession : IDisposable
{
#if UNITY_ANDROID && !UNITY_EDITOR
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr megabonk_lua_session_create(byte[] source, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_session_weapon_attack(IntPtr state, double level, byte[] enemy, int boss, int elite,
        double hp, double distance, out double value, byte[] error, UIntPtr errorSize);
    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern void megabonk_lua_session_destroy(IntPtr state);
#endif
    private readonly OpenBonkLuaWeaponData data;
    private IntPtr state;
    private bool unavailable;
    public OpenBonkLuaWeaponSession(OpenBonkLuaWeaponData data)
    {
        this.data = data;
        try
        {
            if (string.IsNullOrEmpty(data.luaSource) || data.luaSource.IndexOf('\0') >= 0 || Encoding.UTF8.GetByteCount(data.luaSource) > 65536 ||
                float.IsNaN(data.chainRange) || float.IsInfinity(data.chainRange) || data.chainRange < 0f || data.chainRange > 100f ||
                float.IsNaN(data.chainProcCoefficient) || float.IsInfinity(data.chainProcCoefficient) || data.chainProcCoefficient < 0f || data.chainProcCoefficient > 1f)
                throw new ArgumentException("Invalid Lua weapon configuration");
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256]; state = megabonk_lua_session_create(Encoding.UTF8.GetBytes(data.luaSource + "\0"), error, (UIntPtr)error.Length);
            if (state == IntPtr.Zero) throw new InvalidOperationException(Error(error));
#else
            throw new PlatformNotSupportedException("Lua weapon sessions require Android");
#endif
        }
        catch (Exception e) { unavailable = true; Debug.LogWarning("Lua weapon unavailable: " + e.Message); }
    }
    public void Attack(WeaponBase weapon)
    {
        if (unavailable || state == IntPtr.Zero || MyPlayer.Instance == null || weapon == null) return;
        try
        {
            var player = MyPlayer.Instance;
            var enemy = EnemyTargeting.GetEnemy(player.transform.position, WeaponUtility.GetWeaponRange(weapon), 0, data.useVision, null);
            if (enemy == null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var error = new byte[256];
            string enemyName = enemy.enemyData != null ? enemy.enemyData.enemyName.ToString() : "Unknown";
            double distance = Vector3.Distance(player.transform.position, enemy.transform.position);
            int status = megabonk_lua_session_weapon_attack(state, weapon.level, Encoding.UTF8.GetBytes(enemyName + "\0"),
                enemy.IsBoss() ? 1 : 0, enemy.IsElite() ? 1 : 0, enemy.hp, distance, out double multiplier, error, (UIntPtr)error.Length);
            if (status != 0) throw new InvalidOperationException(status == 2 ? "Missing attack callback" : Error(error));
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0d || multiplier > 10d) throw new InvalidOperationException("Attack multiplier must be between 0 and 10");
            if (data.effect == "fireball" || data.effect == "arrow")
            {
                WeaponUtility.WeaponAttack(weapon, (float)multiplier);
                return;
            }
            float baseDamage = weapon.GetValue(EStat.DamageMultiplier) * (float)multiplier;
            int bounces = Mathf.Clamp(WeaponUtility.GetProjectileBounces(weapon), 0, 16);
            int attacks = Mathf.Clamp(WeaponUtility.GetAttackQuantity(weapon), 1, 16);
            for (int i = 0; i < attacks; i++)
            {
                var target = i == 0 ? enemy : EnemyTargeting.GetEnemy(player.transform.position, WeaponUtility.GetWeaponRange(weapon), i, data.useVision, null);
                if (target == null) continue;
                var damage = WeaponUtility.GetDamageContainer(weapon, null, target, target.GetCenterPosition() - player.transform.position, baseDamage);
                damage.element = EElement.Lightning;
                WeaponUtility.LightningStrike(target, bounces, damage, data.chainRange * WeaponUtility.GetAttackSizeMultiplier(weapon), data.chainProcCoefficient, data.hasLightningColor ? (Color?)data.lightningColor : null, data.lightningSound);
            }
#endif
        }
        catch (Exception e) { unavailable = true; Debug.LogWarning("Lua weapon stopped: " + e.Message); Dispose(); }
    }
    private static string Error(byte[] buffer)
    {
        int end = Array.IndexOf(buffer, (byte)0); return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end);
    }
    public void Dispose()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (state != IntPtr.Zero) megabonk_lua_session_destroy(state);
#endif
        state = IntPtr.Zero; unavailable = true;
    }
}
