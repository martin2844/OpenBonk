using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

public class EnemySpecialAttackManyRandom : EnemySpecialAttackPrefab
{
    public bool grounded;
    public bool predictive;
    public int circles;
    private int numToSpawn;
    private int numSpawned;
    public float delayBetweenCircles;
    public float margin;

    protected override void Init()
    {
        numToSpawn = 0;
        numSpawned = 0;
        StartCoroutine(DoAttack());
    }

    private Vector3 GroundPosition(Vector3 position)
    {
        if (!grounded) return position;
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
        if (hits.Length > 0) position = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
        return position;
    }

    private IEnumerator DoAttack()
    {
        Vector3 center = MyPlayer.Instance.transform.position;
        if (predictive) center += MyPlayer.Instance.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
        center += MyPlayer.Instance.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
        center = GroundPosition(center);
        Vector3 centerTarget = center;
        CreateWarningSphere(centerTarget, () => SpawnHitEffect(centerTarget));
        numToSpawn++;
        yield return WaitForSecondsCustom(delayBetweenCircles);
        float step = specialAttack.attackRadius * 2f + margin;
        for (int ring = 1; ring <= circles; ring++)
        {
            float radius = ring * step;
            int count = Mathf.Max(1, Mathf.CeilToInt(2f * Mathf.PI * radius / step));
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2f * Mathf.PI / count;
                Vector3 target = GroundPosition(center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                CreateWarningSphere(target, () => SpawnHitEffect(target));
                numToSpawn++;
            }
            yield return WaitForSecondsCustom(delayBetweenCircles);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        GameObject effect = GetEffectPrefab();
        if (effect != null)
        {
            effect.transform.position = position;
            effect.SetActive(true);
            effect.transform.localScale = Vector3.one * specialAttack.attackRadius;
        }
        if (Physics.CheckSphere(position, specialAttack.attackRadius, GameManager.Instance.whatIsPlayer))
        {
            MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 25f, Vector3.up, false, "Enemy", GetDamageFlags());
            if (eAttack == EEnemyAttack.PoisonSpikes) MyPlayer.Instance.inventory.statusEffects.PoisonPlayer(8f);
        }
        if (++numSpawned >= numToSpawn) ReturnToPool();
    }
}
