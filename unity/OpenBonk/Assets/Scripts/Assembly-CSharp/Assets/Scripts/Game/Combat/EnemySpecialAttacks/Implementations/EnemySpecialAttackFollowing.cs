using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations
{
    public class EnemySpecialAttackFollowing : EnemySpecialAttackPrefab
    {
        public bool grounded;
        public bool predictive;
        public float delayBetweenHits;
        public int numHits;
        private int numSpawned;

        protected override void Init()
        {
            numSpawned = 0;
            attackEffectPrefab.transform.localScale = Vector3.one * specialAttack.attackRadius;
            StartCoroutine(DoAttack());
        }

        private IEnumerator DoAttack()
        {
            for (int i = 0; i < numHits; i++)
            {
                Vector3 target = MyPlayer.Instance.transform.position;
                if (predictive) target += MyPlayer.Instance.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
                if (grounded)
                {
                    RaycastHit[] hits = Physics.RaycastAll(target + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
                    target = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
                }
                Vector3 position = target;
                CreateWarningSphere(position, () => SpawnHitEffect(position));
                float elapsed = 0f;
                while (elapsed < delayBetweenHits) { elapsed += MyTime.deltaTime; yield return null; }
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
            if (++numSpawned >= numHits) ReturnToPool();
        }
    }
}
