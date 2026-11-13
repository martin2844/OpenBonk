using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

public class EnemySpecialAttackPrefabMulti : EnemySpecialAttackPrefab
{
    public bool grounded;
    public int numLines;
    public int degreesBetweenLines;
    public int numHits;
    public float hitsSpacingMultiplier;
    public float delayBetweenHits;
    private int numSpawned;

    protected override void Init()
    {
        numSpawned = 0;
        attackEffectPrefab.transform.localScale = Vector3.one * specialAttack.attackRadius;
        StartCoroutine(DoAttack());
    }

    private IEnumerator DoAttack()
    {
        Vector3 origin = enemy.GetFeetPosition();
        Vector3 direction = (MyPlayer.Instance.transform.position - origin).normalized;
        float radius = specialAttack.attackRadius;
        var positions = new List<Vector3>[numHits];
        for (int hit = 0; hit < numHits; hit++) positions[hit] = new List<Vector3>();
        float startAngle = -(numLines - 1) * degreesBetweenLines * .5f;
        for (int line = 0; line < numLines; line++)
        {
            Vector3 lineDirection = Quaternion.Euler(0f, startAngle + line * degreesBetweenLines, 0f) * direction;
            Vector3 first = origin + lineDirection * radius;
            for (int hit = 0; hit < numHits; hit++)
            {
                Vector3 position = first + lineDirection * (hit * radius * 2f * hitsSpacingMultiplier);
                if (grounded)
                {
                    RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
                    if (hits.Length > 0) position = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
                }
                positions[hit].Add(position);
            }
        }
        for (int hit = 0; hit < numHits; hit++)
        {
            foreach (Vector3 position in positions[hit])
            {
                Vector3 target = position;
                CreateWarningSphere(target, () => SpawnHitEffect(target));
            }
            yield return WaitForSecondsCustom(delayBetweenHits);
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
            MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 25f, Vector3.up, false, "Enemy", GetDamageFlags());
        if (++numSpawned >= numLines * numHits) ReturnToPool();
    }
}
