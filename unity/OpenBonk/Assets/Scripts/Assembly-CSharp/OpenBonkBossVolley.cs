using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Stage bosses throw a fan of elemental bolts, Pokémon-style.</summary>
internal sealed class OpenBonkBossVolley
{
    private const float Windup = .85f;
    private readonly Enemy enemy;
    private readonly OpenBonkMapEnemyData data;
    private float nextAttack = MyTime.time + 2.2f;
    private float fireAt = -1f;
    private Vector3 aim;
    private GameObject charge;

    public OpenBonkBossVolley(Enemy enemy, OpenBonkMapEnemyData data)
    {
        this.enemy = enemy;
        this.data = data;
    }

    public void Step()
    {
        var player = MyPlayer.Instance;
        if (player == null || player.IsDead() || player.isTeleporting) { Cancel(); return; }
        float now = MyTime.time;
        if (fireAt < 0f)
        {
            if (now < nextAttack) return;
            Vector3 feet = player.playerMovement.GetRbFeetPosition();
            float distance = (feet - enemy.GetCenterPosition()).magnitude;
            if (distance < 4f || distance > 42f) { nextAttack = now + .5f; return; }
            aim = feet + Vector3.up * .9f + player.playerMovement.GetVelocity() * .35f;
            fireAt = now + Windup;
            ShowCharge();
            return;
        }
        UpdateCharge();
        if (now < fireAt) return;
        Fire();
    }

    public void Cancel()
    {
        if (charge != null) Object.Destroy(charge);
        charge = null;
        fireAt = -1f;
        nextAttack = MyTime.time + 1f;
    }

    private void Fire()
    {
        Vector3 origin = enemy.GetCenterPosition() + Vector3.up * (enemy.GetHeight() * .2f);
        Vector3 direction = aim - origin;
        float distance = direction.magnitude;
        if (distance < .5f) direction = enemy.transform.forward;
        direction.Normalize();
        int count = Mathf.Max(1, data.volleyCount);
        float stepAngle = count > 1 ? data.volleySpread / (count - 1) : 0f;
        for (int index = 0; index < count; index++)
        {
            float angle = (index - (count - 1) * .5f) * stepAngle;
            OpenBonkBossBolt.Spawn(origin, Quaternion.Euler(0f, angle, 0f) * direction,
                data.volleySpeed, data.volleyRadius, distance / data.volleySpeed + .3f,
                enemy, data.volleyMaterial, data.volleyColor);
        }
        Cancel();
        nextAttack = MyTime.time + data.volleyCooldown;
    }

    private void ShowCharge()
    {
        charge = OpenBonkBossOrb.Create("NimboBossCharge", data.volleyMaterial, data.volleyColor, .3f);
        charge.transform.position = enemy.GetCenterPosition() + Vector3.up * (enemy.GetHeight() * .2f);
    }

    private void UpdateCharge()
    {
        if (charge == null) return;
        float progress = Mathf.Clamp01(1f - (fireAt - MyTime.time) / Windup);
        charge.transform.localScale = Vector3.one * Mathf.Lerp(.3f, .95f, progress);
    }
}

internal sealed class OpenBonkBossBolt : MonoBehaviour
{
    private Vector3 velocity;
    private float radius, life;
    private Enemy source;
    private Material material;
    private Color color;

    internal static void Spawn(Vector3 origin, Vector3 direction, float speed, float radius, float life,
        Enemy source, Material material, Color color)
    {
        var bolt = OpenBonkBossOrb.Create("NimboBossBolt", material, color,
            Mathf.Clamp(.45f + radius * .2f, .5f, 1.1f));
        bolt.transform.position = origin;
        var trail = bolt.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = .3f;
        trail.widthMultiplier = Mathf.Clamp(.22f + radius * .14f, .28f, .6f);
        trail.minVertexDistance = .15f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.shadowCastingMode = ShadowCastingMode.Off;
        var component = bolt.AddComponent<OpenBonkBossBolt>();
        component.velocity = direction.normalized * speed;
        component.radius = radius;
        component.life = life;
        component.source = source;
        component.material = material;
        component.color = color;
    }

    private void Update()
    {
        float step = MyTime.deltaTime;
        if (step <= 0f) return;
        life -= step;
        Vector3 position = transform.position + velocity * step;
        transform.position = position;
        transform.Rotate(0f, 240f * step, 0f, Space.World);
        var player = MyPlayer.Instance;
        if (player != null && !player.IsDead() && !player.isTeleporting)
        {
            Vector3 feet = player.playerMovement.GetRbFeetPosition();
            if ((feet - position).sqrMagnitude < (radius + .6f) * (radius + .6f)) { Explode(feet); return; }
        }
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 8f,
                GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore) && position.y - hit.point.y < .4f)
        {
            Explode(hit.point);
            return;
        }
        if (life <= 0f) Explode(position);
    }

    private void Explode(Vector3 position)
    {
        var player = MyPlayer.Instance;
        if (player != null && !player.IsDead() && !player.isTeleporting)
        {
            Vector3 feet = player.playerMovement.GetRbFeetPosition();
            Vector3 offset = feet - position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radius * radius)
                player.inventory.playerHealth.DamagePlayer(source,
                    offset.sqrMagnitude > .01f ? offset.normalized : Vector3.forward);
        }
        OpenBonkBossFlash.Spawn(position, radius, material, color);
        Object.Destroy(gameObject);
    }
}

internal sealed class OpenBonkBossFlash : MonoBehaviour
{
    private const float Duration = .28f;
    private float age;

    internal static void Spawn(Vector3 position, float radius, Material material, Color color)
    {
        var flash = OpenBonkBossOrb.Create("NimboBossFlash", material, color, radius * .7f);
        flash.transform.position = position;
        flash.AddComponent<OpenBonkBossFlash>().radius = radius;
    }

    private float radius;

    private void Update()
    {
        age += MyTime.deltaTime;
        float progress = Mathf.Clamp01(age / Duration);
        transform.localScale = Vector3.one * Mathf.Lerp(radius * .7f, radius * 1.7f, progress);
        if (progress >= 1f) Object.Destroy(gameObject);
    }
}

internal static class OpenBonkBossOrb
{
    private static readonly Dictionary<Color, Mesh> meshes = new Dictionary<Color, Mesh>();

    internal static GameObject Create(string name, Material material, Color color, float diameter)
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name;
        var collider = orb.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        var filter = orb.GetComponent<MeshFilter>();
        if (!meshes.TryGetValue(color, out var mesh))
        {
            mesh = Object.Instantiate(filter.sharedMesh);
            var colors = new Color[mesh.vertexCount];
            for (int index = 0; index < colors.Length; index++) colors[index] = color;
            mesh.colors = colors;
            meshes[color] = mesh;
        }
        filter.sharedMesh = mesh;
        var renderer = orb.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        orb.transform.localScale = Vector3.one * diameter;
        return orb;
    }
}
