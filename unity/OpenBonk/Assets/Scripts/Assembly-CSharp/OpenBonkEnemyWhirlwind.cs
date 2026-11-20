using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A telegraphed enemy attack using the native tornado weapon's visual.</summary>
internal sealed class OpenBonkEnemyWhirlwind
{
    internal const float Windup = 1.2f;
    private readonly Enemy enemy;
    private readonly Material material;
    private float nextAttack, fireAt = -1f;
    private uint attacks;
    private Vector3 origin, direction;
    private LineRenderer warning;
    private OpenBonkHostileWhirlwind active;

    internal bool IsWindingUp => warning != null;
    internal float NextAttackAt => nextAttack;
    internal OpenBonkHostileWhirlwind Active => active;

    public OpenBonkEnemyWhirlwind(Enemy enemy, Material material)
    {
        this.enemy = enemy;
        this.material = material;
        nextAttack = MyTime.time + Cooldown(enemy.id, 0);
        OpenBonkHostileWhirlwind.PrepareVisual();
    }

    // Stable staggering does not consume the map/spawn RNG stream.
    internal static float Cooldown(uint enemyId, uint attackIndex)
    {
        uint value = unchecked(enemyId * 747796405u + attackIndex * 2891336453u + 277803737u);
        value ^= value >> 16;
        return 12f + value % 4001u * .001f;
    }

    public void Step()
    {
        if (MyTime.paused || MyTime.fixedDeltaTime <= 0f) return;
        var player = MyPlayer.Instance;
        if (enemy == null || enemy.IsDead() || player == null || player.IsDead() || player.isTeleporting)
        { Cancel(); return; }
        float now = MyTime.time;
        if (warning == null)
        {
            if (now < nextAttack || active != null) return;
            nextAttack = now + .5f;
            if (OpenBonkHostileWhirlwind.ActiveCount >= OpenBonkHostileWhirlwind.MaxActive) return;
            Vector3 feet = player.playerMovement.GetRbFeetPosition();
            Vector3 source = enemy.GetBottomPosition();
            Vector3 heading = feet - source;
            heading.y = 0f;
            if (heading.sqrMagnitude < 3f * 3f || heading.sqrMagnitude > 38f * 38f ||
                !Physics.Raycast(source + Vector3.up * 4f, Vector3.down, out var floor, 12f,
                    GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(floor.point.y - feet.y) > 5f) return;
            origin = floor.point + Vector3.up * .08f;
            direction = heading.normalized;
            fireAt = now + Windup;
            ShowWarning();
            return;
        }
        float progress = Mathf.Clamp01(1f - (fireAt - now) / Windup);
        warning.startColor = warning.endColor = new Color(1f, .56f, .12f, Mathf.Lerp(.55f, 1f, progress));
        if (now < fireAt) return;
        active = OpenBonkHostileWhirlwind.TrySpawn(enemy, origin, direction, material);
        ClearWarning();
        nextAttack = now + (active != null ? Cooldown(enemy.id, ++attacks) : 1f);
    }

    public void Cancel()
    {
        ClearWarning();
        if (active != null) active.Cancel();
        active = null;
        nextAttack = MyTime.time + Cooldown(enemy != null ? enemy.id : 0, attacks);
    }

    private void ClearWarning()
    {
        if (warning != null) { warning.gameObject.SetActive(false); OpenBonkHostileWhirlwind.RemoveObject(warning.gameObject); }
        warning = null;
        fireAt = -1f;
    }

    private void ShowWarning()
    {
        warning = new GameObject("Whirlwind warning").AddComponent<LineRenderer>();
        warning.sharedMaterial = material;
        warning.shadowCastingMode = ShadowCastingMode.Off;
        warning.receiveShadows = false;
        warning.widthMultiplier = .22f;
        warning.loop = false;
        warning.positionCount = 5;
        warning.startColor = warning.endColor = new Color(1f, .56f, .12f, .55f);
        Vector3 side = Vector3.Cross(direction, Vector3.up);
        Vector3 tip = origin + direction * 6f + Vector3.up * .16f;
        warning.SetPosition(0, origin + Vector3.up * .16f);
        warning.SetPosition(1, tip);
        warning.SetPosition(2, tip - direction * 1.6f + side);
        warning.SetPosition(3, tip);
        warning.SetPosition(4, tip - direction * 1.6f - side);
        for (int i = 0; i < warning.positionCount; i++)
        {
            Vector3 point = warning.GetPosition(i);
            if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var floor, 6f,
                    GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore))
                warning.SetPosition(i, floor.point + Vector3.up * .16f);
        }
    }
}

/// <summary>Player-only damage and bounded lifetime; never a player projectile.</summary>
internal sealed class OpenBonkHostileWhirlwind : MonoBehaviour
{
    internal const int MaxActive = 4;
    internal const float Speed = 8f, Lifetime = 4.5f, Radius = 1.65f;
    private static readonly HashSet<OpenBonkHostileWhirlwind> live = new HashSet<OpenBonkHostileWhirlwind>();
    private static GameObject visualTemplate;
    private static int templateFrame;
    private Enemy owner;
    private uint ownerId;
    private Vector3 direction;
    private GameObject visual;
    private ParticleSystem[] particles;
    private LineRenderer dangerRing;
    private float age;
    private bool hitPlayer, canceled;

    internal static int ActiveCount => live.Count;
    internal Enemy Owner => owner;
    internal bool HasHitPlayer => hitPlayer;
    internal float Age => age;

    internal static GameObject NativeProjectile()
    {
        var data = DataManager.Instance;
        if (data?.unsortedWeapons == null) return null;
        foreach (var weapon in data.unsortedWeapons)
            if (weapon != null && weapon.eWeapon == EWeapon.Tornado && weapon.attack != null)
                return weapon.attack.GetComponent<WeaponAttack>()?.prefabProjectile;
        return null;
    }

    internal static void PrepareVisual()
    {
        if (visualTemplate != null) return;
        var source = NativeProjectile();
        if (source == null) return;
        // Instantiate inactive: weapon scripts never awaken. Remove scripts,
        // physics and audio before any copy becomes active. The original asset
        // and the player's pool remain completely unchanged.
        visualTemplate = new GameObject("Hostile whirlwind visual template");
        visualTemplate.hideFlags = HideFlags.HideInHierarchy;
        visualTemplate.SetActive(false);
        var copy = Object.Instantiate(source, visualTemplate.transform, false);
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
        { behaviour.enabled = false; Remove(behaviour); }
        foreach (var collider in copy.GetComponentsInChildren<Collider>(true))
        { collider.enabled = false; Remove(collider); }
        foreach (var body in copy.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic = true; body.detectCollisions = false; Remove(body); }
        foreach (var audio in copy.GetComponentsInChildren<AudioSource>(true))
        { audio.enabled = false; Remove(audio); }
        foreach (var child in copy.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 0;
        // Preserve the native particle meshes/materials while using ordinary
        // particle draws on mobile. Instanced mesh-particle paths can submit
        // valid bounds yet draw nothing on GLES drivers.
        foreach (var renderer in copy.GetComponentsInChildren<ParticleSystemRenderer>(true))
            renderer.enableGPUInstancing = false;
        // Pooled/projectile references can carry an inactive root. The holder
        // stays inactive until every inherited script has been removed.
        copy.SetActive(true);
        templateFrame = Time.frameCount;
    }

    private static void Remove(Component component)
    {
        if (Application.isPlaying) Object.Destroy(component);
        else Object.DestroyImmediate(component);
    }

    internal static void RemoveObject(GameObject value)
    {
        if (Application.isPlaying) Object.Destroy(value);
        else Object.DestroyImmediate(value);
    }

    internal static OpenBonkHostileWhirlwind TrySpawn(Enemy source, Vector3 origin, Vector3 direction, Material warningMaterial)
    {
        if (source == null || source.IsDead() || !source.gameObject.activeInHierarchy ||
            live.Count >= MaxActive || direction.sqrMagnitude < .01f || warningMaterial == null) return null;
        PrepareVisual();
        // Runtime Destroy is deferred until the end of the frame. Never
        // activate a clone until all inherited behaviours are actually gone.
        if (visualTemplate == null || (Application.isPlaying && Time.frameCount <= templateFrame) ||
            visualTemplate.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) return null;
        var root = new GameObject("Enemy whirlwind");
        root.transform.position = origin;
        var storm = root.AddComponent<OpenBonkHostileWhirlwind>();
        storm.owner = source;
        storm.ownerId = source.id;
        direction.y = 0f;
        storm.direction = direction.normalized;
        storm.visual = Object.Instantiate(visualTemplate, root.transform, false);
        storm.visual.name = "Native tornado visual";
        storm.visual.hideFlags = HideFlags.None;
        storm.visual.SetActive(true);
        storm.particles = storm.visual.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in storm.particles)
        {
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particle.main;
            main.useUnscaledTime = false;
            particle.Play(false);
            particle.Pause(false);
        }
        storm.dangerRing = new GameObject("Hostile wind radius").AddComponent<LineRenderer>();
        storm.dangerRing.transform.SetParent(root.transform, false);
        storm.dangerRing.useWorldSpace = false;
        storm.dangerRing.sharedMaterial = warningMaterial;
        storm.dangerRing.shadowCastingMode = ShadowCastingMode.Off;
        storm.dangerRing.receiveShadows = false;
        storm.dangerRing.widthMultiplier = .16f;
        storm.dangerRing.startColor = storm.dangerRing.endColor = new Color(1f, .56f, .12f, .9f);
        storm.dangerRing.loop = true;
        storm.dangerRing.positionCount = 24;
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI / 12f;
            storm.dangerRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * Radius, .1f, Mathf.Sin(angle) * Radius));
        }
        live.Add(storm);
        return storm;
    }

    private void FixedUpdate() => Step();

    internal void Step()
    {
        if (canceled) return;
        var player = MyPlayer.Instance;
        if (owner == null || owner.id != ownerId || owner.IsDead() || !owner.gameObject.activeInHierarchy ||
            owner.enemyMovement == null || owner.enemyMovement.state != EnemyMovementRb.State.Normal ||
            player == null || player.IsDead() || player.isTeleporting)
        { Cancel(); return; }
        if (MyTime.paused || MyTime.fixedDeltaTime <= 0f || GameManager.Instance.IsTimeFreeze()) return;
        float step = MyTime.fixedDeltaTime;
        age += step;
        if (age >= Lifetime) { Cancel(); return; }
        Vector3 before = transform.position;
        Vector3 next = before + direction * (Speed * step);
        if (Physics.SphereCast(before + Vector3.up * 1.2f, .6f, direction, out _, Speed * step,
                GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore) ||
            !Physics.Raycast(next + Vector3.up * 3f, Vector3.down, out var floor, 6f,
                GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore))
        { Cancel(); return; }
        next.y = floor.point.y + .08f;
        transform.position = next;
        if (!hitPlayer && IntersectsPlayer(before, next, player.playerMovement.GetRbFeetPosition()))
        {
            hitPlayer = true;
            player.inventory.playerHealth.DamagePlayer(owner, direction);
        }
    }

    internal static bool IntersectsPlayer(Vector3 before, Vector3 after, Vector3 feet)
    {
        Vector3 path = after - before;
        float t = path.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector3.Dot(feet - before, path) / path.sqrMagnitude) : 0f;
        Vector3 offset = feet - (before + path * t);
        return offset.y > -1f && offset.y < 3f && offset.x * offset.x + offset.z * offset.z <= Radius * Radius;
    }

    private void Update()
    {
        if (canceled || MyTime.paused || MyTime.deltaTime <= 0f || GameManager.Instance.IsTimeFreeze()) return;
        foreach (var particle in particles) particle.Simulate(MyTime.deltaTime, false, false, true);
        float fade = Mathf.Min(Mathf.Clamp01(age / .25f), Mathf.Clamp01((Lifetime - age) / .45f));
        visual.transform.localScale = Vector3.one * fade;
    }

    internal void Cancel()
    {
        if (canceled) return;
        canceled = true;
        live.Remove(this);
        gameObject.SetActive(false);
        RemoveObject(gameObject);
    }

    private void OnDestroy() => live.Remove(this);
}
