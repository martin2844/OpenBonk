// Host-side behavior check against small Unity and game stubs.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;

namespace UnityEngine
{
    public class MonoBehaviour { public Transform transform = new Transform(); }
    public class Transform { public Vector3 position, localScale; }
    public class TrailRenderer { public float startWidth; }
    public class Collider { public Vector3 ClosestPoint(Vector3 point) { return point; } }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 one { get { return new Vector3(1,1,1); } }
        public static Vector3 operator *(Vector3 a, float b) { return new Vector3(a.x*b,a.y*b,a.z*b); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
        public Vector3 normalized { get { float m=(float)Math.Sqrt(x*x+y*y+z*z); return m > 0.00001f ? this*(1/m) : new Vector3(); } }
    }
}
namespace Assets.Scripts.Utility { public static class MyTime { public static float time; } }
namespace Assets.Scripts.Actors.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public int hits;
        public void DamageFromPlayerWeapon(object damage) { hits++; }
        public static implicit operator bool(Enemy enemy) { return enemy != null; }
    }
}
namespace Assets.Scripts.Managers
{
    public class EnemyManager
    {
        public static EnemyManager Instance = new EnemyManager();
        public Dictionary<Collider,Enemy> enemies = new Dictionary<Collider,Enemy>();
        public bool GetEnemy(Collider collider, out Enemy enemy) { return enemies.TryGetValue(collider, out enemy); }
    }
}
namespace Assets.Scripts.Inventory__Items__Pickups.Weapons
{
    public class WeaponData { public int eWeapon; }
    public class WeaponBase { public WeaponData weaponData = new WeaponData(); }
    public static class WeaponUtility
    {
        public static float GetAttackSizeMultiplier(WeaponBase weapon) { return 2f; }
        public static object GetDamageContainer(WeaponBase weapon, object projectile, Enemy enemy, Vector3 direction) { return new object(); }
    }
    public static class EnemyTargeting
    {
        public static Collider[] targets; public static float seenRadius;
        public static int GetEnemiesInRadiusSafe(object owner, Vector3 pos, float radius, out Collider[] buffer)
        { buffer=targets; seenRadius=radius; return targets.Length; }
    }
}
public class EffectManager
{
    public static EffectManager Instance = new EffectManager();
    public List<bool> sound = new List<bool>();
    public void EnemyHitEffect(Vector3 pos, Vector3 dir, bool hit, int weapon, object effect, bool useSfx) { sound.Add(useSfx); }
}
public static class DamageZoneCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Main()
    {
        var zone = new DamageZone { cooldown=1f, defaultRadius=3f, trailRenderer=new TrailRenderer() };
        var weapon = new WeaponBase(); zone.Set(weapon);
        Check(zone.transform.localScale.x==2 && zone.trailRenderer.startWidth==2, "size and trail");
        var first=new Collider(); var missing=new Collider(); var last=new Collider();
        var a=new Enemy(); var b=new Enemy(); a.transform.position=new Vector3(1,0,0); b.transform.position=new Vector3(2,0,0);
        EnemyManager.Instance.enemies[first]=a; EnemyManager.Instance.enemies[last]=b;
        EnemyTargeting.targets=new[] {first,missing,last};
        var tick=typeof(DamageZone).GetMethod("FixedUpdate", BindingFlags.Instance|BindingFlags.NonPublic);
        Assets.Scripts.Utility.MyTime.time=10; tick.Invoke(zone,null);
        Check(EnemyTargeting.seenRadius==6 && a.hits==1 && b.hits==1, "range and registered enemies");
        Check(EffectManager.Instance.sound.Count==2 && EffectManager.Instance.sound[0] && !EffectManager.Instance.sound[1], "first-target audio");
        Assets.Scripts.Utility.MyTime.time=10.5f; tick.Invoke(zone,null);
        Check(a.hits==1 && b.hits==1, "cooldown");
        Assets.Scripts.Utility.MyTime.time=11; tick.Invoke(zone,null);
        Check(a.hits==2 && b.hits==2, "inclusive cooldown boundary");
        Console.WriteLine("PASS: damage zone size, targeting, effects and cooldown");
    }
}
