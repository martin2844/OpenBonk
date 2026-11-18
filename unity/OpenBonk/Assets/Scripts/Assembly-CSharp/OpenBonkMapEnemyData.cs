public sealed class OpenBonkMapEnemyData : EnemyData
{
    internal string displayName;
    internal bool charges, hasIdle;
    internal bool sweepAttack;
    internal UnityEngine.Material hazardMaterial;

    public override string GetName() => displayName;
}
