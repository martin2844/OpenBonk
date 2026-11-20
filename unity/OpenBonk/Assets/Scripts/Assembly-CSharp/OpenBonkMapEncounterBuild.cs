using System;
using System.Collections.Generic;
using System.IO;
using Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Resolves map-local enemies and encounters before either is registered.</summary>
internal sealed class OpenBonkMapEncounterBuild : IDisposable
{
    [Serializable, UnityEngine.Scripting.Preserve]
    internal sealed class EnemySpec
    {
        public string id, name, material, movement, attack, attackMaterial;
        public int hp, damage, xp, fps;
        public float speed, cost, scale, radius;
        public ClipSpec walk, idle, charge;
        public VolleySpec volley;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    internal sealed class VolleySpec
    {
        public string color;
        public int count;
        public float spread, speed, radius, cooldown;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    internal sealed class ClipSpec
    {
        public string prefix;
        public int frames;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    internal sealed class StageSpec
    {
        public int stage;
        public string[] start, reserve, swarm, minibosses, overtime;
        public string boss;
        public RewardSpec reward;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    internal sealed class RewardSpec
    {
        public string id, name, description, icon, stat;
        public float bonus;
    }

    private readonly List<Object> owned = new List<Object>();
    private readonly Dictionary<string, OpenBonkMapEnemyData> byKey =
        new Dictionary<string, OpenBonkMapEnemyData>(StringComparer.Ordinal);
    public IEnumerable<OpenBonkMapEnemyData> Enemies => byKey.Values;
    private readonly Dictionary<EItem, OpenBonkItemData> rewards = new Dictionary<EItem, OpenBonkItemData>();
    public IEnumerable<OpenBonkItemData> Rewards => rewards.Values;

    public static OpenBonkMapEncounterBuild Create(string packageId, OpenBonkExternalMapData map,
        AssetBundle bundle, EnemySpec[] enemies, StageSpec[] stages, Func<string, Texture2D> rewardIcon)
    {
        if (bundle == null || enemies == null || enemies.Length < 1 || enemies.Length > 24 ||
            stages == null || stages.Length < 1 || stages.Length > map.stages.Length)
            throw new InvalidDataException("Map enemies and encounters must be declared together");

        var result = new OpenBonkMapEncounterBuild();
        try
        {
            var enemyIds = new HashSet<EEnemy>();
            foreach (var spec in enemies)
            {
                var enemy = result.CreateEnemy(packageId, bundle, spec);
                if (!enemyIds.Add(enemy.enemyName) || result.byKey.ContainsKey(spec.id))
                    throw new InvalidDataException("Duplicate local enemy ID");
                result.byKey.Add(spec.id, enemy);
            }

            foreach (var stage in stages)
            {
                result.ApplyStage(map, stage);
                if (stage.reward != null)
                {
                    if (string.IsNullOrEmpty(stage.boss)) throw new InvalidDataException("Reward requires a local boss");
                    var item = result.CreateReward(packageId, stage.reward, rewardIcon);
                    map.bossRewards.Add(map.stages[stage.stage - 1], item.eItem);
                }
            }
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private OpenBonkItemData CreateReward(string packageId, RewardSpec spec, Func<string, Texture2D> loadIcon)
    {
        var id = (EItem)OpenBonkContentIds.Item(packageId, spec.id);
        if (rewards.ContainsKey(id) || !Text(spec.name, 80) || !Text(spec.description, 500) ||
            !Enum.TryParse(spec.stat, false, out EStat stat) || stat.ToString() != spec.stat || !Enum.IsDefined(typeof(EStat), stat) ||
            !Range(spec.bonus, .01f, 1f))
            throw new InvalidDataException("Invalid arena reward");
        var icon = loadIcon(spec.icon);
        if (icon == null) throw new InvalidDataException("Missing arena reward icon");
        var item = Own(ScriptableObject.CreateInstance<OpenBonkItemData>());
        item.eItem = id;
        item.name = item.displayName = spec.name;
        item.externalId = packageId + ":" + spec.id;
        item.description = spec.description;
        item.icon = icon;
        item.isEnabled = true;
        item.inItemPool = false;
        item.rarity = EItemRarity.Rare;
        item.maxAmount = item.maxAmountPerRun = 1;
        item.bonuses = new[] { new OpenBonkItemBonus { stat = stat, modifyType = EStatModifyType.Addition, perStack = spec.bonus } };
        rewards.Add(id, item);
        return item;
    }

    private OpenBonkMapEnemyData CreateEnemy(string packageId, AssetBundle bundle, EnemySpec spec)
    {
        if (spec == null || !Text(spec.name, 80) || !Text(spec.material, 120) ||
            (spec.movement != "ground" && spec.movement != "fly" && spec.movement != "charge") ||
            (spec.movement == "charge") != (spec.charge != null) ||
            (string.IsNullOrEmpty(spec.attack) != string.IsNullOrEmpty(spec.attackMaterial)) ||
            (!string.IsNullOrEmpty(spec.attack) && (!Text(spec.attackMaterial, 120) ||
                (spec.attack != "pulse" && spec.attack != "sweep" && spec.attack != "whirlwind"))) ||
            (spec.volley != null && (!TryHexColor(spec.volley.color, out _) ||
                !Range(spec.volley.count, 1f, 9f) || !Range(spec.volley.spread, 0f, 180f) ||
                !Range(spec.volley.speed, 3f, 60f) || !Range(spec.volley.radius, .3f, 5f) ||
                !Range(spec.volley.cooldown, .5f, 20f))) ||
            !Range(spec.hp, 1, 1000000) || !Range(spec.damage, 1, 10000) || !Range(spec.xp, 0, 1000) ||
            !Range(spec.fps, 4, 24) || !Range(spec.speed, .1f, 40f) || !Range(spec.cost, .1f, 30f) ||
            !Range(spec.scale, .25f, 12f) || !Range(spec.radius, .1f, 8f))
            throw new InvalidDataException("Invalid local enemy");
        var kind = (EEnemy)OpenBonkContentIds.Enemy(packageId, spec.id);
        var material = bundle.LoadAsset<Material>(spec.material);
        if (material == null || material.shader == null) throw new InvalidDataException("Missing enemy material");

        var animation = Own(ScriptableObject.CreateInstance<AnimatedMeshScriptableObject>());
        animation.name = spec.name + " animation";
        animation.AnimationFPS = spec.fps;
        var walk = LoadClip(bundle, "walk", spec.walk);
        animation.Animations = new List<AnimatedMeshScriptableObject.Animation> { walk };
        if (spec.idle != null) animation.Animations.Add(LoadClip(bundle, "idle", spec.idle));
        if (spec.charge != null) animation.Animations.Add(LoadClip(bundle, "charge", spec.charge));
        var bounds = walk.Meshes[0].bounds;
        if (spec.radius * 2f > bounds.size.y)
            throw new InvalidDataException("Enemy capsule diameter exceeds model height");

        var enemy = Own(ScriptableObject.CreateInstance<OpenBonkMapEnemyData>());
        enemy.name = spec.name;
        enemy.displayName = spec.name;
        enemy.enemyName = kind;
        enemy.material = material;
        enemy.animation = animation;
        enemy.rendererOffset = Vector3.up * bounds.min.y;
        enemy.rendererScale = spec.scale;
        enemy.colliderRadius = spec.radius;
        enemy.hp = spec.hp;
        enemy.damage = spec.damage;
        enemy.xp = spec.xp;
        enemy.mass = 1;
        enemy.speed = spec.speed;
        enemy.isFlying = spec.movement == "fly";
        enemy.charges = spec.movement == "charge";
        enemy.hasIdle = spec.idle != null;
        if (!string.IsNullOrEmpty(spec.attack))
        {
            enemy.hazardMaterial = bundle.LoadAsset<Material>(spec.attackMaterial);
            if (enemy.hazardMaterial == null || enemy.hazardMaterial.shader == null)
                throw new InvalidDataException("Missing enemy attack material");
            enemy.sweepAttack = spec.attack == "sweep";
            enemy.whirlwindAttack = spec.attack == "whirlwind";
            if (enemy.whirlwindAttack)
            {
                // Use the native ranged-enemy spacing so a flyer does not
                // spend its whole cooldown pressed against the player's body.
                enemy.minStayAtDistance = 8f;
                enemy.maxStayAtDistance = 13f;
            }
        }
        if (spec.volley != null)
        {
            enemy.volleyMaterial = enemy.hazardMaterial != null
                ? enemy.hazardMaterial
                : bundle.LoadAsset<Material>("NimboWarning");
            if (enemy.volleyMaterial == null || enemy.volleyMaterial.shader == null)
                throw new InvalidDataException("Missing enemy volley material");
            TryHexColor(spec.volley.color, out enemy.volleyColor);
            enemy.volleyAttack = true;
            enemy.volleyCount = spec.volley.count;
            enemy.volleySpread = spec.volley.spread;
            enemy.volleySpeed = spec.volley.speed;
            enemy.volleyRadius = spec.volley.radius;
            enemy.volleyCooldown = spec.volley.cooldown;
        }
        enemy.creditCost = spec.cost;
        enemy.canBeElite = true;
        enemy.canBeExecuted = true;
        enemy.teleportCooldown = 12;
        enemy.maxSuckTime = 15;
        enemy.specialAttacks = Array.Empty<EnemySpecialAttack>();
        return enemy;
    }

    private static AnimatedMeshScriptableObject.Animation LoadClip(AssetBundle bundle, string name, ClipSpec clip)
    {
        if (clip == null || !Text(clip.prefix, 100) || clip.frames < 1 || clip.frames > 24)
            throw new InvalidDataException("Invalid enemy animation clip");
        var meshes = new List<Mesh>(clip.frames);
        for (int frame = 0; frame < clip.frames; frame++)
        {
            var mesh = bundle.LoadAsset<Mesh>(clip.prefix + frame.ToString("D2", System.Globalization.CultureInfo.InvariantCulture));
            if (mesh == null || mesh.vertexCount < 3 || mesh.vertexCount > 24000 ||
                !Range(mesh.bounds.size.x, .01f, 30f) || !Range(mesh.bounds.size.y, .01f, 30f) ||
                !Range(mesh.bounds.size.z, .01f, 30f))
                throw new InvalidDataException($"Missing or invalid enemy mesh in {name}");
            meshes.Add(mesh);
        }
        return new AnimatedMeshScriptableObject.Animation { Name = name, Meshes = meshes };
    }

    private void ApplyStage(OpenBonkExternalMapData map, StageSpec spec)
    {
        if (spec == null || spec.stage < 1 || spec.stage > map.stages.Length)
            throw new InvalidDataException("Invalid encounter stage");
        var stage = map.stages[spec.stage - 1];
        if (map.encounters.ContainsKey(stage)) throw new InvalidDataException("Duplicate encounter stage");

        var start = ResolveRoster(spec.start, required: true);
        var reserve = ResolveRoster(spec.reserve);
        foreach (var enemy in reserve)
            if (start.Contains(enemy)) throw new InvalidDataException("Reinforcement already starts in the stage");
        var swarm = spec.swarm == null ? start : ResolveRoster(spec.swarm, required: true);
        var overtime = spec.overtime == null ? swarm : ResolveRoster(spec.overtime, required: true);
        var timeline = stage.stageTimeline;
        timeline.startEnemies = start;
        timeline.minibosses = ResolveRoster(spec.minibosses);
        if (!string.IsNullOrEmpty(spec.boss)) timeline.boss = ResolveEnemy(spec.boss);
        if (timeline.events != null)
            foreach (var entry in timeline.events)
                if (entry != null && entry.eTimelineEvent == ETimelineEvent.ESwarm)
                    entry.enemies = swarm;
        map.encounters.Add(stage, new OpenBonkStageEncounter(start, reserve, overtime));
    }

    private List<EEnemy> ResolveRoster(string[] ids, bool required = false)
    {
        if (ids == null) ids = Array.Empty<string>();
        if ((required && ids.Length == 0) || ids.Length > 12) throw new InvalidDataException("Invalid enemy roster length");
        var result = new List<EEnemy>(ids.Length);
        foreach (var id in ids)
        {
            var enemy = ResolveEnemy(id).enemyName;
            if (result.Contains(enemy)) throw new InvalidDataException("Duplicate enemy in roster");
            result.Add(enemy);
        }
        return result;
    }

    private OpenBonkMapEnemyData ResolveEnemy(string id)
    {
        if (id == null || !byKey.TryGetValue(id, out var enemy))
            throw new InvalidDataException("Unknown local enemy in encounter");
        return enemy;
    }

    private static bool Text(string value, int max) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= max && value.IndexOf('\0') < 0;
    private static bool Range(float value, float min, float max) => value >= min && value <= max;

    private static bool TryHexColor(string value, out Color color)
    {
        color = Color.white;
        if (string.IsNullOrEmpty(value) || value.Length != 7 || value[0] != '#') return false;
        int packed;
        if (!int.TryParse(value.Substring(1), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out packed)) return false;
        color = new Color(((packed >> 16) & 255) / 255f, ((packed >> 8) & 255) / 255f, (packed & 255) / 255f, 1f);
        return true;
    }
    private T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }

    public void Dispose()
    {
        foreach (var obj in owned)
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        owned.Clear();
    }
}

internal sealed class OpenBonkStageEncounter
{
    public readonly List<EEnemy> start, reserve, overtime;

    public OpenBonkStageEncounter(List<EEnemy> start, List<EEnemy> reserve, List<EEnemy> overtime)
    {
        this.start = start;
        this.reserve = reserve;
        this.overtime = overtime;
    }
}
