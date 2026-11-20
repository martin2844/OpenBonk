using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Actual native visual, hostile damage, timing, pause and cleanup checks.</summary>
public static class EnemyWhirlwindCheck
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly Type Attack = typeof(Enemy).Assembly.GetType("OpenBonkEnemyWhirlwind", true);
    private static readonly Type Storm = typeof(Enemy).Assembly.GetType("OpenBonkHostileWhirlwind", true);
    private static object Call(Type type, object value, string name, params object[] args) => type.GetMethod(name, Flags).Invoke(value, args);
    private static T Property<T>(Type type, object value, string name) => (T)type.GetProperty(name, Flags).GetValue(value);
    private static void Set(object value, string name, object field) => value.GetType().GetField(name, Flags).SetValue(value, field);
    private static void Require(bool result, string description) { if (!result) throw new Exception("EnemyWhirlwindCheck: " + description); }
    private static T Raw<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

    public static void Run()
    {
        var oldData = DataManager.Instance; var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance;
        var oldRun = MapController.runConfig; int oldStage = MapController.index;
        float oldTime = MyTime.time, oldFixed = MyTime.fixedDeltaTime, oldDelta = MyTime.deltaTime;
        float oldDifficulty = MyTime.difficultyTimer, oldFinal = MyTime.finalSwarmTimer;
        bool oldPause = MyTime.paused;
        var oldRandom = Utility.MyRandom.random;
        var templateField = Storm.GetField("visualTemplate", Flags);
        var oldTemplate = (GameObject)templateField.GetValue(null);
        templateField.SetValue(null, null);
        var objects = new List<GameObject>(); var resources = new List<Object>(); var storms = new List<Component>();
        object attack = null;
        PlayerStatusEffects status = null;
        try
        {
            Require(Property<int>(Storm, null, "ActiveCount") == 0, "requires an idle scene without hostile whirlwinds");
            Vector3 origin = new Vector3(6000, 0, 6000);
            var dataRoot = New("whirlwind data", objects, false);
            var data = dataRoot.AddComponent<DataManager>(); DataManager.Instance = data;
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponTornado.asset");
            Require(weapon != null && weapon.attack != null, "native weapon asset");
            data.unsortedWeapons = new List<WeaponData> { weapon };
            var prefab = (GameObject)Call(Storm, null, "NativeProjectile");
            Require(prefab != null && prefab.GetComponent<ProjectileWhirlwind>() != null, "native player projectile reference");
            int originalScripts = prefab.GetComponentsInChildren<MonoBehaviour>(true).Length;
            int originalColliders = prefab.GetComponentsInChildren<Collider>(true).Length;
            var originalRenderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            var originalInstancing = new bool[originalRenderers.Length];
            for (int i = 0; i < originalRenderers.Length; i++) originalInstancing[i] = originalRenderers[i].enableGPUInstancing;
            Require(originalRenderers.Length >= 1 && originalRenderers[0].mesh != null, "native tornado mesh renderer");
            var gameRoot = New("whirlwind manager", objects, false);
            var game = gameRoot.AddComponent<GameManager>(); GameManager.Instance = game;
            int groundLayer = LayerMask.NameToLayer("Ground");
            game.whatIsGround = game.whatIsGroundAndObjects = 1 << groundLayer;
            var map = ScriptableObject.CreateInstance<MapData>(); resources.Add(map);
            map.eMap = EMap.Forest;
            MapController.runConfig = new RunConfig { mapData = map, mapTierIndex = 0 };
            MapController.index = 0;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(floor);
            floor.name = "whirlwind walkable floor"; floor.layer = groundLayer;
            floor.transform.position = origin + Vector3.down * .5f;
            floor.transform.localScale = new Vector3(200, 1, 200);
            var playerRoot = New("whirlwind player", objects, false);
            var player = playerRoot.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.playerMovement = playerRoot.AddComponent<PlayerMovement>();
            player.inventory = Raw<PlayerInventory>();
            player.inventory.playerHealth = Raw<PlayerHealth>();
            player.inventory.playerHealth.hp = player.inventory.playerHealth.maxHp = 100;
            player.inventory.statusEffects = status = new PlayerStatusEffects();
            var stats = Raw<PlayerStatsNew>(); stats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 0;
            stats.stats[EStat.EnemyDamageMultiplier] = stats.stats[EStat.EnemyScalingMultiplier] = stats.stats[EStat.DamageCooldownMultiplier] = 1;
            player.inventory.playerStats = stats;
            playerRoot.transform.position = origin + Vector3.forward * 14;
            var enemyRoot = New("whirlwind caster", objects, false);
            var enemy = enemyRoot.AddComponent<Enemy>(); enemy.enabled = false; enemy.hp = enemy.maxHp = 100;
            var enemyData = ScriptableObject.CreateInstance<EnemyData>(); resources.Add(enemyData);
            enemyData.damage = 20; enemyData.speed = 8; enemyData.minStayAtDistance = 8; enemyData.maxStayAtDistance = 13;
            typeof(Enemy).GetProperty("enemyData", Flags).SetValue(enemy, enemyData);
            typeof(Enemy).GetProperty("id", Flags).SetValue(enemy, 17u);
            enemy.enemyMovement = enemyRoot.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enabled = false;
            enemy.enemyMovement.enemy = enemy;
            enemyRoot.transform.position = origin + Vector3.up;
            enemyRoot.SetActive(true);
            var material = new Material(Shader.Find("Sprites/Default")); resources.Add(material);
            MyTime.time = 100; MyTime.fixedDeltaTime = MyTime.deltaTime = .02f;
            MyTime.difficultyTimer = MyTime.finalSwarmTimer = 0; MyTime.paused = false;
            Utility.MyRandom.random = new System.Random(1234);
            Physics.SyncTransforms();

            Set(enemy, "minStayAtDistance", 8f); Set(enemy, "maxStayAtDistance", 13f);
            enemy.enemyMovement.distanceToTarget = 2;
            Require(enemy.IsRunningFromPlayer(), "native ranged AI retreats from contact range");
            enemy.enemyMovement.distanceToTarget = 10;
            Require(!enemy.IsRunningFromPlayer() && enemy.GetSpeed() == 0, "native ranged AI holds inside its castable spacing band");
            for (uint id = 0; id < 64; id++) for (uint cast = 0; cast < 8; cast++)
            {
                float interval = (float)Call(Attack, null, "Cooldown", id, cast);
                Require(interval >= 12 && interval <= 16, "bounded occasional cadence");
            }
            attack = Activator.CreateInstance(Attack, enemy, material);
            float due = Property<float>(Attack, attack, "NextAttackAt");
            MyTime.time = due - .01f; Call(Attack, attack, "Step");
            Require(!Property<bool>(Attack, attack, "IsWindingUp"), "no attack before cooldown");
            MyTime.time = due; Call(Attack, attack, "Step");
            Require(Property<bool>(Attack, attack, "IsWindingUp") && Property<int>(Storm, null, "ActiveCount") == 0, "visible telegraph before damage volume");
            MyTime.paused = true; MyTime.time = due + 2; Call(Attack, attack, "Step");
            Require(Property<int>(Storm, null, "ActiveCount") == 0, "pause cannot finish windup");
            MyTime.paused = false; MyTime.time = due + 1.19f; Call(Attack, attack, "Step");
            Require(Property<int>(Storm, null, "ActiveCount") == 0, "full telegraph interval");
            MyTime.time = due + 1.21f; Call(Attack, attack, "Step");
            var storm = Property<Component>(Attack, attack, "Active"); storms.Add(storm);
            Require(storm != null && !Property<bool>(Attack, attack, "IsWindingUp"), "windup produces one hostile whirlwind");
            Require(Property<float>(Attack, attack, "NextAttackAt") - MyTime.time >= 12, "full cooldown after launch");
            var visual = storm.transform.Find("Native tornado visual");
            Require(visual != null && visual.GetComponentsInChildren<MonoBehaviour>(true).Length == 0 &&
                visual.GetComponentsInChildren<Collider>(true).Length == 0 && visual.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "visual has no player weapon scripts or physical collision");
            var renderers = visual.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Require(renderers.Length == originalRenderers.Length, "all native particle visual systems preserved");
            for (int i = 0; i < renderers.Length; i++)
                Require(renderers[i].mesh == originalRenderers[i].mesh && renderers[i].sharedMaterial == originalRenderers[i].sharedMaterial,
                    "exact native meshes and materials retained");
            foreach (var renderer in renderers)
                Require(!renderer.enableGPUInstancing && renderer.enabled && renderer.gameObject.activeInHierarchy,
                    "active ordinary particle draw path");
            Set(storm, "age", .25f);
            for (int i = 0; i < 15; i++) Call(Storm, storm, "Update");
            int particlesAlive = 0, meshParticlesAlive = 0;
            foreach (var system in visual.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                Debug.Log($"Whirlwind particle check: name={system.name} active={system.gameObject.activeInHierarchy} count={system.particleCount} emitting={system.isEmitting} paused={system.isPaused} time={system.time}");
                if (!system.gameObject.activeInHierarchy || renderer == null || !renderer.enabled) continue;
                particlesAlive += system.particleCount;
                if (renderer.mesh != null)
                {
                    var liveParticles = new ParticleSystem.Particle[256];
                    int count = system.GetParticles(liveParticles);
                    for (int i = 0; i < count; i++)
                        if (liveParticles[i].remainingLifetime > 0 && liveParticles[i].GetCurrentSize3D(system).sqrMagnitude > .01f &&
                            liveParticles[i].GetCurrentColor(system).a > 8) meshParticlesAlive++;
                    Require(renderer.bounds.size.sqrMagnitude > .01f && renderer.transform.lossyScale.sqrMagnitude > .01f,
                        "nonzero native mesh particle bounds and world scale");
                }
            }
            Require(particlesAlive > 0 && meshParticlesAlive > 0, "active native tornado mesh particles actually emit");
            foreach (var system in visual.GetComponentsInChildren<ParticleSystem>(true))
            { system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); system.Play(false); system.Pause(false); }
            MyTime.deltaTime = .005f;
            for (int i = 0; i < 60; i++) Call(Storm, storm, "Update");
            foreach (var system in visual.GetComponentsInChildren<ParticleSystem>(true))
                Debug.Log($"Whirlwind high FPS check: name={system.name} count={system.particleCount} time={system.time}");
            Require(visual.GetComponentInChildren<ParticleSystem>().particleCount > 0, "particle simulation advances below the fixed timestep");
            MyTime.deltaTime = .02f;
            Vector3 position = storm.transform.position;
            MyTime.paused = true; Call(Storm, storm, "Step");
            Require(storm.transform.position == position && Property<float>(Storm, storm, "Age") == .25f, "pause freezes travel/lifetime");
            MyTime.paused = false;
            playerRoot.transform.position = origin + new Vector3(10, 0, 1); Call(Storm, storm, "Step");
            Require(Mathf.Abs(storm.transform.position.x - origin.x) < .001f && storm.transform.position.z > position.z,
                "projectile follows locked aim instead of homing");
            Require(player.inventory.playerHealth.hp == 100, "dodging sideways avoids damage");
            playerRoot.transform.position = storm.transform.position; Call(Storm, storm, "Step");
            Require(Property<bool>(Storm, storm, "HasHitPlayer") && player.inventory.playerHealth.hp == 85 && enemy.hp == 100,
                "native enemy damage applies to player only");
            Set(player.inventory.playerHealth, "damageCooldown", 0f);
            Call(Storm, storm, "Step");
            Require(player.inventory.playerHealth.hp == 85, "one hit per whirlwind even after damage cooldown clears");
            Call(Attack, attack, "Cancel");
            Require(Property<int>(Storm, null, "ActiveCount") == 0, "caster cancellation removes owned attack");

            playerRoot.transform.position = origin + Vector3.right * 50;
            for (int i = 0; i < 4; i++) storms.Add(Spawn(enemy, origin, material));
            Require(Property<int>(Storm, null, "ActiveCount") == 4 && Spawn(enemy, origin, material) == null, "four-storm global cap");
            foreach (var item in storms) if (item != null) Call(Storm, item, "Cancel");
            var expiring = Spawn(enemy, origin, material); storms.Add(expiring);
            for (int i = 0; i < 226 && expiring != null; i++) Call(Storm, expiring, "Step");
            Require(expiring == null && Property<int>(Storm, null, "ActiveCount") == 0, "bounded lifetime cleans up");
            var deadOwner = Spawn(enemy, origin, material); storms.Add(deadOwner); enemy.hp = 0;
            Call(Storm, deadOwner, "Step"); Require(deadOwner == null, "dead caster cleans up active attack"); enemy.hp = 100;
            var reusedOwner = Spawn(enemy, origin, material); storms.Add(reusedOwner);
            typeof(Enemy).GetProperty("id", Flags).SetValue(enemy, 18u); Call(Storm, reusedOwner, "Step");
            Require(reusedOwner == null, "pooled owner generation cannot inherit an attack");
            var charmedOwner = Spawn(enemy, origin, material); storms.Add(charmedOwner);
            enemy.enemyMovement.state = EnemyMovementRb.State.Charmed; Call(Storm, charmedOwner, "Step");
            Require(charmedOwner == null, "charming caster cancels hostile attack"); enemy.enemyMovement.state = EnemyMovementRb.State.Normal;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(wall); wall.layer = groundLayer;
            wall.transform.position = origin + new Vector3(0, 2, 1); wall.transform.localScale = new Vector3(6, 4, .3f);
            Physics.SyncTransforms(); var blocked = Spawn(enemy, origin, material); storms.Add(blocked);
            for (int i = 0; i < 20 && blocked != null; i++) Call(Storm, blocked, "Step");
            Require(blocked == null, "solid wall stops attack");
            Require(!(bool)Call(Storm, null, "IntersectsPlayer", origin, origin + Vector3.forward, origin + Vector3.up * 8), "another elevation is outside damage volume");
            Require(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length == originalScripts &&
                prefab.GetComponentsInChildren<Collider>(true).Length == originalColliders && prefab.GetComponent<ProjectileWhirlwind>().enabled,
                "native player prefab unchanged");
            for (int i = 0; i < originalRenderers.Length; i++)
                Require(originalRenderers[i].enableGPUInstancing == originalInstancing[i], "native player renderer configuration unchanged");
            Debug.Log("EnemyWhirlwindCheck PASS: native visual identity, ranged spacing, telegraph/cadence, locked travel, player-only single hit, pause, wall, lifetime, owner death/reuse/charm, cap and original prefab preservation");
        }
        finally
        {
            if (attack != null) Call(Attack, attack, "Cancel");
            foreach (var storm in storms) if (storm != null) Object.DestroyImmediate(storm.gameObject);
            var template = (GameObject)templateField.GetValue(null); if (template != null) Object.DestroyImmediate(template);
            templateField.SetValue(null, oldTemplate);
            status?.OnDestroy();
            foreach (var value in objects) if (value != null) Object.DestroyImmediate(value);
            foreach (var value in resources) if (value != null) Object.DestroyImmediate(value);
            DataManager.Instance = oldData; GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer;
            MapController.runConfig = oldRun; MapController.index = oldStage;
            MyTime.time = oldTime; MyTime.fixedDeltaTime = oldFixed; MyTime.deltaTime = oldDelta;
            MyTime.difficultyTimer = oldDifficulty; MyTime.finalSwarmTimer = oldFinal; MyTime.paused = oldPause;
            Utility.MyRandom.random = oldRandom;
        }
    }

    private static Component Spawn(Enemy enemy, Vector3 origin, Material material) =>
        (Component)Call(Storm, null, "TrySpawn", enemy, origin + Vector3.up * .08f, Vector3.forward, material);
    private static GameObject New(string name, List<GameObject> objects, bool active)
    {
        var root = new GameObject(name); root.SetActive(active); objects.Add(root); return root;
    }
}
