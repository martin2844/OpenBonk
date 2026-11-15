// Host-side behavior check of cactus collision handling using small Unity stubs.
using System;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

namespace UnityEngine
{
    public class MonoBehaviour { public Transform transform = new Transform(); }
    public class Transform { public Vector3 position; }
    public class GameObject { public int layer; }
    public class Collision { public GameObject gameObject = new GameObject(); public Transform transform = new Transform(); }
    public static class LayerMask { public static int NameToLayer(string name) { return name == "Player" ? 6 : -1; } }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
        public float magnitude { get { return (float)Math.Sqrt(x*x+y*y+z*z); } }
        public Vector3 normalized { get { float m=magnitude; return m>0.00001f ? this*(1f/m) : new Vector3(); } }
        public static Vector3 up { get { return new Vector3(0,1,0); } }
        public static Vector3 operator -(Vector3 a,Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
        public static Vector3 operator +(Vector3 a,Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3 operator *(Vector3 a,float b) { return new Vector3(a.x*b,a.y*b,a.z*b); }
    }
}
namespace Assets.Scripts.Utility { public static class MyTime { public static float time; } }
namespace Assets.Scripts.Actors.Player
{
    public class PlayerHealth
    {
        public int calls; public float damage, knockback; public Vector3 direction; public bool flag; public string source;
        public void DamagePlayerExternal(float damage,float knockback,Vector3 direction,bool flag,string source)
        { calls++; this.damage=damage; this.knockback=knockback; this.direction=direction; this.flag=flag; this.source=source; }
    }
    public class Inventory { public PlayerHealth playerHealth; }
    public class MyPlayer { public static MyPlayer Instance; public Inventory inventory; }
}
public static class DamagePlayerColliderCheck
{
    static void Require(bool value,string message) { if (!value) throw new Exception(message); }
    static bool Near(float x,float y) { return Math.Abs(x-y)<.0001f; }
    public static void Main()
    {
        var health = new PlayerHealth();
        MyPlayer.Instance = new MyPlayer { inventory = new Inventory { playerHealth = health } };
        var script = new DamagePlayerCollider { damage=3, knockbackForce=25, refreshTime=.5f };
        var player = new Collision(); player.gameObject.layer=6; player.transform.position=new Vector3(2,0,0);
        var other = new Collision(); other.gameObject.layer=7;
        var method = typeof(DamagePlayerCollider).GetMethod("OnCollisionEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(script,new object[] {other});
        Require(health.calls==0,"non-player collision caused damage");
        MyTime.time=10f; method.Invoke(script,new object[] {player});
        Require(health.calls==1 && Near(health.damage,3) && Near(health.knockback,25),"damage values");
        Require(health.direction.x>0 && health.direction.y>0 && Near(health.direction.magnitude,1),"knockback direction");
        Require(!health.flag && health.source=="Cactus","damage metadata");
        MyTime.time=10.49f; method.Invoke(script,new object[] {player});
        Require(health.calls==1,"cooldown did not suppress repeated collision");
        MyTime.time=10.5f; method.Invoke(script,new object[] {player});
        Require(health.calls==2,"collision was not accepted at cooldown expiry");
        MyPlayer.Instance=null; MyTime.time=11f; method.Invoke(script,new object[] {player});
        Require(health.calls==2,"missing player should not cause damage");
        Console.WriteLine("PASS: cactus collision filters player, applies damage and respects cooldown");
    }
}
