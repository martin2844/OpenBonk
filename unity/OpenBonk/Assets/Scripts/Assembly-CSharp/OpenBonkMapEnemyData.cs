public sealed class OpenBonkMapEnemyData : EnemyData
{
    internal string displayName;
    internal bool charges, hasIdle;
    internal bool sweepAttack;
    internal UnityEngine.Material hazardMaterial;
    internal bool volleyAttack;
    internal UnityEngine.Material volleyMaterial;
    internal UnityEngine.Color volleyColor;
    internal int volleyCount;
    internal float volleySpread, volleySpeed, volleyRadius, volleyCooldown;

    public override string GetName() => displayName;
}
