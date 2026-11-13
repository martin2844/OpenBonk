using System;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Data-driven per-level bonuses for externally registered characters.
public sealed class OpenBonkLevelPassiveData : PassiveData
{
    public string displayName, description;
    public EStat[] stats;
    public float[] perLevel;
    public override string GetName() => displayName ?? string.Empty;
    public override string GetDescription() => description ?? string.Empty;
}

public sealed class OpenBonkLevelPassive : PassiveAbility
{
    private readonly OpenBonkLevelPassiveData data;
    public OpenBonkLevelPassive(OpenBonkLevelPassiveData data) { this.data = data; }
    public override void Init() { PlayerXp.A_LevelUp += OnLevelUp; }
    private void OnLevelUp(int level)
    {
        int gainedLevels = Math.Max(0, level);
        for (int i = 0; i < data.stats.Length; i++)
            SetStat(new StatModifier { stat = data.stats[i], modifyType = EStatModifyType.Flat, modification = gainedLevels * data.perLevel[i] });
    }
    public override void Cleanup() { PlayerXp.A_LevelUp -= OnLevelUp; }
    public override void Tick() { }
    public override EPassive GetPassiveType() => data.ePassive;
}
