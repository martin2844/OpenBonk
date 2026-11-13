using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks
{
    public class EnemyProjectileMud : EnemySpecialAttackPrefab
    {
        public GameObject hitEffect;
        public ParticleSystem mudParticles;
        public Transform slamParticles;
        public Transform preParticles;
        public bool grounded;
        public bool predictive;

        protected override void Init()
        {
            hitEffect.SetActive(false);
            preParticles.gameObject.SetActive(true);
            Vector3 position = MyPlayer.Instance.transform.position;
            if (predictive) position += MyPlayer.Instance.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
            if (grounded)
            {
                RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
                if (hits.Length > 0) position = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
            }
            CreateWarningSphere(position, SpawnHitEffect);
            transform.position = position;
            slamParticles.localScale = Vector3.one * specialAttack.attackRadius;
            preParticles.localScale = Vector3.one * specialAttack.attackRadius;
            var shape = mudParticles.shape;
            shape.radius = specialAttack.attackRadius;
        }

        private void SpawnHitEffect()
        {
            preParticles.gameObject.SetActive(false);
            hitEffect.SetActive(true);
            if (Physics.CheckSphere(transform.position, specialAttack.attackRadius, GameManager.Instance.whatIsPlayer))
            {
                var player = MyPlayer.Instance;
                Vector3 direction = (player.transform.position - transform.position).normalized;
                player.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 8f, direction, false, "Enemy", DcFlags.BypassEvade | DcFlags.BossDamage);
                player.inventory.statusEffects.SlowPlayer(3f);
            }
            Invoke(nameof(ReturnToPool), 2f);
        }
    }
}
