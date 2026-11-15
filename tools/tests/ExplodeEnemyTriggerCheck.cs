// Host-side behavioral check of ExplodeEnemyTrigger against small Unity stubs.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;

namespace UnityEngine
{
    public class MonoBehaviour { public Transform transform = new Transform(); }
    public class Transform { public Vector3 position; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude { get { return (float)Math.Sqrt(x*x + y*y + z*z); } }
        public Vector3 normalized { get { float m = magnitude; return m > 0.00001f ? this * (1f/m) : new Vector3(); } }
        public static Vector3 up { get { return new Vector3(0,1,0); } }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3 operator *(Vector3 a, float b) { return new Vector3(a.x*b,a.y*b,a.z*b); }
    }
    public static class Mathf { public static float Min(float a, float b) { return Math.Min(a,b); } }
    public class GameObject { public int layer; }
    public class Collider { public GameObject gameObject = new GameObject(); public Transform transform = new Transform(); }
    public static class LayerMask { public static int NameToLayer(string name) { return name == "Enemy" ? 7 : -1; } }
    public static class Physics
    {
        public static Collider[] hits;
        public static int mask; public static float radius; public static Vector3 center;
        public static Collider[] OverlapSphere(Vector3 point, float distance, int layerMask)
        { center=point; radius=distance; mask=layerMask; return hits; }
    }
}
public class GameManager { public static GameManager Instance; public int whatIsEnemy; }
namespace Assets.Scripts.Actors
{
    public class DamageContainer
    {
        public Vector3 direction; public float knockback; public float procCoefficient; public string damageSource;
        public DamageContainer(float proc, string source) { procCoefficient=proc; damageSource=source; }
    }
}
namespace Assets.Scripts.Actors.Enemies
{
    public class Enemy { public Movement enemyMovement = new Movement(); }
    public class Movement { public List<DamageContainer> received = new List<DamageContainer>(); public void Knockback(DamageContainer d) { received.Add(d); } }
}
namespace Assets.Scripts.Managers
{
    public class EnemyManager
    {
        public static EnemyManager Instance;
        public Dictionary<Collider, Enemy> targets = new Dictionary<Collider, Enemy>();
        public bool GetEnemy(Collider collider, out Enemy enemy) { return targets.TryGetValue(collider, out enemy); }
    }
}
public static class ExplodeEnemyTriggerCheck
{
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool Near(float a, float b) { return Math.Abs(a-b) < 0.0001f; }
    public static void Main()
    {
        GameManager.Instance = new GameManager { whatIsEnemy = 128 };
        EnemyManager.Instance = new EnemyManager();
        var near = new Collider(); near.gameObject.layer=7; near.transform.position=new Vector3(2,0,0);
        var far = new Collider(); far.gameObject.layer=7; far.transform.position=new Vector3(6,0,0);
        var otherLayer = new Collider(); otherLayer.gameObject.layer=3;
        var unregistered = new Collider(); unregistered.gameObject.layer=7;
        var a = new Enemy(); var b = new Enemy(); var c = new Enemy();
        EnemyManager.Instance.targets[near]=a; EnemyManager.Instance.targets[far]=b; EnemyManager.Instance.targets[otherLayer]=c;
        Physics.hits = new[] { near, far, otherLayer, unregistered };
        var trigger = new ExplodeEnemyTrigger { radius=10 };
        typeof(ExplodeEnemyTrigger).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(trigger, null);
        Assert(Physics.mask==128 && Near(Physics.radius,10), "overlap configuration");
        Assert(a.enemyMovement.received.Count==1 && b.enemyMovement.received.Count==1 && c.enemyMovement.received.Count==0, "target filtering");
        var da=a.enemyMovement.received[0]; var db=b.enemyMovement.received[0];
        Assert(Near(da.knockback,42) && Near(db.knockback,30), "falloff");
        Assert(Near(da.direction.x,1) && Near(da.direction.y,0.4f) && Near(da.direction.z,0), "direction");
        Assert(da.procCoefficient==0 && da.damageSource=="ExplodeEnemyTrigger (Bug probably)", "damage metadata");
        Console.WriteLine("PASS: explosion finds enemies and applies distance-based knockback");
    }
}
