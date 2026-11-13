using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Utility;

[Serializable]
public sealed class OpenBonkItemBonus
{
    public EStat stat;
    public EStatModifyType modifyType;
    public float perStack;
}

// An external item can apply declared stats and opt into Lua callbacks.
public sealed class OpenBonkItemData : ItemData
{
    public string externalId, displayName, description;
    public OpenBonkItemBonus[] bonuses;
    public string luaSource;
    public bool luaStackChanged, luaPreAttack, luaOnHit, luaLevelUp, luaEnemyKilled, luaPlayerDamaged, luaBeforePlayerDamage, luaTick;
    public override string GetName() => displayName ?? string.Empty;
    public override string GetDescription() => description ?? string.Empty;
    public override string GetInternalName() => externalId ?? string.Empty;
    public bool ValidBonuses()
    {
        if (bonuses == null || bonuses.Length > 16 || (bonuses.Length == 0 && !luaPreAttack && !luaOnHit && !luaBeforePlayerDamage && !luaTick && !luaEnemyKilled)) return false;
        var seen = new HashSet<EStat>();
        foreach (var bonus in bonuses)
            if (bonus == null || !Enum.IsDefined(typeof(EStat), bonus.stat) || !seen.Add(bonus.stat) ||
                (bonus.modifyType != EStatModifyType.Flat && bonus.modifyType != EStatModifyType.Addition) ||
                float.IsNaN(bonus.perStack) || float.IsInfinity(bonus.perStack) || Math.Abs(bonus.perStack) > 10f) return false;
        return true;
    }
}

public sealed class OpenBonkStackingItem : ItemBase
{
    private readonly OpenBonkItemData data;
    private OpenBonkLuaItemSession session;
    private float stackScale = 1f, levelScale = 1f, killScale = 1f, damageScale = 1f, tickScale = 1f;
    private float nextTickTime;
    public OpenBonkStackingItem(OpenBonkItemData data, ItemInventory inventory) : base(inventory) { this.data = data; }
    protected override void OnInitOrAmountChanged()
    {
        stackScale = 1f;
        if (data.luaStackChanged && (session == null || !session.Invoke("stack_changed", amount, out stackScale))) stackScale = 0f;
        ApplyBonuses();
    }
    private void ApplyBonuses()
    {
        float scale = Math.Min(10f, stackScale * levelScale * killScale * damageScale * tickScale);
        foreach (var bonus in data.bonuses)
            SetStat(new StatModifier { stat = bonus.stat, modifyType = bonus.modifyType, modification = amount * bonus.perStack * scale });
    }
    private void OnLevelUp(int level)
    {
        if (session == null || !session.Invoke("level_up", level, out levelScale)) levelScale = 0f;
        ApplyBonuses();
    }
    private void OnEnemyKilled(Enemy enemy, DamageContainer deathSource)
    {
        float heal = 0f; int grantXp = 0, grantGold = 0;
        if (session == null || !session.InvokeEnemyKilled(amount, enemy, deathSource, out killScale, out heal, out grantXp, out grantGold)) killScale = 0f;
        ApplyBonuses();
        var inventory = MyPlayer.Instance?.inventory;
        var health = inventory?.playerHealth;
        if (heal > 0f && health != null && health.hp > 0) health.Heal(heal, false);
        if (grantXp > 0 && inventory != null && health != null && health.hp > 0) inventory.AddXp(grantXp);
        if (grantGold > 0 && inventory != null && health != null && health.hp > 0) inventory.ChangeGold(grantGold);
    }
    private void OnPlayerDamaged(PlayerHealth health, DamageContainer hit, bool shieldDamage)
    {
        if (MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || health != MyPlayer.Instance.inventory.playerHealth || hit == null) return;
        if (session == null || !session.Invoke("player_damaged", hit.damage, out damageScale, hit, playerHealth: health, stacks: amount, shieldHit: shieldDamage)) damageScale = 0f;
        ApplyBonuses();
    }
    public override void Init()
    {
        if (data.luaStackChanged || data.luaPreAttack || data.luaOnHit || data.luaLevelUp || data.luaEnemyKilled || data.luaPlayerDamaged || data.luaBeforePlayerDamage || data.luaTick) session = new OpenBonkLuaItemSession(data.externalId, data.luaSource);
        if (data.luaTick) nextTickTime = MyTime.time + 1f;
        if (data.luaLevelUp) PlayerXp.A_LevelUp += OnLevelUp;
        if (data.luaEnemyKilled) Enemy.A_EnemyDied += OnEnemyKilled;
        if (data.luaPlayerDamaged) PlayerHealth.A_TakeDamage += OnPlayerDamaged;
    }
    public override void Cleanup()
    {
        if (data.luaLevelUp) PlayerXp.A_LevelUp -= OnLevelUp;
        if (data.luaEnemyKilled) Enemy.A_EnemyDied -= OnEnemyKilled;
        if (data.luaPlayerDamaged) PlayerHealth.A_TakeDamage -= OnPlayerDamaged;
        session?.Dispose(); session = null;
    }
    public override void Tick()
    {
        if (!data.luaTick || amount <= 0 || MyTime.paused || MyTime.time < nextTickTime) return;
        nextTickTime = MyTime.time + 1f;
        float heal = 0f; int grantXp = 0;
        var inventory = MyPlayer.Instance?.inventory;
        var health = inventory?.playerHealth;
        if (session == null || !session.InvokeTick(amount, health, MyTime.time, out tickScale, out heal, out grantXp, inventory?.playerXp, GameManager.Instance)) tickScale = 0f;
        ApplyBonuses();
        if (heal > 0f && health != null && health.hp > 0) health.Heal(heal, false);
        if (grantXp > 0 && inventory != null && health != null && health.hp > 0) inventory.AddXp(grantXp);
    }
    public void BeforePlayerDamage(DamageContainer hit)
    {
        if (!data.luaBeforePlayerDamage || session == null || hit == null || float.IsNaN(hit.damage) || float.IsInfinity(hit.damage) || hit.damage <= 0f) return;
        var health = MyPlayer.Instance?.inventory?.playerHealth;
        if (health == null || !session.Invoke("before_player_damage", hit.damage, out float multiplier, hit, playerHealth: health, stacks: amount)) return;
        hit.damage = (float)Math.Min(float.MaxValue, (double)hit.damage * multiplier);
    }
    public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
    {
        if (data.luaPreAttack && session != null && session.Invoke("pre_attack", amount, out float multiplier, dc)) itemAttackModifier.AddMultiplier(multiplier);
    }
    public override void ProcOnHitEffects(DamageContainer dc)
    {
        if (!data.luaOnHit || session == null || dc == null || dc.enemy == null || !dc.enemy.CanTakeDamage() || dc.enemy.IsDeadOrDyingNextFrame() || dc.procCoefficient <= 0f ||
            float.IsNaN(dc.damage) || float.IsInfinity(dc.damage) || dc.damage <= 0f) return;
        if (!session.InvokeOnHit(amount, dc, out var actions)) return;
        if (actions[1] > 0) dc.enemy.AddDebuff(EDebuff.Freeze, dc, (float)actions[1]);
        if (actions[2] > 0) dc.enemy.AddDebuff(EDebuff.Stun, dc, (float)actions[2]);
        if (actions[3] > 0) dc.enemy.AddDebuff(EDebuff.Burn, dc, (float)actions[3]);
        if (actions[4] > 0) dc.enemy.AddDebuff(EDebuff.Poison, dc, (float)actions[4]);
        float damage = dc.damage * (float)actions[0];
        if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return;
        var bonus = new DamageContainer(0f, "mod:" + data.externalId) { damage = damage, enemy = dc.enemy, direction = dc.direction, element = dc.element };
        dc.enemy.DamageFromPlayerOther(bonus);
    }
    public override bool HasOnHitEffectProc() => data.luaOnHit;
    public override bool HasPreAttackProc() => data.luaPreAttack;
}
